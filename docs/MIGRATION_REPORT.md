# Relatório de migração — Cotador Logístico (desktop → web)

Este relatório documenta o resultado da migração descrita no pedido
original (desktop WinForms/WebView2 → aplicação web React + ASP.NET Core +
Supabase). Ver também `docs/ARCHITECTURE.md`, `docs/SECURITY.md`,
`docs/SUPABASE.md`, `docs/SETUP.md` e `docs/DEPLOY.md` para o detalhe de
cada área.

## Arquitetura final

```
CotadorLogistico.sln
├── backend/CotadorLogistico.Core            domínio puro (entidades, interfaces, FakeQuoteGenerator)
├── backend/CotadorLogistico.Infrastructure   Postgres/Dapper, secrets (Vault/AES), Frenet/ME, câmbio, IA, Supabase Auth admin
├── backend/CotadorLogistico.Api              Web API (controllers, auth JWT, middlewares)
├── frontend/CotadorLogistico.Web             React 19 + TypeScript + Vite
├── tests/CotadorLogistico.Tests              xUnit (85 testes)
├── supabase/{migrations,scripts,seed}        schema SQL versionado
└── legacy-desktop/                            app WinForms original, preservado como referência
```

Monolito modular único (backend + frontend + Postgres gerenciado) —
nenhuma fila, microserviço ou orquestração adicional, conforme pedido.

## Arquivos criados

- **Backend** (~57 arquivos `.cs`): domínio (`Profile`, `Quote`,
  `QuoteOption`, `Role`), repositórios Postgres/Dapper, dois `ISecretsStore`
  (`VaultSecretsStore`, `AesGcmSecretsStore` + `AesGcmCipher`), proxies
  Frenet/Melhor Envio, `FrankfurterExchangeRateClient` +
  `ExchangeRateUpdaterBackgroundService`, `GeminiPackageDimensionEstimator`,
  `SupabaseAuthAdminClient`, `SupabaseJwksProvider`, autenticação JWT dual
  (JWKS/HS256 legado), 9 controllers, middlewares de exceção/segurança,
  rate limiting.
- **Frontend** (~38 arquivos): páginas (`LoginPage`, `CotadorPage`,
  `ProfilePage`, `SupervisorPage`, `SettingsPage`), contexts (`Auth`,
  `Theme`, `I18n`, `Currency`), componentes (`Header`, `QuoteForm`,
  `ResultCard`, `SummaryPanel`, `AdvantageChart`, `ExportBar`,
  `AiAssistPopover`, `Popover`/`Modal` genéricos), `lib/quoting.ts` (lógica
  de cotação livre de framework), CSS preservando a identidade visual
  original + ajuste de centralização/largura máxima (seção 3).
- **Banco**: 10 migrations SQL, 2 scripts administrativos
  (`create-organization-and-owner.sql`, `promote-supervisor.sql`),
  `seed/seed.sql` (documentação, sem dados fictícios).
- **Testes**: 21 arquivos (~85 testes) cobrindo Core, Infrastructure e
  autorização dos controllers.
- **Documentação**: `README.md` reescrito + 5 documentos em `docs/`.

## Arquivos migrados (lógica preservada, adaptada ao novo ambiente)

- `FakeQuoteGenerator` — copiado sem alteração de lógica (`Core/Demo/`).
- `FrenetApiProxy`/`MelhorEnvioApiProxy` — mesmo payload/URL/headers; só a
  origem do token mudou (de `ISettingsService` local para
  `ISecretsStore` centralizado).
- Lógica de cálculo de vencedor (preço/prazo independentes),
  formatação de CEP/decimais, lista das 27 capitais, textos de
  i18n (pt-BR/es-MX/en-US, com chaves novas para as telas adicionadas) —
  portados para `frontend/CotadorLogistico.Web/src/lib` e `src/i18n`.

## Arquivos removidos (nunca chegaram ao novo projeto)

- `LocalServer.cs` (Kestrel embutido) — papel absorvido pela API real.
- `NativeExportBridge.cs`, uso de `SaveFileDialog` — exportação agora é
  100% browser (`XLSX.writeFile`, `canvas.toDataURL` + link `download`).
- `JsonFileSettingsService.cs`/`%APPDATA%` — substituído por
  `ISecretsStore` centralizado no servidor.
- `MainForm.cs`, `Program.cs` do WinForms, dependência de WebView2.

O projeto desktop original foi preservado integralmente em
`legacy-desktop/` para referência/comparação, não apagado (ver seção 44 do
pedido de migração) — considere removê-lo só depois de validar o novo
sistema em produção com dados reais.

## Banco de dados / migrations

| Migration | Conteúdo |
|---|---|
| `0001_extensions.sql` | `pgcrypto`, `supabase_vault` |
| `0002_profiles.sql` | `profiles` + RLS + funções auxiliares de role |
| `0003_presence.sql` | `presence` + status efetivo com janela de 90s |
| `0004_quotes.sql` | `quotes` + `quote_options` (com `provider` distinguindo Frenet/Melhor Envio) + RLS |
| `0005_app_settings.sql` | auditoria de "quem/quando" configurou cada integração |
| `0006_exchange_rates.sql` | câmbio central |
| `0007_secrets_vault.sql` | funções restritas sobre o Supabase Vault |
| `0008_secrets_aes_fallback.sql` | tabela alternativa para o modo AES |
| `0009_organizations.sql` | multi-tenancy real: `organizations` + `profiles.organization_id` — nunca cruza empresas diferentes (Time, presença, cotações) |
| `0010_user_integrations.sql` | corrige o escopo das API keys: de "por organização" para "por USUÁRIO individual" — `user_integrations` substitui `app_settings`; nem entre membros do mesmo time as chaves Frenet/Melhor Envio/Gemini são compartilhadas (decisão explícita do dono da conta) |

RLS habilitado em todas as tabelas de `public`. Scripts administrativos:
`create-organization-and-owner.sql`, `promote-supervisor.sql`,
`list-accounts.mjs`.

## Roles

`OPERATOR < SUPERVISOR < OWNER` (ver `Core.Domain.Role`). Promoção de papel
é sempre via SQL administrativo — nunca pela aplicação. Autorização
verificada explicitamente em cada controller (`RequireRole`), nunca só
escondendo botão no frontend.

## Endpoints

| Método | Rota | Autenticação | Observação |
|---|---|---|---|
| GET | `/api/me` | JWT | perfil + contagens |
| PATCH | `/api/me/preferences` | JWT | tema/idioma |
| GET/POST | `/api/settings` | JWT (POST exige OWNER) | status de integrações, nunca o valor do segredo |
| POST | `/api/shipping/frenet` | JWT | repasse à Frenet |
| POST | `/api/shipping/melhorenvio` | JWT | repasse ao Melhor Envio |
| POST | `/api/quotes` | JWT | persistência |
| GET | `/api/quotes/metrics/price-advantage` | JWT | gráfico da seção 27 |
| GET | `/api/team` | JWT (SUPERVISOR+) | "Meu Time" |
| POST | `/api/team/users` | JWT (SUPERVISOR+) | criar OPERATOR/SUPERVISOR |
| POST | `/api/presence/heartbeat` | JWT | seção 12 |
| POST | `/api/ai/package-dimensions` | JWT, rate limited | único endpoint de IA |
| GET | `/api/demo/frenet`, `/api/demo/melhorenvio` | anônimo, rate limited | Modo Demonstração |
| GET | `/api/exchange-rates` | anônimo | câmbio público |
| GET | `/health` | anônimo | health check |

## Variáveis de ambiente

Ver tabela completa em `docs/SUPABASE.md`. Resumo: `VITE_SUPABASE_URL`,
`VITE_SUPABASE_PUBLISHABLE_KEY`, `VITE_API_BASE_URL` (frontend, públicas);
`SUPABASE_URL`, `SUPABASE_SECRET_KEY`, `SUPABASE_DB_CONNECTION`,
`SUPABASE_JWT_SECRET` (opcional), `SECRETS_MODE`/`SECRETS_MASTER_KEY`
(opcional), `GEMINI_MODEL`/`GEMINI_API_BASE_URL` (opcionais),
`CORS_ALLOWED_ORIGINS` (backend, nunca expostas ao navegador).

## Testes realizados

- `dotnet test CotadorLogistico.sln` → **93/93 aprovados**, sem depender de
  rede, Postgres ou APIs externas reais.
- Suite de resolução de dependências (`DependencyInjectionTests`) —
  instancia todos os 9 controllers com o grafo de DI real, pegando
  dependências faltando antes de produção.
- **Validação manual ponta a ponta do frontend**, rodando o backend real
  (inicialmente com configuração de placeholder) contra o frontend real no
  navegador: login demo (`--demomode`) → cotação em lote das 27 capitais →
  modo comparação → popover "mostrar mais opções" → popover de comparação
  → seleção de opção alternativa → recálculo do vencedor em tela; tema
  claro/escuro; responsividade (375px e desktop).
- **Validação ponta a ponta contra um projeto Supabase real** (feita numa
  sessão seguinte, já com credenciais reais fornecidas pelo usuário): as 8
  migrations foram aplicadas com sucesso
  (`supabase/scripts/run-migrations.mjs`), confirmadas via consulta direta
  (todas as tabelas com RLS habilitado, extensões `pgcrypto`/
  `supabase_vault` presentes, schema `app_secrets` criado), e o backend
  real rodou contra esse banco — o ciclo completo do
  `ExchangeRateUpdaterBackgroundService` (Frankfurter → Postgres →
  `GET /api/exchange-rates`) foi confirmado funcionando de ponta a ponta
  com dados reais.
- Essa validação (frontend + banco real) encontrou e corrigiu **7 bugs
  reais** antes da entrega (detalhados na seção seguinte) — 4 deles só
  seriam visíveis contra um Postgres de verdade, nunca contra dublês.

## Bugs encontrados e corrigidos durante a validação

**Encontrados rodando frontend + backend juntos (sem banco real):**

1. `IFakeQuoteGenerator` nunca fora registrado no container de DI —
   `DemoController` devolvia HTTP 500 em toda chamada. Corrigido, e um
   teste de regressão (`DependencyInjectionTests`) foi adicionado para
   pegar essa classe de erro automaticamente no futuro.
2. O perfil sintético do Modo Demonstração sobrescrevia o tema/idioma já
   detectados do navegador (hardcoded para `light`/`pt-BR`). Corrigido:
   `ThemeContext`/`I18nContext` só sincronizam com o perfil numa sessão
   real.
3. (Ergonomia) Um botão de IA posicionado com offset absoluto fixo
   causava overflow horizontal em viewports estreitos (< 400px).
   Corrigido para participar do fluxo flex normal.

**Encontrados só ao rodar contra o Postgres real do Supabase** (nenhum
teste com dublês conseguiria pegar estes — só aparecem com um driver de
banco de verdade conversando com Postgres de verdade):

4. **Toda leitura de perfil quebrava** (`GET /api/me`, e portanto qualquer
   requisição autenticada, já que `CurrentUserMiddleware` chama isso em
   toda request): Dapper não materializa um record posicional com
   propriedade `DateTimeOffset`/`DateOnly` quando o driver (Npgsql) reporta
   o tipo da coluna como `DateTime` — lança `NotSupportedException` em
   runtime. Afetava `ProfileRepository`, `ExchangeRateRepository` e
   `AppSettingsAuditRepository`. Corrigido: as três trocaram de record
   posicional para classe com construtor sem parâmetros (caminho de
   materialização por reflexão do Dapper), convertendo para
   `DateOnly`/`DateTimeOffset` explicitamente depois.
5. Dapper 2.1.x não sabe montar um parâmetro de gravação a partir de
   `DateOnly` sozinho (`exchange_rates.effective_date` nunca conseguia ser
   escrito). Corrigido com um `SqlMapper.TypeHandler<DateOnly>` dedicado,
   registrado uma vez no composition root.
6. `ExchangeRateOptions.Currencies` duplicava (`"USD, MXN, USD, MXN"`):
   o `ConfigurationBinder` do .NET, ao ligar um array de configuração sobre
   uma propriedade que já chega com um valor default não-vazio, ANEXA em
   vez de substituir. Corrigido: a propriedade começa vazia e o default de
   verdade é aplicado via `PostConfigure`, depois do binding; teste de
   regressão adicionado.
7. A Frankfurter renomeou o endpoint v2 de câmbio: `/latest?symbols=...`
   (o que o código usava) virou `/rates?quotes=...`, e o formato da
   resposta mudou de um objeto único `{rates:{...}}` para um array de
   registros `{date,base,quote,rate}` — um por moeda, cada um com sua
   própria data de referência. Reescrito `FrankfurterExchangeRateClient`
   (e a interface `IExchangeRateClient`, que agora devolve a data de cada
   cotação) de acordo; teste de regressão garante que a URL chamada nunca
   volta a ser `/latest`.

## Pendências

- **Mecanismo de autenticação da API do Gemini não verificado contra a
  API real** (sem acesso à internet para essa API específica neste
  ambiente) — ver aviso completo em `docs/SECURITY.md`. Confirme a
  documentação atual antes de publicar.
- **Login real via Supabase Auth (e-mail/senha) e o fluxo de criação de
  conta pelo supervisor (API administrativa do Auth) ainda não foram
  exercitados ponta a ponta** com um usuário de verdade — o banco em si já
  foi validado (migrations aplicadas, backend conectando e operando
  normalmente), mas falta criar o primeiro usuário/OWNER e testar o login
  real pela tela. Ver `docs/SUPABASE.md` para os próximos passos.
- **Cadastro público está habilitado no Supabase Auth do projeto** (é o
  padrão de todo projeto novo) — a aplicação não expõe nenhuma tela de
  cadastro, mas o Auth em si aceita `signUp` se alguém chamar a API
  diretamente. Recomendado desligar em Authentication → Sign In / Providers
  → Email (ou onde o painel atual expuser essa opção) antes de considerar
  o sistema pronto para uso real.
- Nomes de coluna/exemplos de e-mail de contato (`MelhorEnvioApiProxy`)
  precisam ser trocados pelos dados reais da empresa antes de publicar.

## Riscos conhecidos

- Sem uma instância de backend redundante testada de verdade, o
  comportamento do `pg_advisory_lock` do câmbio (seção 26) foi validado
  por leitura de código e teste da lógica pura de "está desatualizado?",
  não por um teste de concorrência real com duas instâncias.
- A extensão `supabase_vault` precisa estar disponível no plano/projeto
  Supabase usado (confirmada presente no projeto usado para validar esta
  migração); se não estiver disponível no seu, troque para o modo `Aes`
  antes de configurar qualquer integração (ver `docs/SETUP.md`).
- O bundle do frontend (~548 KB no chunk principal, chart.js e xlsx já
  isolados em chunks separados carregados sob demanda) está um pouco
  acima do limite de aviso do Vite — aceitável para uma ferramenta interna,
  mas vale revisitar se a lista de dependências crescer.
  instâncias do backend.

Estas ideias **não foram implementadas** de propósito — ficam registradas
aqui para decisão futura, conforme pedido.
