namespace MotorCredito.Arquitetura.Tests;

internal static class PacotesProibidos
{
    public static IReadOnlyList<string> Lista { get; } =
    [
        "FluentAssertions",
        "Moq",
        "Newtonsoft.Json",
        "Amazon.Lambda.Annotations"
    ];
}
