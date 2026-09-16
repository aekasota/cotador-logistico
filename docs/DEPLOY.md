# Deploy

Arquitetura de deploy alvo (ver seção 41 do pedido de migração):

```
Frontend  → build estático (Vite) → qualquer hosting de arquivos estáticos/CDN
Backend   → ASP.NET Core (processo único) → qualquer host que rode .NET 10
Banco     → Supabase (Postgres gerenciado)
Segredos  → variáveis de ambiente do host do backend + Supabase Vault
```

Nenhuma dependência de Windows, WinForms, WebView2 ou runtime desktop
permanece — o backend roda em qualquer host Linux/Windows com o .NET 10
runtime (ou self-contained, sem runtime instalado).

## Backend

```bash
dotnet publish backend/CotadorLogistico.Api -c Release -o ./publish
```

Gera um `CotadorLogistico.Api.dll` (ou um executável self-contained, com
`-r linux-x64 --self-contained true`, por exemplo) para rodar atrás de um
reverse proxy (Nginx, Caddy, Azure App Service, um Container App, etc.).

Variáveis de ambiente obrigatórias em produção — ver a tabela completa em
`SUPABASE.md`:

- `SUPABASE_URL`, `SUPABASE_SECRET_KEY`, `SUPABASE_DB_CONNECTION`
- `CORS_ALLOWED_ORIGINS` (URL real do frontend em produção)
- `ASPNETCORE_ENVIRONMENT=Production` (ativa `UseHsts()`)

Se o backend rodar atrás de um reverse proxy/load balancer, configure
`ForwardedHeadersOptions.KnownProxies`/`KnownNetworks` em `Program.cs` de
acordo com a infraestrutura real (o valor padrão só habilita o
processamento de `X-Forwarded-For`/`X-Forwarded-Proto`, sem restringir de
quais IPs esses headers são aceitos — restrinja antes de expor
publicamente).

### Logs

Serilog grava em `logs/log-.txt` (relativo ao diretório de execução),
rotacionado diariamente, 30 dias retidos — ajuste `Program.cs` se o seu
host precisar de um caminho diferente (ex.: um volume persistente, ou envio
para um coletor central de logs).

## Frontend

```bash
cd frontend/CotadorLogistico.Web
npm run build
```

Gera `dist/` — arquivos estáticos puros (HTML/CSS/JS). Publique em
qualquer CDN/hosting estático (Vercel, Netlify, Cloudflare Pages, um bucket
S3 + CloudFront, Nginx servindo arquivos estáticos, etc.).

Configure as variáveis de build (`VITE_SUPABASE_URL`,
`VITE_SUPABASE_PUBLISHABLE_KEY`, `VITE_API_BASE_URL`) no ambiente de build
do seu provedor de hosting — elas são embutidas no bundle JS no momento do
build (padrão do Vite), não lidas em runtime.

Como é uma SPA com rotas via `BrowserRouter`, configure seu hosting para
redirecionar qualquer rota desconhecida para `index.html` (rewrite/fallback
de SPA) — caso contrário, recarregar a página em `/profile`, por exemplo,
devolve 404 do servidor estático.

## Banco (Supabase)

Já gerenciado pelo Supabase — não há infraestrutura de banco para operar
além de rodar as migrations (ver `SUPABASE.md`) e, se aplicável, configurar
backups automáticos no painel do projeto (Settings → Backups).

## CI/CD

`.github/workflows/build-e-testes.yml` foi atualizado para a nova estrutura:
restaura e builda a solução `.NET`, roda `dotnet test`, e builda o frontend
(`npm ci && npm run build`) a cada push/PR na branch `main`. Não faz deploy
automático — adapte o job final ao seu provedor de hosting quando decidir
onde publicar (ex.: um passo adicional de `azure/webapps-deploy`,
`vercel deploy`, etc.).
