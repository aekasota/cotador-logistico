# Configuração do Supabase

Este guia cobre a criação e configuração do projeto Supabase usado pelo
Cotador Logístico. Nenhum valor real (URLs, chaves) aparece aqui — sempre
copie os seus do painel do Supabase.

## 1. Criar o projeto

1. Crie uma conta/organização em https://supabase.com se ainda não tiver.
2. "New Project" → escolha nome, senha do banco (guarde-a — vai para a
   connection string) e região.
3. Aguarde o projeto provisionar (alguns minutos).

## 2. Onde encontrar cada valor

No painel do projeto, em **Settings → API**:

- **Project URL** → `SUPABASE_URL` (backend) e `VITE_SUPABASE_URL` (frontend).
- **Publishable key** (`sb_publishable_...`) → `VITE_SUPABASE_PUBLISHABLE_KEY`.
  Segura para o navegador por design (ver seção 32 do pedido de migração e
  `SECURITY.md`).
- **Secret key** (`sb_secret_...`, antiga "service_role key") →
  `SUPABASE_SECRET_KEY`. **Nunca** vai para o frontend, `.env` do Vite, ou
  qualquer lugar versionado.

Em **Settings → Database → Connection string** (modo "Session" ou via
Supavisor, conforme sua preferência de pooling):

- Connection string Postgres → `SUPABASE_DB_CONNECTION`. Também nunca vai
  para o frontend.

Em **Settings → API → JWT Keys** (o nome exato desta seção pode variar
conforme a versão do painel):

- Se o projeto usa **chaves de assinatura assimétricas** (padrão em
  projetos novos): não configure `SUPABASE_JWT_SECRET` — o backend valida
  os tokens automaticamente via JWKS (`{SUPABASE_URL}/auth/v1/.well-known/jwks.json`).
- Se o projeto ainda usa o **JWT secret legado** (simétrico, HS256):
  copie-o para `SUPABASE_JWT_SECRET`.

## 3. Rodar as migrations

As migrations vivem em `supabase/migrations/*.sql`, numeradas e aplicadas em
ordem. Duas formas de aplicá-las:

### Opção A — Supabase CLI (recomendada)

```bash
npm install -g supabase
supabase login
supabase link --project-ref <seu-project-ref>
supabase db push
```

`supabase db push` aplica todas as migrations em `supabase/migrations/` que
ainda não foram registradas no projeto linkado.

### Opção B — SQL Editor do Supabase Studio

Abra cada arquivo de `supabase/migrations/`, em ordem numérica, e execute no
SQL Editor. Mais trabalhoso, mas funciona sem instalar nada.

### Sobre a extensão Vault

`0001_extensions.sql` tenta habilitar `supabase_vault`. Ela já vem
disponível em projetos Supabase hospedados — se por algum motivo não
estiver (ex.: Postgres auto-hospedado), comente essa linha, pule
`0007_secrets_vault.sql`, e configure o backend para usar o modo `Aes` (ver
`SECURITY.md`).

## 4. Criar a primeira organização e seu OWNER

Não existe cadastro público (ver seção 5). Cada empresa/cliente é uma
**organização** própria (ver `supabase/migrations/0009_organizations.sql`) —
com seu próprio time. As chaves de API (Frenet/Melhor Envio/Gemini) são de
cada **usuário individual**, não da organização (ver
`0010_user_integrations.sql`): nem entre membros do mesmo time elas são
compartilhadas — cada pessoa cadastra as próprias em Configurações depois
de logar. A primeira organização e seu primeiro usuário são criados
manualmente:

1. **Authentication → Users → Add user**, marque "Auto Confirm User",
   preencha e-mail/senha.
2. Copie o `id` do usuário criado (aparece na lista, ou via
   `select id, email from auth.users;`).
3. Abra `supabase/scripts/create-organization-and-owner.sql`, substitua o
   nome da empresa, o UUID e o nome da pessoa, e rode no SQL Editor (ou via
   `psql`).

A partir daí, essa pessoa loga normalmente pela tela de login e já entra
como OWNER da organização recém-criada — com acesso a "Meu Time" para criar
OPERATOR/SUPERVISOR pela própria aplicação (sempre dentro da mesma
organização) e a "Configurações" para cadastrar as PRÓPRIAS chaves de API
(cada usuário que ela criar também cadastra as suas, individualmente).

Para atender uma SEGUNDA empresa/cliente mais tarde, rode o mesmo script de
novo com um novo usuário — isso cria uma organização totalmente nova e
isolada da primeira. `supabase/scripts/list-accounts.mjs` lista todas as
organizações e seus usuários, se precisar conferir o que já existe.

## 5. Promover alguém a SUPERVISOR

Só o OWNER decide isso, e só via SQL (ver seção 7 — de propósito, não existe
essa opção na interface):

```bash
psql "$SUPABASE_DB_CONNECTION" -f supabase/scripts/promote-supervisor.sql
```

(edite o UUID dentro do arquivo antes de rodar).

## 6. Conferir RLS

Todas as tabelas de `public` têm Row Level Security habilitado (ver as
migrations). Para conferir rapidamente que está tudo ativo:

```sql
select tablename, rowsecurity from pg_tables where schemaname = 'public';
```

Todas as linhas devem mostrar `rowsecurity = true`.

## 7. Privilégios da conexão direta do backend

`SUPABASE_DB_CONNECTION` deve apontar para uma role Postgres com permissão
de: ler `auth.users` (usado só para checar e-mail duplicado e mostrar o
e-mail no perfil — nunca a senha, que não existe fora do Auth), ler/escrever
as tabelas de `public` deste projeto, e executar as funções de
`app_secrets` (se estiver usando o modo Vault). A connection string padrão
que o Supabase mostra em **Settings → Database** já usa o usuário `postgres`
(com esses privilégios); se você optar por criar uma role dedicada e mais
restrita para o backend, garanta esses três pontos explicitamente.

## 8. Variáveis de ambiente — resumo

| Variável | Onde | Nunca vai para |
|---|---|---|
| `VITE_SUPABASE_URL` | frontend (`.env.local`) | — (pública) |
| `VITE_SUPABASE_PUBLISHABLE_KEY` | frontend (`.env.local`) | — (pública) |
| `VITE_API_BASE_URL` | frontend (`.env.local`) | — (pública) |
| `SUPABASE_URL` | backend (env do processo) | frontend |
| `SUPABASE_SECRET_KEY` | backend | frontend, logs, Git |
| `SUPABASE_DB_CONNECTION` | backend | frontend, logs, Git |
| `SUPABASE_JWT_SECRET` (opcional) | backend | frontend, logs, Git |
| `SECRETS_MASTER_KEY` (só modo Aes) | backend | frontend, logs, Git |
| `GEMINI_MODEL` / `GEMINI_API_BASE_URL` (opcionais) | backend | — |
