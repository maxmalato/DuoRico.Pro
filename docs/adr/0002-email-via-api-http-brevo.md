# 0002. E-mail transacional via API HTTP do Brevo

- **Status:** Aceita
- **Data:** 2026-09-15

## Contexto

O Identity envia e-mails de confirmação de cadastro, redefinição de senha e troca de e-mail. O envio por SMTP funcionava localmente, mas falhava em produção.

O Render **bloqueia o tráfego de saída nas portas SMTP 25, 465 e 587** em web services do plano free desde 26/09/2025 ([changelog oficial](https://render.com/changelog/free-web-services-will-no-longer-allow-outbound-traffic-to-smtp-ports)), e a porta 25 em qualquer plano. A conexão não é recusada: ela fica pendente até o timeout, e a requisição falha depois de cerca de 100 segundos.

Restrições adicionais:

- A aplicação roda num subdomínio do Render, sem domínio próprio.
- O volume é baixo (e-mails transacionais de poucos usuários).
- Não queríamos guardar senha de aplicativo do Gmail ou credenciais SMTP.

## Decisão

Enviar os e-mails pela **API HTTP do Brevo** (HTTPS na porta 443), com `BrevoEmailSender` implementando `IEmailSender<ApplicationUser>` sobre um `HttpClient` tipado (timeout de 15s).

A implementação é escolhida pela chave `EmailSettings:Provider`:

- `Brevo`: envio real, usado em produção;
- `Log`: o `LoggingEmailSender` escreve o link no log. Serve para desenvolver sem segredo e sem gastar a cota. **Proibido fora de Development**: nesse caso a aplicação não sobe.

A configuração é validada no startup (`ValidateOnStart`).

## Alternativas consideradas

- **SMTP (Gmail ou outro provedor)** — descartado pelo bloqueio de portas do Render.
- **Plano pago do Render para liberar SMTP** — descartado. Custo recorrente só para isso, e a porta 25 continuaria bloqueada.
- **Outros provedores com API HTTP** — em geral exigem domínio próprio verificado. O Brevo aceita remetente único verificado por código, sem domínio próprio, com 300 e-mails/dia no plano gratuito.

## Consequências

- ✅ O envio funciona no Render free.
- ✅ Nenhuma credencial SMTP é armazenada.
- ⚠️ A API key do Brevo vale para **toda a conta** (não tem escopo por permissão) e precisa ser tratada como credencial de alto valor, com rotação periódica ([SECURITY.md](../../SECURITY.md#gestão-de-segredos)).
- ⚠️ Limite de 300 e-mails/dia. Se o uso crescer, será preciso trocar de plano ou de provedor.
- ⚠️ Sem remetente verificado, a API responde 400.
- ⚠️ As variáveis precisam existir **antes** do deploy, senão a validação de startup impede a aplicação de subir.
