# 0004. Neon com pooler, DbContext curto e retry de falhas transitórias

- **Status:** Aceita
- **Data:** 2026-10-13

## Contexto

O banco é PostgreSQL serverless no **Neon**, e a interface é **Blazor Server**. Isso trazia três problemas:

1. **Limite de conexões**: o endpoint direto do Neon aceita poucas conexões simultâneas por plano.
2. **Falhas transitórias**: o Neon "adormece" quando fica ocioso. A primeira conexão depois disso pode estourar o timeout, e conexões ociosas podem cair sem aviso.
3. **DbContext de longa duração**: no Blazor Server, um `DbContext` *scoped* vive o circuito inteiro do usuário. O resultado era change tracker crescendo, dados antigos e risco de operações concorrentes no mesmo contexto.

## Decisão

- Usar o endpoint **com pooler** do Neon (host terminado em `-pooler`, PgBouncer em modo transação), com `Maximum Pool Size=20`, `Timeout=15`, `Keepalive=30` e `No Reset On Close=true` na connection string.
- Habilitar `EnableRetryOnFailure(maxRetryCount: 3)` no Npgsql.
- Registrar `AddDbContextFactory<ApplicationDbContext>` e fazer cada método de repositório abrir **um contexto curto por operação**.
- Rodar as transações explícitas dentro de `Database.CreateExecutionStrategy().ExecuteAsync(...)`.

## Alternativas consideradas

- **Endpoint direto, sem pooler** — descartado. O limite de conexões é atingido rápido.
- **`DbContext` scoped no circuito** — descartado pelos problemas de concorrência e de memória.
- **Sem retry** — descartado. O cold start do Neon viraria erro para o usuário.

## Consequências

- ✅ Menos conexões abertas e resiliência ao cold start.
- ✅ Cada operação vê dados atuais, sem estado acumulado no circuito.
- ⚠️ Com o pooler em modo transação não existe estado de sessão entre comandos, e por isso `No Reset On Close=true` é obrigatório (o `DISCARD ALL` do reset não é suportado).
- ⚠️ Toda transação explícita **tem** que rodar dentro do execution strategy, senão o EF Core lança exceção com `EnableRetryOnFailure` ativo.
- ⚠️ Os blocos dentro do execution strategy podem ser repetidos e precisam ser seguros para reexecução.
