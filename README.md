# HypesoftChallengeBackEnd

Backend do desafio técnico Hypesoft — API REST para o projeto **ShopSense**, construída com ASP.NET Core 9 seguindo Clean Architecture, DDD e CQRS.

---

## Tecnologias

- **ASP.NET Core 9** — Web API
- **MongoDB** — banco de dados (sem Entity Framework)
- **Keycloak** — autenticação e autorização via JWT
- **MediatR** — padrão CQRS (Commands e Queries)
- **AutoMapper** — mapeamento de entidades para DTOs
- **FluentValidation** — validação de inputs
- **Serilog** — logging estruturado
- **Docker / Docker Compose** — infraestrutura local
- **xUnit + NSubstitute + FluentAssertions** — testes unitários

---

## Arquitetura

```
src/
├── Hypesoft.Domain/          # Entidades, exceções, interfaces de repositório
├── Hypesoft.Application/     # Commands, Queries, Handlers, DTOs, Validators
├── Hypesoft.Infrastructure/  # Repositórios MongoDB, contexto, DI
└── Hypesoft.API/             # Controllers, Middlewares, Program.cs

tests/
└── Hypesoft.Tests/           # Testes unitários dos Handlers
```

---

## Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

---

## Como rodar

### 1. Subir infraestrutura (MongoDB + Keycloak)

```bash
docker-compose up -d mongodb keycloak
```

Aguarde ~15 segundos para o Keycloak inicializar completamente.

### 2. Configurar o Keycloak

Execute o script de setup automático (cria o realm, client e usuário de teste):

```powershell
powershell -ExecutionPolicy Bypass -File setup-keycloak.ps1
```

### 3. Rodar a API

```bash
dotnet run --project src/Hypesoft.API/Hypesoft.API.csproj
```

A API estará disponível em: **http://localhost:5000**

---

## Endpoints

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| GET | `/health` | ❌ | Health check |
| GET | `/categories` | ✅ | Listar categorias |
| POST | `/categories` | ✅ | Criar categoria |
| GET | `/categories/{id}` | ✅ | Buscar categoria por ID |
| PUT | `/categories/{id}` | ✅ | Atualizar categoria |
| DELETE | `/categories/{id}` | ✅ | Remover categoria |
| GET | `/products` | ✅ | Listar produtos |
| POST | `/products` | ✅ | Criar produto |
| GET | `/products/{id}` | ✅ | Buscar produto por ID |
| PUT | `/products/{id}` | ✅ | Atualizar produto |
| DELETE | `/products/{id}` | ✅ | Remover produto |

Documentação interativa: **http://localhost:5000/swagger**

---

## Autenticação

A API usa JWT via Keycloak. Para obter um token de acesso:

```powershell
Invoke-RestMethod -Uri "http://localhost:8080/realms/shopsense/protocol/openid-connect/token" `
  -Method Post `
  -Body @{ client_id="shopsense-frontend"; username="testuser"; password="Test@123"; grant_type="password" } `
  -ContentType "application/x-www-form-urlencoded"
```

Use o `access_token` retornado no header `Authorization: Bearer <token>`.

**Credenciais de teste:**
- Usuário: `testuser`
- Senha: `Test@123`

**Keycloak Admin Console:** http://localhost:8080  
- Usuário: `admin` / Senha: `admin`

---

## Testes

```bash
dotnet test HypesoftChallengeBackEnd.sln
```

---

## Variáveis de configuração

No arquivo `src/Hypesoft.API/appsettings.json`:

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "hypesoft"
  },
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/shopsense",
    "ClientId": "shopsense-frontend"
  }
}
```

---

## CORS

Configurado para aceitar requisições de `http://localhost:5173` (frontend Vue/Vite).

---

## Arquitetura do Sistema (C4 Model)

### Nível 1 — Contexto

```
┌─────────────────────────────────────────────────────────┐
│                     ShopSense System                     │
│                                                          │
│  ┌──────────┐    HTTP/JWT    ┌──────────────────────┐   │
│  │  Usuário │ ─────────────► │   Frontend (Vue/Vite) │  │
│  └──────────┘                └──────────┬───────────┘   │
│                                         │ REST/JSON      │
│                              ┌──────────▼───────────┐   │
│                              │   Backend (ASP.NET)   │   │
│                              └──────────┬───────────┘   │
│                    ┌─────────────────────┘              │
│          ┌─────────▼──────┐   ┌──────────────────┐     │
│          │    MongoDB     │   │    Keycloak       │     │
│          │  (dados)       │   │  (autenticação)   │     │
│          └────────────────┘   └──────────────────┘     │
└─────────────────────────────────────────────────────────┘
```

### Nível 2 — Containers

| Container | Tecnologia | Responsabilidade |
|---|---|---|
| Frontend | Vue 3 + Vite | Interface do usuário, consome a API |
| Backend API | ASP.NET Core 9 | Lógica de negócio, endpoints REST |
| MongoDB | MongoDB 7 | Persistência dos dados |
| Keycloak | Keycloak 26 | Autenticação OAuth2/OpenID Connect |

### Nível 3 — Componentes (Backend)

```
Hypesoft.API  ──►  Hypesoft.Application  ──►  Hypesoft.Domain
                          │
                          ▼
               Hypesoft.Infrastructure  ──►  MongoDB
```

| Camada | Responsabilidade |
|---|---|
| **Domain** | Entidades, regras de negócio, interfaces de repositório |
| **Application** | Commands, Queries, Handlers, DTOs, Validadores |
| **Infrastructure** | Repositórios MongoDB, contexto, seed de dados |
| **API** | Controllers, Middlewares, configuração de DI |

---

## Decisões Arquiteturais (ADRs)

### ADR-001: MongoDB como banco de dados principal

**Contexto:** Sistema de gestão de produtos com possibilidade de atributos flexíveis por categoria.

**Decisão:** Usar MongoDB via `MongoDB.Driver` sem Entity Framework.

**Motivo:**
- Schema flexível facilita evolução dos modelos de produto
- Performance de leitura superior para consultas simples
- Sem overhead do ORM para um domínio relativamente simples
- Alinhado com o stack sugerido no desafio

---

### ADR-002: Keycloak para autenticação

**Contexto:** Sistema precisa de autenticação segura com suporte a OAuth2/OpenID Connect.

**Decisão:** Integrar Keycloak como Identity Provider externo.

**Motivo:**
- Solução enterprise battle-tested para auth
- Suporte nativo a OAuth2, OpenID Connect e JWT
- Permite adicionar SSO, MFA e roles granulares sem alterar o backend
- Desacopla a lógica de autenticação da aplicação

---

### ADR-003: Clean Architecture + CQRS

**Contexto:** Projeto precisa ser maintível, testável e escalável.

**Decisão:** Adotar Clean Architecture com separação em 4 camadas + CQRS via MediatR.

**Motivo:**
- Dependências apontam sempre para dentro (Domain não depende de nada)
- CQRS separa operações de leitura e escrita, facilitando otimização independente
- MediatR desacopla Controllers dos Handlers, facilitando testes unitários
- Estrutura facilita onboarding de novos desenvolvedores
