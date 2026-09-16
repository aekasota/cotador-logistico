# Arquitetura — Cotador Logístico Web

Este documento descreve a arquitetura resultante da migração do aplicativo desktop
(WinForms + WebView2 + servidor Kestrel embutido) para uma aplicação web
multiusuário. Para o racional de cada decisão de segurança, veja também
[`SECURITY.md`](SECURITY.md).

## Visão geral

```
CotadorLogistico.sln
├── backend/
│   ├── CotadorLogistico.Core            (domínio puro, sem ASP.NET)
│   ├── CotadorLogistico.Infrastructure   (Postgres/Dapper, HTTP externo, secrets, IA)
│   └── CotadorLogistico.Api              (ASP.NET Core Web API — composition root)
├── frontend/
│   └── CotadorLogistico.Web              (React + TypeScript + Vite)
├── tests/
│   └── CotadorLogistico.Tests            (xUnit — Core + Infrastructure + Api)
├── supabase/
│   ├── migrations/                       (schema SQL versionado)
│   ├── scripts/                          (create-organization-and-owner.sql, promote-supervisor.sql, list-accounts.mjs)
│   └── seed/
└── legacy-desktop/                        (aplicativo WinForms original, mantido só como referência)
```

Monolito modular único, de propósito (ver seção 45 do pedido de migração): nenhuma
fila, nenhum serviço adicional, nenhuma orquestração distribuída. Um backend
ASP.NET Core, um frontend estático, um banco Postgres (Supabase).

## Camadas do backend

- **Core**: entidades de domínio (`Profile`, `Quote`, `QuoteOption`, `Role`),
  interfaces de repositório e de integração (`IShippingApiProxy`,
  `ISecretsStore`, `IExchangeRateClient`, `IPackageDimensionEstimator`,
  `ISupabaseAuthAdminClient`), e a lógica de negócio que não depende de nada
  externo (`FakeQuoteGenerator`, migrado sem alterações de lógica do app
  desktop). Não referencia ASP.NET Core nem Npgsql.
- **Infrastructure**: implementações concretas — repositórios Postgres via
  Npgsql/Dapper, os dois cofres de segredo (`VaultSecretsStore`/
  `AesGcmSecretsStore`), os proxies de frete (`FrenetApiProxy`,
  `MelhorEnvioApiProxy`), o cliente de câmbio (`FrankfurterExchangeRateClient`)
  e o `BackgroundService` que o mantém atualizado, o estimador de dimensões
  via Gemini, o cliente administrativo do Supabase Auth e o resolvedor de
  JWKS.
- **Api**: controllers finos, autenticação JWT, middlewares (exceções,
  headers de segurança), rate limiting. Cada controller checa autorização
  explicitamente (`RequireRole`) — não há um sistema de policies "escondido"
  (ver seção 48 do pedido de migração: a autorização real vive aqui, nunca só
  no frontend).

## Fluxo de uma cotação

1. O frontend chama `POST /api/shipping/frenet` (e, se a comparação estiver
   habilitada, `POST /api/shipping/melhorenvio` em paralelo) com o payload
   exatamente no formato que cada API externa espera — preservado do
   `script.js` original.
2. O controller verifica se a transportadora está configurada
   (`ISecretsStore`), repassa a chamada e devolve a resposta sem modificar o
   corpo.
3. O frontend normaliza e renderiza os resultados (`lib/quoting.ts`),
   calculando os vencedores de preço/prazo de forma independente.
4. Fora do Modo Demonstração, o frontend chama `POST /api/quotes` para
   persistir o que foi mostrado — o backend nunca recalcula nada, só grava.
5. Se a comparação estiver ativa, o frontend busca
   `GET /api/quotes/metrics/price-advantage` para o gráfico "vantagem por
   capital" (seção 27).

## Modo Demonstração — por que não existe mais um decorator

O aplicativo desktop original usava `DemoAwareShippingApiProxy<TReal>`, um
decorator que interceptava a chamada real e a substituía por dados
fabricados quando um "código mágico" era salvo como token da Frenet. Isso
fazia sentido num app de usuário único, onde o "modo demo" era um estado do
processo inteiro.

Na versão web, o Modo Demonstração é, por design, uma sessão que **nunca
passa pelo Supabase Auth** (ver seção 6). Persistir esse estado como
"configuração salva" não se aplica mais — quem está em modo demo não tem
sessão real, não tem `profiles`, não tem nada para "decorar" com contexto de
usuário. Por isso o Modo Demonstração foi reimplementado como uma rota
paralela e pública: `DemoController` (`/api/demo/frenet`,
`/api/demo/melhorenvio`), anônima, protegida só por rate limiting por IP,
que chama `IFakeQuoteGenerator` diretamente — a mesma classe, migrada sem
alterações de lógica.

Consequências desse desenho, todas intencionais:

- Cotações demo **nunca são persistidas** — `DemoController` nunca toca
  `IQuoteRepository`. Isso satisfaz trivialmente "jamais misturar dados demo
  com métricas reais" (seção 6/11) sem precisar filtrar `is_demo` em nenhuma
  consulta do caminho principal.
- A coluna `quotes.is_demo` existe no schema (ver seção 10 — "caso alguma
  persistência seja necessária") mas nunca é gravada como `true` pelo
  backend atual. Ver `supabase/migrations/0004_quotes.sql`.
- Nenhuma credencial real (Frenet, Melhor Envio) é lida em Modo
  Demonstração — os endpoints demo não dependem de `ISecretsStore`.

## Câmbio central

`ExchangeRateUpdaterBackgroundService` roda dentro do próprio processo da
API (nenhum serviço adicional). Ao iniciar e depois periodicamente, verifica
se `exchange_rates` está desatualizada; se estiver, adquire um
`pg_advisory_lock` do Postgres antes de chamar a API externa (Frankfurter) —
isso garante que, mesmo com múltiplas instâncias do backend rodando atrás de
um load balancer, só uma delas realmente faz a chamada HTTP externa por
ciclo. Falhas na API externa são logadas como aviso e o último valor válido
é mantido — nunca substituído por zero/null.

## Segredos

Ver [`SECURITY.md`](SECURITY.md) para o desenho completo. Resumo: o backend
depende só de `ISecretsStore`; a implementação padrão (`VaultSecretsStore`)
delega para o Supabase Vault através de funções SQL restritas que vivem fora
do schema `public` (portanto nunca expostas pelo Data API); a alternativa
(`AesGcmSecretsStore`) cifra com AES-256-GCM usando uma chave mestra que só
existe no ambiente do backend.

## Frontend

React 19 + TypeScript + Vite + React Router. Sem Redux/estado global
genérico — contexts pequenos e específicos (`AuthContext`, `ThemeContext`,
`I18nContext`, `CurrencyContext`) cobrem exatamente o que precisa ser
compartilhado entre telas. A lógica de cotação (montar payload, normalizar
resposta, calcular vencedores) vive em `lib/quoting.ts`, sem nenhuma
dependência de React — os componentes só chamam essas funções e renderizam
o resultado.

O CSS (`styles/global.css`) preserva as variáveis, tipografia (Sora) e
linguagem visual do aplicativo original quase literalmente — a mudança
principal é a introdução de `--content-max-width`/`--page-max-width` e um
`.page-shell` centralizado (ver seção 3 do pedido de migração: o conteúdo
não deve mais ficar preso ao topo/esquerda em monitores grandes).

## O que foi deliberadamente simplificado em relação ao app desktop

- **`LocalServer`** (Kestrel embutido, servido pelo próprio processo
  WinForms) deixou de existir — seu papel foi substituído pela combinação
  natural de "frontend estático" + "API real", que é o que a migração pediu.
- **`NativeExportBridge`/`SaveFileDialog`** foram removidos — exportação
  agora acontece inteiramente no navegador (`XLSX.writeFile`,
  `canvas.toDataURL` + link `download`), sem nenhuma ponte nativa.
- **`JsonFileSettingsService`** (arquivo em `%APPDATA%`) foi substituído por
  `ISecretsStore`, centralizado no servidor.
