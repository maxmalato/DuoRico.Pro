# 🏗️ Arquitetura

Visão técnica do Duo Rico Pro: como o código está organizado, como os dados de cada casal ficam isolados e como funcionam a autenticação e o envio de e-mails.

O **porquê** de cada decisão está registrado nos [ADRs](adr/README.md). A operação em produção está no [runbook](runbook.md).

---

## Visão geral

```text
 Navegador
    │  HTTPS (TLS termina no proxy do Render)
    ▼
┌──────────────────────────────────────────────────────────────┐
│ ASP.NET Core (.NET 10) — container Docker no Render          │
│                                                              │
│  Middleware: ForwardedHeaders → SecurityHeaders (CSP)        │
│              → RateLimiter → Antiforgery                     │
│                                                              │
│  Components (Blazor Server + MudBlazor)                      │
│      │  páginas de conta: SSR estático (Identity)            │
│      │  páginas do app:   Interactive Server (SignalR)       │
│      ▼                                                       │
│  Services  ──►  Repositories  ──►  ApplicationDbContext      │
│      │                               (EF Core + Npgsql)      │
│      └─► IEmailSender ──► BrevoEmailSender ─────────────┐    │
└──────────────────────────────────────┬──────────────────┼────┘
                                       ▼                  ▼
                           PostgreSQL (Neon, pooler)   Brevo API (HTTPS 443)
```

---

## Estrutura de pastas

Tudo fica no projeto único `DuoRico.Pro/`:

| Pasta | Responsabilidade |
|-------|------------------|
| `Components/Pages` | Páginas do app: `Dashboard`, `Incomes`, `Expenses`, `NotFound`, `Error` |
| `Components/Shared` | Componentes reutilizáveis: tabela e formulário de transação, widgets, `MoneyField`, `PeriodSelector` |
| `Components/Layout` | `MainLayout`, `AuthLayout`, navegação (`NavMenu`, `BottomNav`), `UserMenu`, tema |
| `Components/Account` | Fluxos do Identity (login, cadastro, 2FA, gestão da conta, vínculo de casal) |
| `Services` | Regras de aplicação: `TransactionService`, `EmergencyFundService`, `CoupleService`, `CurrentUserService`, envio de e-mail |
| `Repositories` | Acesso a dados: `TransactionRepository`, `EmergencyFundRepository` |
| `Interfaces` | Contratos dos services e repositórios (injeção de dependência) |
| `Models` | Entidades de domínio com validação própria: `Transaction`, `EmergencyFund` |
| `DTOs` | Objetos de entrada e saída entre a UI e os services |
| `Data` | `ApplicationDbContext`, `ApplicationUser`, `AppClaimsPrincipalFactory` |
| `Security` | Rate limit e cabeçalhos de segurança (`SecurityExtensions`) |
| `Helpers` / `Constants` | Utilitários (`ClaimsExtensions`, `DateHelpers`, `AppVersion`) e categorias |
| `Migrations` | Migrations do EF Core (geradas, não editar à mão) |

### Fluxo de dependências

```text
Components ──► Interfaces ◄── Services ──► Interfaces ◄── Repositories ──► Data
                                 │
                                 └──► Models / DTOs
```

- Os componentes dependem apenas das **interfaces** (`ITransactionService`, `ICurrentUserService` etc.), registradas no `Program.cs`.
- Os services não acessam o `DbContext` diretamente. A exceção é o `CoupleService`, que trabalha sobre as tabelas do Identity.
- As entidades (`Models`) protegem as próprias invariantes. Por exemplo, `Transaction.Update` valida descrição e valor, e os repositórios passam pela entidade em vez de atribuir campos soltos.

---

## Acesso a dados

### DbContext de vida curta

No Blazor Server, um `DbContext` *scoped* viveria durante todo o circuito do usuário, com change tracker crescendo, dados antigos e risco de operações concorrentes no mesmo contexto. Por isso:

- os **repositórios** recebem `IDbContextFactory<ApplicationDbContext>` e abrem um contexto **por operação** (`await using var context = ...`);
- o `ApplicationDbContext` *scoped* continua disponível para o Identity, o Data Protection e as páginas SSR, onde o escopo é a própria requisição.

### Consultas

- As leituras projetam direto para DTO no SQL (`TransactionRepository.GetByPeriodAsync`), sem entrar no change tracker.
- As exclusões usam `ExecuteDeleteAsync`, sem carregar entidades.
- Os parcelamentos são gravados num único `SaveChanges`: tudo ou nada.
- Uma transação explícita roda dentro do `CreateExecutionStrategy()`, que é obrigatório com `EnableRetryOnFailure` ([ADR 0004](adr/0004-neon-com-pooler-e-retry.md)).

### Índices

| Tabela | Índice | Motivo |
|--------|--------|--------|
| `AspNetUsers` | `CoupleId` | Filtro por casal em todas as telas |
| `Transactions` | `(UserId, Year, Month)` | Consulta por período |
| `EmergencyFunds` | `CoupleId` (único) | Uma reserva por casal |

### Concorrência otimista

A reserva de emergência é editada pelos dois parceiros. `EmergencyFund.Version` é mapeado para a coluna de sistema `xmin` do PostgreSQL (`IsRowVersion`). Se os dois salvarem ao mesmo tempo, o segundo recebe um conflito em vez de sobrescrever o valor do primeiro.

---

## Isolamento por casal (multi-tenant)

O "tenant" é o **casal**, identificado por `ApplicationUser.CoupleId` (`Guid`).

1. **No login**, o `AppClaimsPrincipalFactory` grava `CoupleId` e o nome do usuário como claims no cookie. O cookie é criptografado pelo Data Protection, então o navegador não consegue ler nem alterar esses valores.
2. **Nas telas**, o `CurrentUserService.GetAsync()` lê o usuário das claims, sem consultar o banco. Sessões antigas, abertas antes de as claims existirem, caem num fallback que consulta o banco num escopo próprio.
3. **Nos repositórios**, toda consulta e toda alteração filtram por `coupleId` (`t.User!.CoupleId == coupleId`). Um ID de transação de outro casal simplesmente não é encontrado (`false`).
4. **Ao vincular ou desvincular**, o `CoupleService` troca o `CoupleId`, soma as reservas de emergência numa única transação e atualiza o *security stamp*. Com isso, as outras sessões da conta são revalidadas e recebem as claims novas.

> ⚠️ **Regra para código novo:** todo método de repositório que lê ou altera dados de negócio **recebe `coupleId` e filtra por ele**. Nunca confie em um ID vindo da UI sem esse filtro.

### Convite de casal

- Código de 8 caracteres sem símbolos ambíguos, gerado com `RandomNumberGenerator`.
- Guardado em `AspNetUserTokens` apenas como **hash SHA-256** mais a data de validade (24h). O texto puro nunca é gravado.
- Uso único. Código inexistente, expirado ou já usado recebe a mesma mensagem, sem revelar qual foi o caso.
- No máximo 2 membros por casal.

---

## Autenticação e sessão

| Item | Implementação |
|------|---------------|
| Identity | `AddIdentityCore<ApplicationUser>` com cookies, `RequireConfirmedAccount = true` |
| Mensagens | `PortugueseIdentityErrorDescriber` (pt-BR) |
| 2FA | TOTP (aplicativos autenticadores) e códigos de recuperação |
| Força bruta | Lockout após 5 falhas (15 min) e rate limit de 10 POSTs/min por IP nas rotas de conta |
| Revalidação | `IdentityRevalidatingAuthenticationStateProvider` revalida o *security stamp* no circuito Blazor |
| Data Protection | Fora de Development, keyring no PostgreSQL protegido por certificado ([ADR 0003](adr/0003-keyring-data-protection-no-postgresql.md)) |

### Cabeçalhos de segurança

O `UseSecurityHeaders` aplica em todas as respostas:

- **CSP** com nonce por requisição. Os scripts só podem vir do próprio site, e o único script inline é o import map do Blazor. Estilos inline são permitidos porque o MudBlazor os gera.
- `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Referrer-Policy` e `Permissions-Policy`.

Os scripts próprios ficam em `wwwroot/js/` (`auth.js`, `theme.js`, `moneyMask.js`), justamente para não precisar liberar `unsafe-inline`.

### Proxy reverso

O TLS termina no proxy do Render. O `UseForwardedHeaders` (com `KnownProxies` vazio, porque o container só é alcançável pelo proxy) faz o app enxergar `https`, que é necessário para montar links de e-mail corretos. Não existe `UseHttpsRedirection`, porque ele quebraria o health check interno do Render.

---

## Envio de e-mails

```text
IEmailSender<ApplicationUser>
 ├─ BrevoEmailSender     Provider=Brevo  → POST https://api.brevo.com/ (HttpClient, timeout 15s)
 └─ LoggingEmailSender   Provider=Log    → escreve o link no log (somente Development)
```

- A escolha acontece no `Program.cs`, a partir de `EmailSettings:Provider`.
- O `EmailSettings` é validado no boot (`ValidateDataAnnotations` + `ValidateOnStart`). Se faltar configuração, a aplicação não sobe.
- `Provider=Log` fora de Development lança exceção no startup.
- Motivo de usar API HTTP em vez de SMTP: [ADR 0002](adr/0002-email-via-api-http-brevo.md).

---

## Configuração

| Seção | Classe | Validação |
|-------|--------|-----------|
| `ConnectionStrings:DefaultConnection` | — | Exceção no startup se ausente |
| `EmailSettings` | `Services/EmailSettings.cs` | `IValidatableObject` + `ValidateOnStart` |
| `DataProtection` | `Services/DataProtectionSettings.cs` | Exceção no startup se ausente fora de Development |

Localmente os valores vêm de **User Secrets**. Em produção vêm de **variáveis de ambiente** (`Secao__Chave`). Veja o [README](../README.md#2️⃣-configurar-user-secrets) e o [runbook](runbook.md).

---

## Versionamento

A versão fica definida em um único lugar, o `<Version>` do `DuoRico.Pro.csproj`, e é lida em runtime pelo `Helpers/AppVersion.cs` ([ADR 0005](adr/0005-versao-unica-no-csproj.md)).
