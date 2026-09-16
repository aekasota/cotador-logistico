using CotadorLogistico.Core.Domain;

namespace CotadorLogistico.Tests.Domain;

public sealed class RoleTests
{
    [Theory]
    [InlineData(Role.Operator, Role.Operator, true)]
    [InlineData(Role.Supervisor, Role.Operator, true)]
    [InlineData(Role.Owner, Role.Supervisor, true)]
    [InlineData(Role.Operator, Role.Supervisor, false)]
    [InlineData(Role.Supervisor, Role.Owner, false)]
    public void IsAtLeast_RespeitaAHierarquia(Role actual, Role minimum, bool expected)
    {
        Assert.Equal(expected, actual.IsAtLeast(minimum));
    }

    [Theory]
    [InlineData(Role.Operator, "OPERATOR")]
    [InlineData(Role.Supervisor, "SUPERVISOR")]
    [InlineData(Role.Owner, "OWNER")]
    public void ToDbString_DevolveOTextoEsperadoPeloBanco(Role role, string expected)
    {
        Assert.Equal(expected, role.ToDbString());
    }

    [Theory]
    [InlineData("operator", Role.Operator)]
    [InlineData("SUPERVISOR", Role.Supervisor)]
    [InlineData("Owner", Role.Owner)]
    public void TryParse_NaoLigaParaMaiusculasMinusculas(string text, Role expected)
    {
        Assert.True(RoleExtensions.TryParse(text, out var role));
        Assert.Equal(expected, role);
    }

    [Fact]
    public void TryParse_ComTextoInvalido_RetornaFalse()
    {
        Assert.False(RoleExtensions.TryParse("gerente", out _));
        Assert.False(RoleExtensions.TryParse(null, out _));
    }
}
