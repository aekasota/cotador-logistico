# Segurança

Este documento explica as decisões de segurança da migração (ver também a seção
"Pendências e riscos conhecidos" no relatório final da migração).

## Regra de ouro (ver seção 48 do pedido de migração)

O frontend nunca é tratado como fronteira de segurança. Todo lugar em que o
frontend "esconde" um botão (ex.: o botão "Meu Time" só aparece para
SUPERVISOR/OWNER), o backend também recusa a operação de forma independente
— a checagem de UI é só conveniência de navegação, nunca a única defesa.
Ver `RequireRole` em `CotadorLogistico.Api.Controllers.CotadorControllerBase`
e os testes em `tests/CotadorLogistico.Tests/Controllers/`.

## Autenticação

- Login real: Supabase Auth, chamado diretamente pelo frontend
  (`supabase-js`) com a publishable key. O backend nunca vê a senha do
  usuário.
- O backend valida cada JWT recebido (`Authorization: Bearer <token>`) via
  `Microsoft.AspNetCore.Authentication.JwtBearer`, configurado em
  `CotadorLogistico.Api.Authentication.SupabaseJwtBearerSetup` para dois
  modos possíveis (JWKS assimétrico — padrão — ou segredo HS256 legado, se
  `SUPABASE_JWT_SECRET` estiver configurado). Ver `SUPABASE.md` para saber
  qual modo corresponde ao seu projeto.
- Depois de validado o JWT, `CurrentUserMiddleware` carrega o `profiles`
  correspondente do Postgres — é o `role` gravado ali, não nada do próprio
  JWT, que decide autorização daqui em diante.
- Modo Demonstração (`--demomode`) nunca chega a chamar o Supabase Auth —
  ver `docs/ARCHITECTURE.md`.

## Autorização (roles)

Três papéis (`OPERATOR < SUPERVISOR < OWNER`, ver `CotadorLogistico.Core.Domain.Role`).
A promoção de papel é sempre uma operação administrativa via SQL
(`supabase/scripts/`) — nunca existe, em nenhum lugar da aplicação, um botão
que altere o `role` de alguém para `SUPERVISOR` ou `OWNER`. Ver seção 7 do
pedido de migração e `TeamController.ResolveRoleAndSupervisorAsync`.

## Segredos (tokens de API)

Nunca em texto puro no banco. Duas implementações de `ISecretsStore`:

1. **Vault (padrão)** — `CotadorLogistico.Infrastructure.Secrets.VaultSecretsStore`
   delega para o Supabase Vault através de três funções SQL restritas
   (`app_secrets.set_secret`/`get_secret`/`is_configured`,
   `supabase/migrations/0007_secrets_vault.sql`). Essas funções:
   - vivem no schema `app_secrets`, não `public` — o Data API do Supabase
     (PostgREST) só expõe schemas configurados em "Exposed schemas" (por
     padrão só `public`), então nem aparecem como endpoint HTTP;
   - têm `EXECUTE` revogado de `PUBLIC`/`anon`/`authenticated` explicitamente
     (defesa em profundidade, já que o Postgres concede `EXECUTE` a `PUBLIC`
     por padrão em toda função nova);
   - são as ÚNICAS coisas com permissão de ler `vault.decrypted_secrets`.
2. **AES-256-GCM (alternativa)** — `AesGcmSecretsStore` + `AesGcmCipher`
   (puro, testado isoladamente em `tests/.../Secrets/AesGcmCipherTests.cs`).
   A chave mestra (`SECRETS_MASTER_KEY`) só existe no ambiente do backend —
   nunca no banco.

Em ambos os casos, a API pública (`GET /api/settings`) devolve só booleanos
(`frenetConfigured`, `melhorEnvioConfigured`, `geminiConfigured`) — nunca o
valor do token. Ver `SettingsController`.

## Chaves Supabase: publishable vs. secret

- `VITE_SUPABASE_PUBLISHABLE_KEY` é segura no navegador **desde que RLS
  esteja ativo** em todas as tabelas acessadas via Data API — o que é o
  caso aqui, mas o frontend deste projeto não usa o Data API para nada além
  da própria autenticação (ver `lib/supabaseClient.ts`).
- `SUPABASE_SECRET_KEY` (antiga service_role) **bypassa RLS** e só é usada
  pelo backend, para a API administrativa do Supabase Auth (criação de
  contas). Nunca deve chegar ao navegador.

## Gemini

`GeminiPackageDimensionEstimator` (`CotadorLogistico.Infrastructure.Ai`)
envia a API key pelo header `x-goog-api-key` (nunca na query string, para
não vazar em logs de acesso). **Importante**: em setembro de 2026, a Google
anunciou uma migração de chaves padrão da Generative Language API para
"authorization keys", com rejeição de chaves antigas irrestritas. Esta
implementação foi desenvolvida sem acesso à internet para validar contra a
API real — antes de publicar em produção:

1. Confira a documentação atual em https://ai.google.dev/gemini-api/docs
   para o mecanismo de autenticação vigente.
2. Se o mecanismo mudou, o único lugar que precisa de ajuste é
   `GeminiPackageDimensionEstimator.BuildRequest` (a construção do header)
   — o resto do desenho (endpoint restrito, schema fixo, nunca aceitar
   parâmetros do usuário) continua válido.
3. Rode uma chamada real de ponta a ponta antes de liberar a funcionalidade
   para os usuários.

## O que a IA nunca aceita do frontend (ver seções 18-20)

`POST /api/ai/package-dimensions` aceita exclusivamente
`{ "productDescription": string }`. Modelo, temperatura, instrução de
sistema e schema de saída são fixos no backend
(`GeminiPackageDimensionEstimator`) — não existe parâmetro que o cliente
possa enviar para alterar esse comportamento. A resposta é sempre JSON
estruturado (`responseSchema` do Gemini) e revalidada no servidor antes de
voltar ao cliente (`ParseResponse` — testado em
`tests/.../Ai/GeminiPackageDimensionEstimatorTests.cs`, inclusive contra
envelopes malformados/truncados).

## Erros e logs

- `ExceptionHandlingMiddleware` nunca devolve `ex.Message`/stack trace ao
  cliente — só uma mensagem genérica + um `correlationId`, que também vai
  para o log (para dar suporte sem expor detalhes técnicos).
- Nenhum log registra senha, token, API key, connection string, ou JWT
  completo — ver os comentários "nunca logar X" espalhados pelos
  controllers/serviços que lidam com segredos.

## HTTP

- `SecurityHeadersMiddleware`: `X-Content-Type-Options`, `X-Frame-Options`,
  `Referrer-Policy`, `Content-Security-Policy: default-src 'none'`
  (adequada porque esta API só serve JSON), `Permissions-Policy`.
- CORS restrito à(s) origem(ns) configurada(s) em `CORS_ALLOWED_ORIGINS`
  — nunca `*`.
- `UseHsts()` em produção.
- Rate limiting (`Microsoft.AspNetCore.RateLimiting`, nativo do .NET, sem
  dependência externa): política dedicada para `/api/ai/*` (por usuário) e
  para `/api/demo/*` (por IP, já que são anônimos), mais um limite global
  discreto.
- Tamanho máximo de request (Kestrel `MaxRequestBodySize`) e timeout
  padrão de request (`Microsoft.AspNetCore.Http.Timeouts`).

## Rate limiting / brute force no login

O login em si (`supabase.auth.signInWithPassword`) é servido inteiramente
pelo Supabase Auth, não por este backend — o rate limiting e a proteção
contra força bruta de tentativas de login são responsabilidade do Supabase
(configuráveis no painel do projeto, em Authentication → Rate Limits, e
opcionalmente com CAPTCHA/Turnstile). Não há como este backend adicionar
uma segunda camada sem duplicar esse mecanismo — se seu caso de uso exigir
mais controle, configure isso no painel do Supabase.
