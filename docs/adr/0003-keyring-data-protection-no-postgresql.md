# 0003. Keyring do Data Protection no PostgreSQL, protegido por certificado

- **Status:** Aceita
- **Data:** 2026-09-17

## Contexto

O ASP.NET Core Data Protection criptografa o cookie de login, os tokens de redefinição de senha e de confirmação de e-mail e os dados temporários do 2FA. Por padrão, o keyring fica em disco.

O container do Render é **efêmero**: a cada redeploy ou restart, o disco é descartado e um keyring novo é gerado. Com isso:

- todos os usuários eram deslogados;
- os links de redefinição de senha e de confirmação de e-mail pendentes ficavam inválidos.

## Decisão

Fora de `Development`:

- persistir o keyring no PostgreSQL, que já existe, com `PersistKeysToDbContext<ApplicationDbContext>()` (tabela `DataProtectionKeys`);
- criptografar as chaves em repouso com um **certificado X.509** (`ProtectKeysWithCertificate`), fornecido pelas variáveis `DataProtection__CertificateBase64` e `DataProtection__CertificatePassword`;
- fixar `SetApplicationName("DuoRico.Pro")`.

Se o certificado não estiver configurado, a aplicação **não sobe**.

Em `Development`, o keyring padrão em disco continua sendo usado, para que `dotnet run` e `dotnet ef` funcionem sem nenhuma configuração extra.

## Alternativas consideradas

- **Disco persistente do provedor** — descartado. Prende o serviço a uma única instância e depende do plano contratado.
- **Redis ou um cofre de chaves gerenciado (Key Vault/KMS)** — descartado. Seria mais um serviço para manter e pagar, sem ganho relevante para o porte atual.
- **Keyring no banco sem certificado** — descartado. As chaves ficariam em texto claro: quem lesse o banco poderia forjar cookies de qualquer usuário.

## Consequências

- ✅ Sessões e links sobrevivem a redeploys e restarts.
- ✅ Um vazamento do banco, sozinho, não expõe as chaves.
- ⚠️ O PFX passa a ser um segredo crítico. **Perder o certificado invalida todas as sessões e todos os tokens pendentes.** O original fica num cofre de segredos, fora do repositório.
- ⚠️ A rotação do certificado exige cuidado, porque as chaves já gravadas foram protegidas com o certificado anterior ([runbook](../runbook.md#-rotação-de-segredos)).
