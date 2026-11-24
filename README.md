# 🏛️ EnterpriseFlow — Enterprise Contract & Workflow Engine

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-brightgreen.svg)](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#clean-architecture)
[![Pattern](https://img.shields.io/badge/Pattern-CQRS%20with%20MediatR-purple.svg)](https://github.com/jbogard/MediatR)
[![Database](https://img.shields.io/badge/PostgreSQL-16-blue.svg)](https://www.postgresql.org/)
[![Caching](https://img.shields.io/badge/Redis-7-red.svg)](https://redis.io/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ed.svg)](https://www.docker.com/)

An enterprise-grade contract lifecycle and compliance workflow engine built according to **Clean Architecture** and **CQRS (Command Query Responsibility Segregation)** principles with **ASP.NET Core 8**, **Entity Framework Core 8**, **PostgreSQL**, **Redis**, and **Docker**.

Targeted for enterprise solutions architectures (e.g. Icertis, Barclays, Persistent Systems, PTC, FIS).

---

## 📐 System Architecture Overview

```
                                  ┌────────────────────────────────────────────────────────┐
                                  │                 API Client / Swagger UI                │
                                  └───────────────────────────┬────────────────────────────┘
                                                              │ HTTP JSON / JWT Bearer
                                                              ▼
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  Presentation Layer: EnterpriseFlow.Api                                                                                 │
│  • ASP.NET Core 8 Web API Controllers (`AuthController`, `ContractsController`, `WorkflowController`, `DashboardController`)│
│  • Global RFC 7807 ProblemDetails Exception Handling Middleware                                                         │
│  • Swagger / OpenAPI with interactive JWT Bearer authorization                                                          │
│  • Current User & Claims Context Extraction (`ICurrentUserService`)                                                     │
└─────────────────────────────────────────────┬───────────────────────────────────────────────────────────────────────────┘
                                              │ Sends Commands & Queries
                                              ▼
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  Application Layer: EnterpriseFlow.Application                                                                          │
│  • CQRS with MediatR: Commands (Write + Transactions) & Queries (Read + Distributed Caching)                            │
│  • Pipeline Behaviors: `ValidationBehavior` (FluentValidation) & `LoggingBehavior` (Execution Monitoring)               │
│  • Domain Transfer Objects (DTOs), Paged Results, and Specification Contracts                                           │
└──────────────────────┬───────────────────────────────────────────────────────────┬──────────────────────────────────────┘
                       │ Implements Interfaces                                     │ Uses Domain Entities & Rules
                       ▼                                                           ▼
┌─────────────────────────────────────────────────────────────┐ ┌─────────────────────────────────────────────────────────┐
│  Infrastructure Layer: EnterpriseFlow.Infrastructure        │ │  Domain Layer: EnterpriseFlow.Domain                    │
│  • EF Core 8 with PostgreSQL (Code-First Migrations)        │ │  • Core Entities: `Contract`, `User`, `ContractAuditLog`│
│  • Fluent API Entity Configurations & Indexing              │ │  • Enums: `ContractStatus`, `UserRole`                  │
│  • Redis Distributed Caching (`ICacheService`)              │ │  • State Machine Guard Clauses & Lifecycle Methods      │
│  • BCrypt Password Hashing (`IPasswordHasher`)              │ │  • Domain Exceptions & Immutable Audit Records          │
│  • Cryptographic JWT & Refresh Token Rotation Engine        │ └─────────────────────────────────────────────────────────┘
│  • Database Auto-Seeder (`DatabaseSeeder`)                  │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔄 Contract Lifecycle State Machine

Enterprise compliance workflows rely on deterministic state transitions. A contract cannot jump straight from `Draft` to `Active` without passing through strict compliance review gates:

```
                  ┌───────────────────────────────────────────────────────┐
                  │                                                       │
                  ▼                                                       │
 [Draft] ───► [PendingReview] ───► [Approved] ───► [Active] ───► [Expired]│
                  │                   │               │                   │
                  ▼                   │               ▼                   │
              [Rejected] ─────────────┴───────► [Terminated] ◄────────────┘
```

### Transition Matrix & RBAC Matrix:

| Current Status | Target Status | Action | Permitted Roles | Notes & Guard Clauses |
|---|---|---|---|---|
| `*` (New) | `Draft` | `CreateContract` | Requester, Admin | Initializes draft contract with creation audit log |
| `Draft` | `Draft` | `UpdateDetails` | Creator, Admin | Edits title, description, monetary value, dates, approver |
| `Draft` / `Rejected` | `PendingReview` | `SubmitForReview` | Creator, Admin | Requires valid dates (`ValidTo > ValidFrom`) |
| `PendingReview` | `Approved` | `Approve` | LegalApprover, Admin | Requesters cannot approve their own contracts |
| `PendingReview` | `Rejected` | `Reject` | LegalApprover, Admin | Mandatory non-empty rejection reason required |
| `Rejected` | `PendingReview` | `SubmitForReview` | Creator, Admin | Re-submittal after addressing legal feedback |
| `Approved` | `Active` | `Activate` | Admin, LegalApprover, Requester | Current date must be within contract validity |
| `Active` | `Expired` | `CheckAndExpire` | System Engine | Automatic transition when validity date has elapsed |
| `Draft`, `Pending`, `Approved`, `Active` | `Terminated` | `Terminate` | Admin, LegalApprover, Creator | Terminal state with mandatory justification |

---

## 🛡️ Enterprise Security & Identity

- **JWT Stateless Access Tokens:** Signed with HMAC-SHA256, 15-minute expiration, containing standard RFC 7519 claims (`sub`, `email`, `name`, `role`, `jti`).
- **Cryptographic Refresh Token Rotation:** 64-byte secure random tokens persisted in PostgreSQL with one-time use policy:
  - When an expired access token is refreshed, the existing refresh token is marked as **Used** and rotated into a new token pair.
  - **Replay/Fraud Detection:** If an already-used refresh token is presented, the system immediately flags the session as compromised and revokes all active tokens for that user.
- **BCrypt Password Hashing:** Salted key derivation with work factor 11.
- **RFC 7807 ProblemDetails:** Uniform JSON error responses across all endpoints.

---

## ⚡ High-Performance Redis Distributed Caching

- **Sliding Expiration:** Contract detail queries (`contracts:id:{id}`) and paged listings (`contracts:paged:...`) are cached in Redis for 5 minutes.
- **Cache Invalidation:** Any state change command (`Create`, `Update`, `SubmitForReview`, `Approve`, `Reject`, `Activate`, `Terminate`) immediately invalidates related Redis cache tags (`contracts:*`, `dashboard:*`).
- **Resilience:** Graceful in-memory fallback if Redis is temporarily unreachable.

---

## 👥 Pre-Seeded Default Accounts

The database seeder automatically initializes the following enterprise accounts upon application launch:

| Role | Email | Password | Purpose |
|---|---|---|---|
| **Admin** | `admin@enterpriseflow.internal` | `Admin@123456` | Full system governance, audit override, terminations |
| **Legal Approver** | `approver@enterpriseflow.internal` | `Approver@123456` | Legal compliance verification, approvals, rejections |
| **Requester** | `requester@enterpriseflow.internal` | `Requester@123456` | Authoring draft contracts, re-submittals, updates |

*Pre-seeded contracts in Draft, PendingReview, Approved, and Active states are also initialized for instant evaluation.*

---

## 🚀 Running the System

### Option A: Running with Docker Compose (Recommended)

Ensure Docker Desktop is running, then execute from the solution root:

```bash
docker compose up --build
```

Services started:
- `enterpriseflow-api`: http://localhost:5000 (Swagger UI at root `http://localhost:5000/`)
- `enterpriseflow-postgres`: `localhost:5432` (Database: `enterpriseflow_db`)
- `enterpriseflow-redis`: `localhost:6379`

### Option B: Running Locally with .NET SDK

1. Ensure PostgreSQL is running locally on port 5432:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=enterpriseflow_db;Username=postgres;Password=postgrespassword;",
     "Redis": "localhost:6379"
   }
   ```
2. Build and run:
   ```bash
   dotnet build EnterpriseFlow.sln
   dotnet run --project src/EnterpriseFlow.Api/EnterpriseFlow.Api.csproj
   ```
3. Open your browser at:
   `http://localhost:5000/` or `https://localhost:5001/` to access the interactive Swagger UI.

---

## 🧪 Automated Testing

Run the full xUnit test suite (covering Domain State Machine, CQRS Command Handlers, FluentValidation behaviors, and BCrypt/JWT security):

```bash
dotnet test tests/EnterpriseFlow.Tests/EnterpriseFlow.Tests.csproj
```

**Test Results:**
- 20 unit and integration tests covering:
  - State machine lifecycle transitions and illegal transition guard clauses.
  - Role-based authorization workflow guard clauses.
  - FluentValidation pipeline behavior.
  - BCrypt password hashing and verification.
  - Cryptographic JWT access token generation and refresh token rotation.
  - Redis distributed cache hit and miss handling.

---

## 📬 API Walkthrough & Sample Workflows

### 1. Authenticate as Requester
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "requester@enterpriseflow.internal",
  "password": "Requester@123456"
}
```
*Response returns JWT `accessToken` and `refreshToken`.* Click **Authorize** in Swagger and paste the token!

### 2. Create a Contract Draft
```http
POST /api/contracts
Authorization: Bearer <accessToken>
Content-Type: application/json

{
  "title": "Enterprise Microservices Modernization Contract",
  "description": "Migration of legacy monolith to .NET 8 Clean Architecture and Kubernetes.",
  "valueAmount": 7500000.00,
  "currency": "INR",
  "validFromUtc": "2026-12-01T00:00:00Z",
  "validToUtc": "2027-12-01T00:00:00Z",
  "assignedApproverId": "22222222-2222-2222-2222-222222222222"
}
```

### 3. Submit for Legal Review
```http
POST /api/contracts/{id}/submit-review
Authorization: Bearer <accessToken>
Content-Type: application/json

{
  "comment": "Final statement of work attached. Ready for compliance verification."
}
```

### 4. Approve Contract (Authenticate as Legal Approver)
```http
POST /api/contracts/{id}/approve
Authorization: Bearer <approverAccessToken>
Content-Type: application/json

{
  "comment": "All liability clauses and indemnity provisions comply with corporate policy. Approved."
}
```

### 5. Activate Approved Contract
```http
POST /api/contracts/{id}/activate
Authorization: Bearer <accessToken>
Content-Type: application/json

{
  "comment": "Both corporate signatories have completed digital signing. Contract is live."
}
```

### 6. Inspect Immutable Audit Trail
```http
GET /api/contracts/{id}/audit-logs
Authorization: Bearer <accessToken>
```
*Returns chronologically ordered audit logs recording every state transition, the user who executed it, old state, new state, and comments.*
