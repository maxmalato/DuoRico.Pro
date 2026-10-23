# Changelog

Todas as mudanças relevantes do projeto são registradas neste arquivo.

O formato segue o [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) e o projeto adota o [Versionamento Semântico](https://semver.org/lang/pt-BR/).

## [Não lançado]

## [1.0.0] - 2026-10-23

Primeira versão numerada. Consolida tudo o que já estava em produção até esta data.

### Adicionado

- Autenticação com ASP.NET Core Identity: cadastro, login, confirmação de e-mail, recuperação de senha e troca de e-mail.
- Autenticação em dois fatores (2FA) com aplicativos autenticadores e códigos de recuperação.
- Vínculo de casal (Couple ID) com convites.
- Cadastro de receitas e despesas, com edição, exclusão e status de pagamento.
- Parcelamentos com geração automática das parcelas e exclusão em massa do grupo.
- Dashboard financeiro com seletor de período e card de saúde do cartão de crédito.
- Reserva de emergência compartilhada do casal.
- E-mails transacionais via API HTTP do Brevo.
- Meta tags Open Graph / Twitter Card para pré-visualização de links.
- Exibição da versão da aplicação no menu do usuário.

### Alterado

- Interface padronizada em MudBlazor, com modo escuro, layout responsivo e navegação mobile-first.
- Textos da interface e mensagens de erro do Identity traduzidos para pt-BR.
- Performance: DbContext de vida curta, parcelas em lote, filtros e projeções no banco e novos índices.

### Segurança

- Keyring do Data Protection persistido no PostgreSQL e criptografado com certificado.
- Proteção contra força bruta, Content Security Policy e prevenção de enumeração de contas no cadastro.
- Remoção do provedor SQL Server não utilizado e atualização dos pacotes Microsoft para 10.0.13.

[Não lançado]: https://github.com/maxmalato/DuoRico.Pro/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/maxmalato/DuoRico.Pro/releases/tag/v1.0.0
