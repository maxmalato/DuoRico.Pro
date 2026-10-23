# 📐 Registros de Decisão de Arquitetura (ADR)

Um ADR registra **uma** decisão técnica relevante: o contexto, o que foi decidido e as consequências. O objetivo é que, daqui a meses, qualquer pessoa entenda o **porquê** de algo estar como está antes de mudá-lo.

O formato segue o modelo de [Michael Nygard](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions).

## Índice

| Nº | Decisão | Status | Data |
|----|---------|--------|------|
| [0001](0001-registrar-decisoes-de-arquitetura.md) | Registrar decisões de arquitetura | Aceita | 2026-10-23 |
| [0002](0002-email-via-api-http-brevo.md) | E-mail transacional via API HTTP do Brevo | Aceita | 2026-09-15 |
| [0003](0003-keyring-data-protection-no-postgresql.md) | Keyring do Data Protection no PostgreSQL, protegido por certificado | Aceita | 2026-09-17 |
| [0004](0004-neon-com-pooler-e-retry.md) | Neon com pooler, DbContext curto e retry de falhas transitórias | Aceita | 2026-10-13 |
| [0005](0005-versao-unica-no-csproj.md) | Versão única no `.csproj` (SemVer) | Aceita | 2026-10-23 |

> Os ADRs 0002 a 0005 foram escritos depois das decisões, a partir do código e dos commits que as introduziram. A data é a do commit.

## Quando escrever um ADR

- Escolha ou troca de provedor, biblioteca ou serviço externo.
- Mudança que afeta segurança, dados persistidos ou o deploy.
- Qualquer decisão em que a alternativa "óbvia" foi descartada. Se alguém perguntaria "por que não fizeram X?", vale um ADR.

## Como escrever

1. Copie o template abaixo para `NNNN-titulo-curto.md`, com o próximo número da sequência.
2. Preencha e adicione a linha no índice.
3. ADR aceito **não se edita**. Se a decisão mudar, crie um novo ADR e marque o antigo como `Substituída por [NNNN]`.

```markdown
# NNNN. Título da decisão

- **Status:** Proposta | Aceita | Substituída por [NNNN](NNNN-...md) | Descontinuada
- **Data:** AAAA-MM-DD

## Contexto

Qual problema ou restrição motivou a decisão. Fatos, não opiniões.

## Decisão

O que foi decidido, de forma direta.

## Alternativas consideradas

- **Alternativa A** — por que foi descartada.

## Consequências

O que fica melhor, o que fica pior e o que passa a ser obrigatório.
```
