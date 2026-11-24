using EnterpriseFlow.Application.Common.Interfaces;
using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseFlow.Infrastructure.Persistence;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            if (await _context.Users.AnyAsync())
            {
                _logger.LogInformation("Database already contains users. Skipping seed.");
                return;
            }

            _logger.LogInformation("Seeding initial EnterpriseFlow users and contracts...");

            // 1. Seed Enterprise Users
            var adminUser = new User
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                FullName = "Enterprise Admin",
                Email = "admin@enterpriseflow.internal",
                PasswordHash = _passwordHasher.HashPassword("Admin@123456"),
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var approverUser = new User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                FullName = "Legal Approver (Adv. Deshmukh)",
                Email = "approver@enterpriseflow.internal",
                PasswordHash = _passwordHasher.HashPassword("Approver@123456"),
                Role = UserRole.LegalApprover,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var requesterUser = new User
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                FullName = "Enterprise Requester (Vanshika)",
                Email = "requester@enterpriseflow.internal",
                PasswordHash = _passwordHasher.HashPassword("Requester@123456"),
                Role = UserRole.Requester,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _context.Users.AddRangeAsync(adminUser, approverUser, requesterUser);
            await _context.SaveChangesAsync();

            // 2. Seed Realistic Enterprise Contracts
            // A. Draft Contract
            var draftContract = Contract.Create(
                title: "AWS Cloud Infrastructure & Compute Agreement",
                description: "Multi-year enterprise cloud compute and storage agreement covering development, staging, and production clusters.",
                valueAmount: 4500000.00m,
                currency: "INR",
                validFromUtc: DateTime.UtcNow.AddDays(7),
                validToUtc: DateTime.UtcNow.AddYears(1),
                createdById: requesterUser.Id,
                createdByName: requesterUser.FullName,
                assignedApproverId: approverUser.Id
            );

            // B. Pending Review Contract
            var pendingContract = Contract.Create(
                title: "Enterprise Cyber Security & SOC SOW",
                description: "Managed security operations center (SOC 24x7) monitoring, automated threat hunting, and quarterly penetration testing.",
                valueAmount: 1800000.00m,
                currency: "INR",
                validFromUtc: DateTime.UtcNow.AddDays(14),
                validToUtc: DateTime.UtcNow.AddYears(1),
                createdById: requesterUser.Id,
                createdByName: requesterUser.FullName,
                assignedApproverId: approverUser.Id
            );
            pendingContract.SubmitForReview(requesterUser.Id, requesterUser.FullName, "Prepared and submitted for legal team compliance verification.");

            // C. Approved Contract
            var approvedContract = Contract.Create(
                title: "Barclays Core Banking Integration SOW",
                description: "API gateway connection, ISO 20022 payment messaging compliance, and real-time reconciliation engine.",
                valueAmount: 12000000.00m,
                currency: "INR",
                validFromUtc: DateTime.UtcNow.AddDays(3),
                validToUtc: DateTime.UtcNow.AddYears(2),
                createdById: requesterUser.Id,
                createdByName: requesterUser.FullName,
                assignedApproverId: approverUser.Id
            );
            approvedContract.SubmitForReview(requesterUser.Id, requesterUser.FullName, "Initial compliance reviewed with external counsel.");
            approvedContract.Approve(approverUser.Id, approverUser.FullName, approverUser.Role, "All compliance clauses and indemnification terms verified. Approved.");

            // D. Active Contract
            var activeContract = Contract.Create(
                title: "Persistent Systems Global Talent Partner Agreement",
                description: "Enterprise IT consulting, cloud engineering augmentation, and continuous delivery pipeline management.",
                valueAmount: 6500000.00m,
                currency: "INR",
                validFromUtc: DateTime.UtcNow.AddDays(-30),
                validToUtc: DateTime.UtcNow.AddMonths(11),
                createdById: requesterUser.Id,
                createdByName: requesterUser.FullName,
                assignedApproverId: approverUser.Id
            );
            activeContract.SubmitForReview(requesterUser.Id, requesterUser.FullName, "Submitted with statement of work attached.");
            activeContract.Approve(approverUser.Id, approverUser.FullName, approverUser.Role, "Master service agreement verified.");
            activeContract.Activate(adminUser.Id, adminUser.FullName, adminUser.Role, DateTime.UtcNow, "Executed by both parties. Contract active.");

            await _context.Contracts.AddRangeAsync(draftContract, pendingContract, approvedContract, activeContract);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Database seeded successfully with 3 users and 4 contracts with complete audit trails!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding database initial data.");
            throw;
        }
    }
}
