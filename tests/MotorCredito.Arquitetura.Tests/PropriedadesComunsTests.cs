using Shouldly;

namespace MotorCredito.Arquitetura.Tests;

[Trait("Category", "Arquitetura")]
public sealed class PropriedadesComunsTests
{
    // Spec 01 - cobre RN-03
    [Fact]
    public void Deve_nao_redefinir_propriedades_comuns_Quando_csproj_e_lido()
    {
        var propriedades = new[] { "TargetFramework", "Nullable", "ImplicitUsings", "TreatWarningsAsErrors" };

        foreach (var caminho in Repositorio.ListarProjetos())
        {
            var documento = Repositorio.CarregarProjeto(caminho);
            foreach (var propriedade in propriedades)
                documento.Descendants(propriedade).ShouldBeEmpty();
        }
    }
}
