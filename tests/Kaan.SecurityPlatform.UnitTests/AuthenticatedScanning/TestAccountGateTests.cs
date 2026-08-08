using FluentAssertions;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Features.AuthenticatedScanning;
using Kaan.SecurityPlatform.Application.Features.AuthenticatedScanning.Dtos;
using Kaan.SecurityPlatform.Application.Features.HackerOne;
using Kaan.SecurityPlatform.Application.Features.Validation;
using Kaan.SecurityPlatform.Domain.Entities.Projects;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.AuthenticatedScanning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Xunit;

namespace Kaan.SecurityPlatform.UnitTests.AuthenticatedScanning;

public sealed class TestAccountGateTests
{
    [Fact]
    public async Task Confirm_registration_requires_explicit_submit_approval()
    {
        var svc = CreateService(out _);
        var result = await svc.ConfirmRegistrationSubmitAsync(new ConfirmRegistrationSubmitRequest(
            Guid.NewGuid(), Guid.NewGuid(), "https://example.com/register", ExplicitSubmitApproval: false, null));
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("submit_approval_required");
    }

    [Fact]
    public async Task Identity_profile_requires_user_provided_email()
    {
        var svc = CreateService(out _);
        var result = await svc.CreateIdentityProfileAsync(new UpsertTestIdentityProfileRequest(
            Guid.NewGuid(), "p1", "", null, null, null, null, null, null, null, null, true, true));
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("email_required");
    }

    [Fact]
    public async Task Register_existing_requires_ownership_confirmations()
    {
        var svc = CreateService(out _);
        var result = await svc.RegisterExistingAsync(new RegisterExistingTestAccountRequest(
            Guid.NewGuid(), "Security Test Account", "t@example.com", null, null, "Password!Password!1234",
            null, ValidationSessionRole.TestAccountA, OwnershipConfirmed: false, TestingPermissionConfirmed: true));
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ownership_required");
    }

    [Fact]
    public async Task Scope_required_outside_development()
    {
        var svc = CreateService(out var env, isDevelopment: false);
        var targetId = Guid.NewGuid();
        var result = await svc.CreateIdentityProfileAsync(new UpsertTestIdentityProfileRequest(
            targetId, "p1", "security-test@example.com", "u1", null, null, "Security Test",
            null, null, null, null, true, true));
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("scope_required");
    }

    [Fact]
    public void Password_not_stored_plaintext_in_vault_reference()
    {
        var protector = new RoundTripProtector();
        var vault = new TestAccountVault(protector);
        var password = "PlaintextSecretValue!2026";
        var reference = vault.ProtectPassword(password);
        reference.Should().NotBe(password);
        reference.Should().NotContain("PlaintextSecret");
        vault.UnprotectPassword(reference).Should().Be(password);
    }

    [Fact]
    public void Session_cleanup_clears_secrets()
    {
        string? pwd = "secret";
        string? cookie = "a=b";
        new ScanSessionCleanupService().ClearInMemorySecrets(ref pwd, ref cookie);
        pwd.Should().BeNull();
        cookie.Should().BeNull();
    }

    private static TestAccountManagementService CreateService(out IHostEnvironment env, bool isDevelopment = true)
    {
        var db = Substitute.For<IApplicationDbContext>();
        db.TargetTestAccounts.Returns(Substitute.For<DbSet<Domain.Entities.AuthenticatedScanning.TargetTestAccount>>());
        db.TestIdentityProfiles.Returns(Substitute.For<DbSet<Domain.Entities.AuthenticatedScanning.TestIdentityProfile>>());
        db.DomainAssets.Returns(Substitute.For<DbSet<DomainAsset>>());

        var current = Substitute.For<ICurrentUser>();
        current.CompanyId.Returns(Guid.NewGuid());
        current.UserId.Returns(Guid.NewGuid());
        current.IsSystemAdmin.Returns(true);

        var vault = new TestAccountVault(new RoundTripProtector());
        var scope = Substitute.For<IScopePolicyValidator>();
        scope.GetActiveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Domain.Entities.Validation.ScopePolicy?)null);
        var auth = Substitute.For<IAuthorizationEvidenceService>();
        auth.GetActiveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.Validation.ValidationAuthorizationEvidence?)null);

        env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(isDevelopment ? Environments.Development : Environments.Production);

        var audit = Substitute.For<IBugBountyAuditWriter>();
        return new TestAccountManagementService(
            db, current, vault, scope, auth, new RegistrationPageDetector(), env, audit);
    }

    private sealed class RoundTripProtector : ITestAccountSecretProtector
    {
        public string Protect(string plaintext) => "enc:" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plaintext));
        public string Unprotect(string protectedPayload) =>
            System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(protectedPayload["enc:".Length..]));
    }
}
