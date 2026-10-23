# 🛠️ Runbook de Operação

Procedimentos para manter o Duo Rico Pro em produção: deploy, rollback, migrations, rotação de segredos e recuperação de incidentes.

Contexto técnico: [arquitetura](architecture.md) · Decisões: [ADRs](adr/README.md) · Política: [SECURITY.md](../SECURITY.md)

> Todos os valores abaixo são **fictícios**. Os segredos reais ficam apenas no painel do provedor e num cofre de segredos.

---

## 📦 Ambiente de produção

| Componente | Serviço | Observação |
|------------|---------|------------|
| Aplicação | Render (web service Docker) | Build a partir de `DuoRico.Pro/Dockerfile`; container efêmero |
| Banco | Neon (PostgreSQL serverless) | Endpoint com pooler ([ADR 0004](adr/0004-neon-com-pooler-e-retry.md)) |
| E-mail | Brevo (API HTTP) | 300 e-mails/dia no plano gratuito ([ADR 0002](adr/0002-email-via-api-http-brevo.md)) |

### Variáveis de ambiente

A lista completa está no [README](../README.md#variáveis-de-ambiente). Sem `ConnectionStrings__DefaultConnection`, `EmailSettings__*` (com `Provider=Brevo`) e `DataProtection__*`, **a aplicação não sobe**.

---

## 🚀 Deploy

1. **Antes do merge**: se houver variável nova, cadastre-a no painel do Render **antes** do deploy.
2. Se houver migration, siga a seção [Migrations](#️-migrations-em-produção) e decida se ela entra antes ou depois do deploy.
3. Faça o merge/push na `main`. O Render dispara o build automaticamente.
4. Acompanhe os **Logs** do serviço até aparecer `Application started`.
5. Teste rápido:
   - a tela de login abre;
   - o login com uma conta de teste funciona e o dashboard carrega;
   - o menu do usuário mostra a versão esperada.

---

## ⏪ Rollback

1. No painel do Render, abra o serviço → **Events**.
2. Escolha o último deploy saudável → **Rollback**.
3. Se o deploy com problema aplicou uma migration, avalie se o código anterior é compatível com o schema novo. Migrations apenas aditivas (coluna nova anulável, índice, tabela nova) costumam ser compatíveis. Para migrations destrutivas, veja [Recuperação do banco](#-recuperação-do-banco).

---

## 🔁 Aplicação em ciclo de reinício

**Sintoma:** o deploy não termina e os logs mostram o container subindo e caindo repetidamente.

**Causa mais comum:** a validação de startup barrou a aplicação. Isso é intencional: ela falha cedo e com mensagem clara, em vez de gerar erro 500 em runtime.

| Mensagem no log | Correção |
|-----------------|----------|
| `Connection string 'DefaultConnection' not found.` | Definir `ConnectionStrings__DefaultConnection` |
| `OptionsValidationException` mencionando `EmailSettings:...` | Definir ou corrigir `EmailSettings__SenderEmail` / `EmailSettings__BrevoApiKey` |
| `EmailSettings:Provider=Log só é permitido em Development.` | Trocar para `EmailSettings__Provider=Brevo` |
| `DataProtection:CertificateBase64 não está configurado.` | Definir `DataProtection__CertificateBase64` |
| `DataProtection:CertificatePassword não está configurado.` | Definir `DataProtection__CertificatePassword` |
| Erro ao carregar PKCS#12 / senha inválida | Base64 corrompido ou senha não confere com o PFX |

Depois de corrigir a variável, faça um **Manual Deploy**. Se a correção não for imediata, faça o [rollback](#-rollback).

---

## 🗄️ Migrations em produção

A aplicação **não** aplica migrations automaticamente no startup. Elas são aplicadas manualmente com `dotnet ef`.

1. Gere e revise a migration localmente:

   ```bash
   dotnet ef migrations add NomeDaMigration
   dotnet ef migrations script --idempotent -o migration.sql
   ```

   Leia o SQL gerado, principalmente `DROP`, `ALTER COLUMN` e `NOT NULL` sem valor padrão.

2. Classifique a migration:

   | Tipo | Ordem | Versão |
   |------|-------|--------|
   | Aditiva (tabela, coluna anulável, índice) | Migration **antes** do deploy | `MINOR`/`PATCH` |
   | Destrutiva ou que exige ação manual | Criar branch/backup do banco antes, janela combinada | `MAJOR` |

3. Aplique a migration contra produção a partir da sua máquina, passando a connection string por variável de ambiente da sessão (nunca grave no `appsettings`):

   ```bash
   ConnectionStrings__DefaultConnection="Host=ep-exemplo-123456-pooler.sa-east-1.aws.neon.tech;Database=duorico;Username=usuario;Password=********;SSL Mode=Require" \
   dotnet ef database update
   ```

   > Para migrations grandes, prefira o endpoint **sem** `-pooler`: comandos DDL longos funcionam melhor numa conexão direta.

4. Limpe o histórico do shell se a senha tiver ficado registrada nele.

---

## 🔑 Rotação de segredos

A política (frequência e impacto) está no [SECURITY.md](../SECURITY.md#gestão-de-segredos).

### API key do Brevo

1. No Brevo: **SMTP & API → API Keys → Generate a new API key**.
2. No Render: atualize `EmailSettings__BrevoApiKey` e faça o redeploy.
3. Teste com um "Esqueci minha senha" usando uma conta de teste.
4. No Brevo, **revogue a chave antiga**.

### Senha do banco (Neon)

1. No Neon: **Roles → Reset password** do usuário da aplicação.
2. No Render: atualize `ConnectionStrings__DefaultConnection` e faça o redeploy. Enquanto isso, as conexões com a senha antiga falham e há uma breve indisponibilidade.
3. Confirme o login e o carregamento do dashboard.

### Certificado do Data Protection

> ⚠️ **Impacto alto.** O código atual carrega **um único** certificado. As chaves já gravadas na tabela `DataProtectionKeys` foram protegidas com o certificado antigo e não podem ser lidas com o novo. Trocar o certificado **desloga todos os usuários** e invalida links de confirmação e de redefinição de senha pendentes.

Só rotacione em caso de suspeita de vazamento, ou de forma planejada. Para rotacionar sem derrubar sessões, seria preciso uma mudança de código que mantenha o certificado antigo apenas para leitura (`UnprotectKeysWithAnyCertificate`). Registre essa mudança num ADR antes de fazê-la.

Gerar o certificado (fora do repositório, numa pasta temporária):

```bash
openssl req -x509 -newkey rsa:2048 -nodes -keyout dp.key -out dp.crt -days 3650 -subj "/CN=DuoRico.Pro DataProtection"
openssl pkcs12 -export -out dp.pfx -inkey dp.key -in dp.crt -passout pass:<senha-forte>

# Linux/macOS
base64 -w 0 dp.pfx > dp.pfx.b64

# Windows (PowerShell)
[Convert]::ToBase64String([IO.File]::ReadAllBytes("dp.pfx")) | Set-Content -NoNewline dp.pfx.b64
```

1. Guarde o `dp.pfx` e a senha no cofre de segredos.
2. No Render, atualize `DataProtection__CertificateBase64` (conteúdo do `.b64`) e `DataProtection__CertificatePassword`.
3. Faça o redeploy.
4. **Apague** `dp.key`, `dp.crt`, `dp.pfx` e `dp.pfx.b64` da máquina local.

---

## 💾 Recuperação do banco

O Neon mantém histórico para restauração em um ponto no tempo, com janela conforme o plano contratado. Consulte a [documentação oficial do Neon](https://neon.tech/docs) (seções *Branching* e *Restore*).

- **Antes de uma migration destrutiva**, crie um **branch** do banco no Neon. Ele funciona como snapshot instantâneo.
- **Para recuperar**, restaure a branch principal para um instante anterior ao problema, ou aponte temporariamente a connection string para o branch de backup.
- Depois de restaurar, verifique se `__EFMigrationsHistory` corresponde à versão do código em execução.

---

## 🚨 Incidente de segurança

1. **Contenha**: rotacione o segredo afetado (seção acima) ou faça rollback do deploy vulnerável.
2. **Avalie**: quais dados e quais usuários podem ter sido expostos.
3. **LGPD**: se houver risco ou dano relevante aos titulares, a comunicação à ANPD e aos titulares é obrigatória. Registre datas, escopo e medidas adotadas.
4. **Corrija** e publique uma versão `PATCH`, registrando-a na seção **Segurança** do `CHANGELOG.md`.

---

## 🏷️ Release

Siga o [checklist de versionamento do README](../README.md#️-versionamento): atualizar `<Version>`, mover os itens do `CHANGELOG.md`, commitar e criar a tag.
