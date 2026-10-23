# 🔐 Política de Segurança

O Duo Rico Pro armazena **dados financeiros pessoais** de casais e famílias. Segurança e privacidade são requisitos do produto, e não opcionais.

## Versões suportadas

Somente a versão mais recente em produção recebe correções de segurança. O histórico de versões fica no [CHANGELOG.md](CHANGELOG.md).

| Versão | Suportada |
|--------|-----------|
| Última versão publicada (`1.x`) | ✅ |
| Versões anteriores | ❌ |

## Como reportar uma vulnerabilidade

**Não abra issue pública** para relatar vulnerabilidades.

1. Use o **Private Vulnerability Reporting** do GitHub: aba **Security → Report a vulnerability** do repositório. O recurso precisa estar habilitado em **Settings → Code security**.
2. Descreva:
   - o componente afetado (página, endpoint ou fluxo, como login, 2FA ou convite de casal);
   - os passos para reproduzir;
   - o impacto esperado (exposição de dados, escalonamento de acesso etc.).
3. **Não inclua dados reais** de usuários, senhas, tokens ou chaves no relato. Use contas de teste e dados fictícios.

### O que esperar

| Etapa | Prazo de referência |
|-------|---------------------|
| Confirmação de recebimento | até 5 dias úteis |
| Avaliação inicial e severidade | até 10 dias úteis |
| Correção | conforme a severidade; falhas críticas têm prioridade sobre qualquer outra entrega |

A correção é publicada como versão `PATCH` e registrada na seção **Segurança** do `CHANGELOG.md`, sem detalhes que permitam explorar a falha em versões antigas.

### Fora do escopo

- Ataques de negação de serviço (DoS) e testes de carga contra o ambiente de produção.
- Engenharia social e phishing.
- Vulnerabilidades dos provedores (Render, Neon, Brevo), que devem ser reportadas diretamente a eles.

## Controles existentes

| Controle | Onde |
|----------|------|
| ASP.NET Core Identity com confirmação de e-mail obrigatória | `Program.cs` |
| Autenticação em dois fatores (TOTP) e códigos de recuperação | `Components/Account/` |
| Bloqueio da conta após 5 senhas erradas (15 minutos) | `Program.cs` |
| Rate limit de 10 POSTs/minuto por IP nas rotas de autenticação | `Security/SecurityExtensions.cs` |
| Content Security Policy com nonce, `X-Frame-Options`, `nosniff`, `Referrer-Policy`, `Permissions-Policy` | `Security/SecurityExtensions.cs` |
| HSTS fora de Development | `Program.cs` |
| Prevenção de enumeração de contas (mesma resposta para conta existente ou não) | `Register.razor`, `ForgotPassword.razor`, `ResendEmailConfirmation.razor`, `CoupleService` |
| Código de convite guardado apenas como hash SHA-256, de uso único e válido por 24h | `Services/CoupleService.cs` |
| Isolamento dos dados por casal (`CoupleId`) em todas as consultas | `Repositories/` |
| Keyring do Data Protection no PostgreSQL, criptografado com certificado | `Program.cs` · [ADR 0003](docs/adr/0003-keyring-data-protection-no-postgresql.md) |
| Configuração validada no startup (`ValidateOnStart`) | `Services/EmailSettings.cs` |
| `Provider=Log` de e-mail bloqueado fora de Development | `Program.cs` |
| Segredos fora do controle de versão (User Secrets localmente, variáveis de ambiente em produção) | [README](README.md#variáveis-de-ambiente) |

## Gestão de segredos

Nenhum segredo pode ser versionado, colocado no `Dockerfile` (`ENV`/`ARG`) ou escrito em logs.

| Segredo | Onde fica | Quando rotacionar | Impacto da troca |
|---------|-----------|-------------------|------------------|
| `EmailSettings__BrevoApiKey` | Variável de ambiente no Render | A cada 6 meses, ou imediatamente se houver suspeita de vazamento | Nenhum para os usuários. A chave vale para toda a conta do Brevo, por isso a chave antiga precisa ser revogada no painel |
| `ConnectionStrings__DefaultConnection` (senha do Neon) | Variável de ambiente no Render | A cada 6 meses, ou após qualquer saída de acesso | Breve indisponibilidade durante o redeploy |
| `DataProtection__CertificateBase64` / `DataProtection__CertificatePassword` | Variável de ambiente no Render; PFX original em cofre de segredos | Antes do vencimento do certificado, ou se houver suspeita de vazamento | **Alto.** As chaves já gravadas no banco foram criptografadas com o certificado antigo. Sem ele, todas as sessões, links de redefinição de senha e tokens pendentes deixam de valer |

O passo a passo de cada rotação está no [runbook](docs/runbook.md#-rotação-de-segredos).

### Se um segredo vazar

1. Revogue ou rotacione o segredo **imediatamente**, antes de investigar.
2. Atualize a variável no Render e faça o redeploy.
3. Se o segredo esteve em algum commit, considere o histórico do Git comprometido: trocar o valor é obrigatório, e apenas removê-lo do repositório não basta.
4. Registre o incidente e a correção.

## Privacidade e LGPD

- **Minimização**: a aplicação coleta somente o necessário para a gestão financeira (nome, e-mail e lançamentos).
- **Logs**: nunca registrar valores de transações, senhas, tokens, links de confirmação ou e-mails completos em produção. Os logs usam identificadores (`UserId`) em vez de dados pessoais.
- **`LoggingEmailSender`**: escreve no log os links de confirmação e de redefinição de senha, e por isso fica **restrito a Development**. A aplicação se recusa a subir com `Provider=Log` em outro ambiente.
- **Dados em exemplos**: documentação, testes e relatos de bug usam apenas dados fictícios (`exemplo.com`, valores inventados).
- **Direitos do titular**: o usuário pode baixar os próprios dados e excluir a conta em **Minha conta** (`Manage/PersonalData.razor` e `Manage/DeletePersonalData.razor`).
