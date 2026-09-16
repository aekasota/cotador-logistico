using CotadorLogistico.Core.Domain;

namespace CotadorLogistico.Api.Authentication;

public interface ICurrentUserAccessor
{
    Guid? UserId { get; }

    Profile? Profile { get; }

    bool IsAuthenticatedWithoutProfile { get; }
}

public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    public Guid? UserId { get; internal set; }
    public Profile? Profile { get; internal set; }
    public bool IsAuthenticatedWithoutProfile { get; internal set; }
}
