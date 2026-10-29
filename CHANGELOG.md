# Changelog

Todas as mudanças relevantes do projeto são registradas neste arquivo.

O formato segue o [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) e o projeto adota o [Versionamento Semântico](https://semver.org/lang/pt-BR/).

## [Não lançado]

## [1.0.2] - 2026-10-29

### Corrigido

- Fonte Inter às vezes aparecia na fonte do sistema ao abrir o app. A URL da fonte não tinha fingerprint, então o `MapStaticAssets` a servia com `no-cache` e o navegador a revalidava a cada carga. O `@font-face` agora fica no `App.razor` com a URL do `@Assets` (cache imutável, igual à do preload), e um fallback com métricas da Inter reduz o "pulo" do texto durante a troca.

## [1.0.1] - 2026-10-29

### Adicionado

- Documentação técnica: arquitetura (`docs/architecture.md`), registros de decisão (`docs/adr/`) e runbook de operação (`docs/runbook.md`).
- Política de segurança (`SECURITY.md`) com canal de reporte, gestão de segredos e diretrizes de LGPD.
- `.editorconfig` com o padrão de código do projeto.

### Alterado

- README enxuto: links para a nova documentação, Dockerfile referenciado em vez de copiado, procedimento do certificado do Data Protection movido para o runbook.
- Screenshots do README atualizados com dados fictícios, incluindo Despesas, Casal e Reserva de emergência.

### Corrigido

- Botão "Sair" do menu do usuário não saía da tela no primeiro clique (o redirect de login era montado com `http://` e bloqueado pelo CSP) e retornava erro 500 no segundo. Autenticação e autorização agora rodam depois do `UseForwardedHeaders`.

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

[Não lançado]: https://github.com/maxmalato/DuoRico.Pro/compare/v1.0.2...HEAD
[1.0.2]: https://github.com/maxmalato/DuoRico.Pro/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/maxmalato/DuoRico.Pro/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/maxmalato/DuoRico.Pro/releases/tag/v1.0.0
