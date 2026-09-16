# 📦 Cotador Logístico

Aplicação web desenvolvida para **comparar cotações de frete entre Frenet e Melhor Envio**, permitindo analisar múltiplas rotas, comparar **preço e prazo** e registrar os resultados para acompanhamento da operação.

O projeto nasceu de uma necessidade real da operação comercial: reduzir o tempo gasto em consultas manuais e transformar a análise de frete em um processo mais rápido, visual e padronizado.

A versão atual evoluiu de um aplicativo desktop para uma **arquitetura web com backend, frontend, banco de dados e autenticação**, mantendo o objetivo original e ampliando a solução para uso por equipes e múltiplas organizações.

<img width="1573" height="866" alt="Screenshot_67" src="https://github.com/user-attachments/assets/67fdd718-9a58-4757-90b1-8355632f9e22" />

---

## 🚀 Principais recursos

### Cotação e comparação

* **Cotação em lote** para as 27 capitais brasileiras ou destinos personalizados
* **Comparação Frenet × Melhor Envio** por preço e prazo
* **Seleção de opções alternativas** de frete diretamente na interface
* **Resumo visual da comparação** com destaque para vantagens de preço
* **Persistência das cotações** para geração de métricas
* **Exportação para Excel (.xlsx)**
* **Exportação de gráficos (.png)**

<img width="1532" height="809" alt="Screenshot_68" src="https://github.com/user-attachments/assets/d300d248-6709-4d5d-a1bf-955bd98501f4" />

### Conta e equipe

* **Autenticação por e-mail e senha** utilizando Supabase Auth
* Perfis com três níveis de acesso:

  * `OPERATOR`
  * `SUPERVISOR`
  * `OWNER`
* **Organizações isoladas** para suportar múltiplas empresas
* **Gerenciamento de equipe** por usuários autorizados
* Associação de operadores a supervisores
* Ativação e desativação de usuários
* Geração de **senha temporária** para suporte administrativo
* Visualização de **presença online/offline**
* Perfil individual com informações e métricas da equipe
* Registro de **auditoria** para alterações administrativas e integrações

<img width="1168" height="581" alt="Screenshot_69" src="https://github.com/user-attachments/assets/e0be1177-91dd-4066-ba5a-b09daa1877b9" />

<img width="1160" height="449" alt="Screenshot_71" src="https://github.com/user-attachments/assets/7f48f736-c6e6-47f1-91f2-648cdf1cfed6" />

### Integrações

* Integração com **Frenet**
* Integração com **Melhor Envio**
* Integração com **Supabase**
* **Câmbio centralizado** com atualização periódica via Frankfurter
* Integração opcional com **Google Gemini** para estimativa de dimensões de embalagens
* Status das integrações diretamente pela interface

### Interface

* **Tema claro e escuro**
* **Português, Espanhol e Inglês**
* Layout responsivo
* Componentes reutilizáveis em React
* Modais, popovers, selects customizados e feedback visual
* Navegação protegida por autenticação e permissões

<img width="1532" height="809" alt="Screenshot_68" src="https://github.com/user-attachments/assets/03761a9c-926c-410a-8acb-d3f5524b4eb3" />

---

## 🛠️ Stack

**Backend**

* C#
* .NET 10
* ASP.NET Core Web API
* Dapper
* Npgsql
* Serilog

**Frontend**

* React 19
* TypeScript
* Vite
* React Router
* Chart.js
* SheetJS
* Supabase JS
* Oxlint

**Banco e infraestrutura**

* PostgreSQL via Supabase
* Supabase Auth
* Row Level Security (RLS)
* Supabase Vault
* AES-256-GCM como alternativa para armazenamento de segredos

**Qualidade e entrega**

* xUnit
* Git
* GitHub Actions
* CI para backend e frontend

---

## 🏗️ Arquitetura

O projeto utiliza uma arquitetura de **monólito modular**, separando domínio, infraestrutura, API, interface e persistência.

```text
CotadorLogistico.sln
│
├── backend/
│   ├── CotadorLogistico.Core
│   │   ├── Domain
│   │   ├── Shipping
│   │   ├── ExchangeRates
│   │   ├── Secrets
│   │   └── Ai
│   │
│   ├── CotadorLogistico.Infrastructure
│   │   ├── Database
│   │   ├── Profiles
│   │   ├── Quotes
│   │   ├── Shipping
│   │   ├── ExchangeRates
│   │   ├── Secrets
│   │   ├── Auth
│   │   └── Ai
│   │
│   └── CotadorLogistico.Api
│       ├── Controllers
│       ├── Authentication
│       ├── Middleware
│       └── Contracts
│
├── frontend/
│   └── CotadorLogistico.Web
│       ├── components
│       ├── contexts
│       ├── pages
│       ├── hooks
│       ├── routes
│       ├── lib
│       └── i18n
│
├── tests/
│   └── CotadorLogistico.Tests
│
├── supabase/
│   ├── migrations
│   ├── scripts
│   └── seed
│
├── docs/
│
└── legacy-desktop/
    └── versão original em WinForms
```

O frontend se comunica com a API HTTP, enquanto o backend concentra autenticação, autorização, regras de negócio, integrações externas e acesso ao banco.

As integrações de frete são realizadas pelo backend, evitando expor credenciais de transportadoras ao navegador.

---

## 🔐 Autenticação e autorização

A autenticação utiliza **Supabase Auth**.

Depois do login, o frontend envia o JWT para a API, que valida o token e carrega o perfil do usuário.

As permissões são verificadas **no backend**, e não apenas escondendo funcionalidades na interface.

```text
OPERATOR < SUPERVISOR < OWNER
```

### OPERATOR

Pode realizar as operações permitidas para seu próprio usuário.

### SUPERVISOR

Além das funções do operador, possui acesso aos recursos de equipe sob sua responsabilidade.

### OWNER

Possui controle administrativo completo dentro da própria organização, incluindo gerenciamento da equipe e das integrações de outros usuários.

A aplicação não permite promover usuários para `OWNER` pela interface.

---

## 🏢 Multi-tenancy

Cada empresa possui uma **organização própria** no banco.

Os dados de:

* Usuários
* Presença
* Cotações
* Métricas
* Configurações

são associados à organização correspondente.

O banco utiliza **Row Level Security (RLS)** para impedir que dados de uma organização sejam acessados por outra.

---

## 🔑 Segurança

As credenciais de integração não ficam no frontend nem são versionadas no Git.

As chaves de:

* Frenet
* Melhor Envio
* Gemini

são armazenadas individualmente por usuário.

O backend utiliza o **Supabase Vault** como armazenamento padrão de segredos. Também existe uma alternativa baseada em **AES-256-GCM** para ambientes que não utilizem Vault.

A API possui ainda:

* Validação de JWT
* CORS configurável
* HSTS em produção
* Headers de segurança
* Rate limiting
* Limitação de tamanho de requisições
* Tratamento centralizado de exceções
* Logs sem exposição de senhas, tokens ou chaves

<img width="643" height="759" alt="Screenshot_73" src="https://github.com/user-attachments/assets/4662b0b1-8a18-464e-8a1e-87cad6f91214" />

---

## 🗄️ Banco de dados

A estrutura do banco é versionada em:

```text
supabase/migrations/
```

Entre os recursos implementados estão:

* `profiles`
* `organizations`
* `presence`
* `quotes`
* `quote_options`
* `user_integrations`
* auditoria de alterações
* armazenamento seguro de segredos
* funções auxiliares de autorização

As migrations são numeradas e aplicadas em ordem.

---

## 🤖 Assistente de IA

O projeto possui um endpoint específico para **estimativa de dimensões de embalagens** utilizando Google Gemini.

O frontend não recebe a chave da API diretamente.

A solicitação passa pelo backend, onde são aplicados autenticação, validações e rate limiting.

A integração foi estruturada para ser opcional e configurável por usuário.

<img width="440" height="487" alt="image" src="https://github.com/user-attachments/assets/fee84c81-4dce-4365-9d52-c9b3f246fa6e" />

---

## 💱 Câmbio centralizado

O sistema possui um serviço centralizado de atualização de câmbio.

O backend consulta a **Frankfurter**, salva os valores no PostgreSQL e disponibiliza os dados pela API.

O frontend utiliza esses valores para conversões de moeda sem precisar consultar diretamente o serviço externo.

---

## 🎭 Modo Demonstração

O projeto possui um modo demonstração que permite apresentar a aplicação sem credenciais reais de Frenet, Melhor Envio ou Supabase Auth.

Na tela de login, digite:

```text
--demomode
```

e clique em **Entrar**.

Nenhuma credencial real é necessária para utilizar o fluxo de demonstração.

<img width="677" height="391" alt="image" src="https://github.com/user-attachments/assets/ff3cbd5a-20d9-4bac-9fc5-022c51d8646a" />

---

## 🧪 Testes

O projeto possui uma suíte automatizada com **93 testes aprovados** durante a validação da migração.

Os testes cobrem, entre outros:

* Regras de domínio
* Autenticação e JWT
* Middleware
* Controllers
* Integrações com Frenet e Melhor Envio
* Repositórios
* Injeção de dependências
* Câmbio
* Segredos
* Integração com IA
* Gerenciamento de usuários

Os testes utilizam dublês e não dependem de:

* APIs externas reais
* Banco PostgreSQL real
* Supabase real

Para executar:

```bash
dotnet test CotadorLogistico.sln
```

---

## ✅ Validação

Além dos testes automatizados, a nova aplicação foi validada manualmente.

Foram exercitados:

* Login em modo demonstração
* Cotação em lote das 27 capitais
* Comparação entre transportadoras
* Exibição de opções alternativas
* Recalculo do vencedor
* Tema claro e escuro
* Responsividade em desktop e 375px
* Fluxo de câmbio
* Persistência e leitura de dados no Supabase

Durante a validação foram encontrados e corrigidos bugs de integração, persistência, DI, câmbio e interface.

---

## ⚙️ Executando localmente

### Pré-requisitos

* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* [Node.js 20+](https://nodejs.org/)
* Um projeto Supabase configurado

Consulte [`docs/SETUP.md`](docs/SETUP.md) para a configuração completa.

### Backend

```bash
dotnet run --project backend/CotadorLogistico.Api
```

Por padrão:

```text
http://localhost:5080
```

### Frontend

```bash
cd frontend/CotadorLogistico.Web
npm install
npm run dev
```

Por padrão:

```text
http://localhost:5173
```

### Banco

As migrations estão em:

```text
supabase/migrations/
```

Consulte [`docs/SUPABASE.md`](docs/SUPABASE.md) para criar e configurar o banco.

---

## 📦 Build

### Backend

```bash
dotnet build CotadorLogistico.sln -c Release
```

Para publicação:

```bash
dotnet publish backend/CotadorLogistico.Api -c Release -o ./publish
```

### Frontend

```bash
cd frontend/CotadorLogistico.Web
npm run build
```

O build gera:

```text
frontend/CotadorLogistico.Web/dist/
```

O frontend é composto por arquivos estáticos e pode ser publicado em plataformas de hosting/CDN.

---

## 🔄 CI

O GitHub Actions executa automaticamente:

```text
Push / Pull Request
        │
        ├── Backend
        │   ├── Restore
        │   ├── Build
        │   ├── Test
        │   └── Publish
        │
        └── Frontend
            ├── npm ci
            └── npm run build
```

O workflow atual valida a aplicação, mas **não realiza deploy automático**.

---

## 📚 Documentação

Mais detalhes estão disponíveis em:

* [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) - arquitetura e decisões técnicas
* [`docs/SECURITY.md`](docs/SECURITY.md) - segurança, autenticação e armazenamento de segredos
* [`docs/SETUP.md`](docs/SETUP.md) - execução local
* [`docs/SUPABASE.md`](docs/SUPABASE.md) - configuração do banco e Auth
* [`docs/DEPLOY.md`](docs/DEPLOY.md) - orientações de deploy
* [`docs/MIGRATION_REPORT.md`](docs/MIGRATION_REPORT.md) - relatório da migração desktop → web

---

## 🖼️ Screenshots

<img width="873" height="561" alt="image" src="https://github.com/user-attachments/assets/afdb8bef-da4f-4ede-8cf4-6705883853e0" />

<img width="1505" height="780" alt="image" src="https://github.com/user-attachments/assets/505e9449-579a-4819-94ee-ca34ac83ddb1" />

<img width="523" height="711" alt="image" src="https://github.com/user-attachments/assets/1ec5d066-dbd8-4eca-8d9b-3913f2ae6b8e" />

<img width="1168" height="581" alt="Screenshot_69" src="https://github.com/user-attachments/assets/ee264266-4f8c-4f46-a193-65f52fe990eb" />

<img width="1160" height="449" alt="Screenshot_71" src="https://github.com/user-attachments/assets/d057d77c-88cd-4386-ac5a-d2b50a1c6e16" />

<img width="1515" height="802" alt="Screenshot_74" src="https://github.com/user-attachments/assets/cbc53bbe-4db9-4dea-b2f7-be69fc45daa5" />

<img width="440" height="487" alt="image" src="https://github.com/user-attachments/assets/99a66ec9-4837-46ea-b1e3-de33f23f766d" />

---

## 📌 Objetivo

Mais do que um comparador de fretes, o Cotador Logístico foi desenvolvido como uma solução para um problema real da operação, unindo **automação, integração de APIs, análise de dados, segurança e experiência de usuário** em uma única aplicação.

A evolução para uma arquitetura web transforma o projeto em uma base preparada para operação multiusuário, múltiplas organizações e integração com infraestrutura de produção.
