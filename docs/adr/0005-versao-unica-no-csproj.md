# 0005. Versão única no `.csproj` (SemVer)

- **Status:** Aceita
- **Data:** 2026-10-23

## Contexto

O projeto não tinha versão numerada, e não era possível saber qual versão estava em produção. O build roda no Docker do Render, que usa apenas a pasta `DuoRico.Pro/` como contexto. A pasta `.git` não fica disponível, o que descarta ferramentas que derivam a versão das tags do Git.

## Decisão

- Adotar o [Versionamento Semântico](https://semver.org/lang/pt-BR/), começando em `1.0.0`.
- Definir a versão **uma única vez**, em `<Version>` no `DuoRico.Pro.csproj`, sem o hash do commit no `InformationalVersion`.
- Ler a versão em runtime com `Helpers/AppVersion.cs` e exibi-la **somente para usuários autenticados**, no menu do usuário.
- Registrar as mudanças no `CHANGELOG.md` (Keep a Changelog) e criar tags `vX.Y.Z` no Git.

## Alternativas consideradas

- **Ferramentas baseadas em tags do Git (MinVer, GitVersion, Nerdbank.GitVersioning)** — descartado. Dependem do `.git`, que não existe no build do Render, e adicionariam uma dependência.
- **Versão em arquivo separado** — descartado. Seriam duas fontes de verdade.
- **Exibir a versão publicamente (rodapé)** — descartado. Revelar a versão em produção ajuda quem procura falhas conhecidas.

## Consequências

- ✅ Uma fonte de verdade que funciona em qualquer ambiente de build.
- ⚠️ A versão é atualizada **manualmente** no release, seguindo o checklist do [README](../../README.md#️-versionamento).
- ⚠️ Migration destrutiva ou que exige ação manual conta como mudança `MAJOR`.
