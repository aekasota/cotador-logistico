# Executando localmente

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/) (testado com Node 24)
- Um projeto Supabase configurado — ver [`SUPABASE.md`](SUPABASE.md)

## 1. Backend (`CotadorLogistico.Api`)

Configure as variáveis de ambiente (ver tabela completa em `SUPABASE.md`).
No PowerShell:

```powershell
$env:SUPABASE_URL = "https://SEU-PROJETO.supabase.co"
$env:SUPABASE_SECRET_KEY = "sb_secret_..."
$env:SUPABASE_DB_CONNECTION = "Host=...;Port=5432;Database=postgres;Username=...;Password=..."
$env:CORS_ALLOWED_ORIGINS = "http://localhost:5173"

dotnet run --project backend/CotadorLogistico.Api
```

No bash:

```bash
export SUPABASE_URL="https://SEU-PROJETO.supabase.co"
export SUPABASE_SECRET_KEY="sb_secret_..."
export SUPABASE_DB_CONNECTION="Host=...;Port=5432;Database=postgres;Username=...;Password=..."
export CORS_ALLOWED_ORIGINS="http://localhost:5173"

dotnet run --project backend/CotadorLogistico.Api
```

Por padrão sobe em `http://localhost:5080` (ajustável via `ASPNETCORE_URLS`).
Se as variáveis obrigatórias (`SUPABASE_URL`, `SUPABASE_SECRET_KEY`,
`SUPABASE_DB_CONNECTION`) não estiverem definidas, o processo falha
imediatamente na inicialização com uma mensagem clara — isso é proposital
(ver `Secrets`/`Supabase` `ValidateOnStart()` em
`CotadorLogistico.Infrastructure.DependencyInjection`).

### Habilitando a IA (opcional em desenvolvimento)

`GEMINI_MODEL`/`GEMINI_API_BASE_URL` têm valores padrão razoáveis. A própria
chave da API do Gemini é configurada **pela aplicação** (tela de
Configurações, como SUPERVISOR/OWNER), não por variável de ambiente — ela
fica no cofre de segredos (Vault/AES), não em texto puro em lugar nenhum.

### Modo do cofre de segredos

Por padrão (`Secrets:Mode = Vault` em `appsettings.json`), o backend usa o
Supabase Vault. Para usar a alternativa AES-256-GCM (ex.: Postgres
auto-hospedado sem a extensão Vault):

```bash
export SECRETS_MODE="Aes"
export SECRETS_MASTER_KEY="$(openssl rand -base64 32)"
```

## 2. Frontend (`CotadorLogistico.Web`)

```bash
cd frontend/CotadorLogistico.Web
npm install
cp .env.example .env.local
# edite .env.local com a URL/publishable key do seu projeto Supabase
npm run dev
```

Sobe em `http://localhost:5173`. O valor de `VITE_API_BASE_URL` em
`.env.local` deve apontar para onde o backend está rodando
(`http://localhost:5080` por padrão).

### Testando sem nenhuma credencial real

Na tela de login, digite exatamente `--demomode` no campo de e-mail e clique
em Entrar — nenhuma senha é pedida, nenhuma chamada ao Supabase Auth
acontece, e a tela funciona inteira com dados fabricados (ver seção 6 do
pedido de migração e `docs/ARCHITECTURE.md`).

## 3. Testes

```bash
dotnet test CotadorLogistico.sln
```

Nenhum teste depende de rede real, Postgres real, ou das APIs externas
(Frenet/Melhor Envio/Gemini/Frankfurter/Supabase) — tudo usa dublês
(`FakeSecretsStore`, `StubHttpMessageHandler`, repositórios em memória etc.).
Ver `tests/CotadorLogistico.Tests`.

## 4. Build de produção

```bash
dotnet build CotadorLogistico.sln -c Release
cd frontend/CotadorLogistico.Web && npm run build
```

O build do frontend gera `frontend/CotadorLogistico.Web/dist/` — um
conjunto de arquivos estáticos prontos para qualquer hosting estático
(ver [`DEPLOY.md`](DEPLOY.md)).
