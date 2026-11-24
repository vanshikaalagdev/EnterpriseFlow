# 🏛️ Project 2: EnterpriseFlow — Enterprise Contract & Workflow Engine (.NET 8 Clean Architecture)
**Target Roles:** .NET Developer Intern / Backend Engineer / Enterprise SDE (Pune Tech: Icertis, Barclays, PTC, Persistent)  
**Primary Tech Stack:** C#, ASP.NET Core 8 Web API, Entity Framework Core, PostgreSQL, MediatR (CQRS), Redis Caching, Docker  
**Architecture Pattern:** Clean Architecture (Domain, Application, Infrastructure, Presentation)

---

## 1. Executive Summary & Why This Project Gets You Hired in Pune

In Pune, 95% of college graduates only know basic MERN (MongoDB, Express, React, Node). Meanwhile, Pune's largest and highest-paying enterprise software powerhouses (**Icertis, Barclays, PTC, Persistent Systems, FIS, and Siemens**) run core business workflows on **C# and .NET Core**.

By pairing **CloudVault** (Node.js + AWS S3 + Redis) with **EnterpriseFlow** (ASP.NET Core 8 + Clean Architecture + CQRS + PostgreSQL), you demonstrate:
1. **True Dual-Stack Backend Mastery:** You can build startup microservices (Node/AWS) AND enterprise mission-critical systems (.NET 8).
2. **Enterprise Software Design:** You don't write spaghetti controller code; you follow **Clean Architecture**, **CQRS (Command Query Responsibility Segregation)**, and **SOLID principles**.
3. **Enterprise Security:** JWT with Refresh Token rotation, Role-Based Access Control (RBAC), and immutable audit logging.

---

## 2. High-Level System Architecture

```
                  ┌────────────────────────────────────────────────────────┐
                  │                 API Client / Swagger UI                │
                  └───────────────────────────┬────────────────────────────┘
                                              │ HTTP JSON
                                              ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│  Presentation Layer: EnterpriseFlow.Api                                                │
│  • ASP.NET Core 8 Web API Controllers / Minimal APIs                                   │
│  • Global Exception & Validation Middleware                                            │
│  • Swagger / OpenAPI with JWT Bearer Auth                                              │
└─────────────────────────────────────────────┬──────────────────────────────────────────┘
                                              │
                                              ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│  Application Layer: EnterpriseFlow.Application (MediatR CQRS)                          │
│  • Commands (CreateContract, ApproveContract, TransitionState)                         │
│  • Queries (GetContractById, ListContractsWithPagination, GetAuditHistory)             │
│  • FluentValidation Pipelines (Request pre-validation before execution)                │
│  • DTOs, AutoMapper/Mapster, Interface Contracts                                      │
└──────────────────────┬───────────────────────────────────────────┬─────────────────────┘
                       │ Implements Interfaces                     │ Uses Core Models
                       ▼                                           ▼
┌────────────────────────────────────────────────┐ ┌─────────────────────────────────────┐
│  Infrastructure Layer: Persistence & Identity  │ │  Domain Layer: EnterpriseFlow.Domain│
│  • EF Core 8 with PostgreSQL (Code-First)      │ │  • Core Entities (Contract, User)   │
│  • Repository Pattern & Unit of Work           │ │  • State Machine Enums              │
│  • Redis Distributed Caching (IDistributedCache)│ │  • Domain Exceptions & Value Objects│
│  • Identity Service (BCrypt + JWT Tokens)      │ │  • Audit Log Records                │
└────────────────────────────────────────────────┘ └─────────────────────────────────────┘
```

---

## 3. Core Enterprise Features

### 1. Domain State Machine & Contract Lifecycle
Enterprise workflows rely on deterministic state transitions. A contract cannot jump straight from `Draft` to `Active`:
```
   [Draft] ──► [PendingReview] ──► [Approved] ──► [Active] ──► [Expired / Terminated]
       ▲              │
       └──[Rejected]──┘
```
* **Guard Clauses:** Only users with `Role.LegalApprover` or `Role.Admin` can approve or reject.
* **Audit Trail:** Every state change automatically writes an immutable entry into `ContractAuditLogs` recording `UserId`, `OldStatus`, `NewStatus`, `Reason`, and `TimestampUtc`.

### 2. CQRS Pattern with MediatR
* Separates **Read operations (Queries)** from **Write operations (Commands)**.
* Commands trigger database transactions with validation pipeline behaviors.
* Queries bypass unnecessary domain logic, querying read models with high-speed **Redis caching**.

### 3. Enterprise Authentication & RBAC
* Stateless JWT Access Tokens (15-minute expiry).
* Cryptographically secure Refresh Tokens stored in PostgreSQL with token rotation and revocation.
* Role-based authorization policies: `[Authorize(Roles = "Admin,LegalApprover")]`.

### 4. Distributed Redis Caching
* Frequently queried contract listings and metadata queries are cached in Redis with a 5-minute Sliding Expiration window.
* Any state change command automatically invalidates related cache tags.

---

## 4. Entity Framework Core Database Schema

```csharp
// Domain Entities
public enum ContractStatus { Draft, PendingReview, Approved, Rejected, Active, Expired, Terminated }
public enum UserRole { Requester, LegalApprover, Admin }

public class Contract
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal ValueAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public ContractStatus Status { get; set; } = ContractStatus.Draft;
    
    public Guid CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
    
    public Guid? AssignedApproverId { get; set; }
    public User? AssignedApprover { get; set; }
    
    public DateTime ValidFromUtc { get; set; }
    public DateTime ValidToUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ContractAuditLog> AuditLogs { get; set; } = new List<ContractAuditLog>();
}

public class ContractAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContractId { get; set; }
    public Guid ChangedById { get; set; }
    public string ChangedByName { get; set; } = string.Empty;
    public ContractStatus PreviousStatus { get; set; }
    public ContractStatus NewStatus { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
```

---

## 5. Implementation Roadmap (Phased Execution)

### Phase 1: Solution Architecture & Project Setup
* Create clean .NET multi-project solution:
  * `EnterpriseFlow.Domain` (Class library)
  * `EnterpriseFlow.Application` (Class library)
  * `EnterpriseFlow.Infrastructure` (Class library)
  * `EnterpriseFlow.Api` (Web API project)
* Setup `docker-compose.yml` with **PostgreSQL 16** and **Redis 7**.

### Phase 2: Domain Modeling & Persistence
* Define Entities, Enums, and Repository interfaces in `Domain`.
* Configure EF Core `DbContext`, entity type configurations, and initial Code-First migrations in `Infrastructure`.
* Seed initial Admin, Approver, and Requester accounts.

### Phase 3: MediatR CQRS & Application Logic
* Implement FluentValidation pipeline behavior.
* Write Commands: `CreateContractCommand`, `SubmitForReviewCommand`, `ApproveContractCommand`, `RejectContractCommand`.
* Write Queries: `GetContractByIdQuery`, `GetContractsPagedQuery`.
* Add Redis caching decorator for queries.

### Phase 4: API Presentation & Security
* Setup JWT Bearer authentication and policy handlers.
* Build Controllers with clean HTTP verb mapping (`POST /api/contracts`, `POST /api/contracts/{id}/approve`, `GET /api/contracts`).
* Configure Swagger with JWT authorization button and request examples.
* Implement global `ExceptionHandlingMiddleware` for clean RFC 7807 ProblemDetails responses.

### Phase 5: Containerization & Verification
* Write multi-stage `Dockerfile` optimizing build cache and Alpine runtime image.
* Verify end-to-end execution via Swagger and automated integration tests.
