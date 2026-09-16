using CotadorLogistico.Api.Authentication;
using CotadorLogistico.Core.Domain;

namespace CotadorLogistico.Tests.TestDoubles;

internal sealed class FakeCurrentUserAccessor : ICurrentUserAccessor
{
    public FakeCurrentUserAccessor(Profile? profile)
    {
        Profile = profile;
        UserId = profile?.Id;
        IsAuthenticatedWithoutProfile = profile is null;
    }

    public Guid? UserId { get; }
    public Profile? Profile { get; }
    public bool IsAuthenticatedWithoutProfile { get; }

    public static readonly Guid DefaultOrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static readonly Guid DefaultUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static Profile MakeProfile(
        Role role, Guid? supervisorId = null, Guid? id = null, Guid? organizationId = null,
        bool isActive = true, bool mustChangePassword = false) => new()
    {
        Id = id ?? Guid.NewGuid(),
        OrganizationId = organizationId ?? DefaultOrganizationId,
        Name = $"Usuário {role}",
        Position = "Vendas",
        Role = role,
        SupervisorId = supervisorId,
        Theme = "light",
        Language = "pt-BR",
        IsActive = isActive,
        MustChangePassword = mustChangePassword,
        Email = $"{role}@teste.com".ToLowerInvariant()
    };
}
