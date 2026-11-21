using System.Xml.Linq;
using Shouldly;

namespace MotorCredito.Arquitetura.Tests;

[Trait("Category", "Arquitetura")]
public sealed class SolutionTests
{
    // Spec 01 - cobre RN-01, RN-05, CA-04
    [Fact]
    public void Deve_conter_todos_os_projetos_de_src_e_tests_Quando_solution_e_lida()
    {
        var raiz = Repositorio.LocalizarRaiz();
        var solution = XDocument.Load(Path.Combine(raiz.FullName, "MotorAnaliseCredito.slnx"));
        var projetos = solution.Descendants("Project")
            .Select(projeto => projeto.Attribute("Path")?.Value)
            .Where(caminho => caminho is not null)
            .Select(caminho => caminho!.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar))
            .ToArray();

        projetos.Length.ShouldBe(11);
        projetos.Count(caminho => caminho.StartsWith($"src{Path.DirectorySeparatorChar}", StringComparison.Ordinal)).ShouldBe(6);
        projetos.Count(caminho => caminho.StartsWith($"tests{Path.DirectorySeparatorChar}", StringComparison.Ordinal)).ShouldBe(5);

        var projetosDaSolution = projetos
            .Select(caminho => Path.GetFullPath(Path.Combine(raiz.FullName, caminho)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var projetosDoDisco = Repositorio.ListarProjetos()
            .Where(caminho =>
                caminho.StartsWith(Path.Combine(raiz.FullName, "src") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || caminho.StartsWith(Path.Combine(raiz.FullName, "tests") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            .Select(caminho => Path.GetFullPath(caminho))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        projetosDaSolution.SetEquals(projetosDoDisco).ShouldBeTrue();
    }
}
