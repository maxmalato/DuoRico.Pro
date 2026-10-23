# 0001. Registrar decisões de arquitetura

- **Status:** Aceita
- **Data:** 2026-10-23

## Contexto

Várias decisões importantes do projeto (provedor de e-mail, persistência do Data Protection, configuração do banco) tinham a justificativa espalhada em comentários no `Program.cs`, em seções do README e em mensagens de commit. O README é voltado a quem vai instalar e rodar o projeto, e misturar ali o histórico das decisões deixa o documento longo e difícil de manter.

## Decisão

Registrar as decisões de arquitetura como ADRs em `docs/adr/`, no formato de Michael Nygard, numerados sequencialmente e versionados junto com o código.

## Alternativas consideradas

- **Manter tudo no README** — descartado. O README cresce sem controle e perde o foco em instalação e uso.
- **Wiki do GitHub** — descartado. Fica fora do versionamento do código, não passa por revisão junto com a mudança e não acompanha o histórico do Git.

## Consequências

- Decisões novas que se encaixem nos critérios do [índice](README.md#quando-escrever-um-adr) passam a exigir um ADR junto com a mudança.
- Comentários no código podem apontar para o ADR em vez de repetir a justificativa inteira.
