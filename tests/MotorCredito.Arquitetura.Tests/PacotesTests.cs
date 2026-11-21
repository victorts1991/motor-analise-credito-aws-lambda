using System.Xml.Linq;
using Shouldly;

namespace MotorCredito.Arquitetura.Tests;

[Trait("Category", "Arquitetura")]
public sealed class PacotesTests
{
    // Spec 01 - cobre RN-09
    [Fact]
    public void Deve_nao_usar_pacote_proibido_Quando_qualquer_projeto_ou_props_e_lido()
    {
        foreach (var caminho in Repositorio.ListarProjetos())
        {
            var documento = Repositorio.CarregarProjeto(caminho);
            var pacotes = documento.Descendants()
                .Where(elemento => elemento.Name.LocalName is "PackageReference" or "PackageVersion")
                .Select(elemento => elemento.Attribute("Include")?.Value)
                .Where(nome => nome is not null)
                .ToArray();

            pacotes.ShouldNotContain(nome => PacotesProibidos.Lista.Contains(nome, StringComparer.OrdinalIgnoreCase));
        }

        var raiz = Repositorio.LocalizarRaiz();
        var props = XDocument.Load(Path.Combine(raiz.FullName, "Directory.Packages.props"));
        var pacotesDosProps = props.Descendants("PackageVersion")
            .Select(pacote => pacote.Attribute("Include")?.Value)
            .Where(nome => nome is not null)
            .ToArray();

        pacotesDosProps.ShouldNotContain(nome => PacotesProibidos.Lista.Contains(nome, StringComparer.OrdinalIgnoreCase));
    }

    // Spec 01 - cobre RN-04
    [Fact]
    public void Deve_nao_informar_versao_Quando_csproj_referencia_pacote()
    {
        foreach (var caminho in Repositorio.ListarProjetos())
        {
            var documento = Repositorio.CarregarProjeto(caminho);
            foreach (var referencia in documento.Descendants("PackageReference"))
                referencia.Attribute("Version").ShouldBeNull();
        }
    }

    // Spec 01 - cobre RN-04
    [Fact]
    public void Deve_ter_versao_central_Quando_pacote_e_referenciado()
    {
        var raiz = Repositorio.LocalizarRaiz();
        var props = XDocument.Load(Path.Combine(raiz.FullName, "Directory.Packages.props"));
        var centralizados = props.Descendants("PackageVersion")
            .Select(pacote => pacote.Attribute("Include")?.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var caminho in Repositorio.ListarProjetos())
        {
            var documento = Repositorio.CarregarProjeto(caminho);
            foreach (var referencia in documento.Descendants("PackageReference"))
            {
                var nome = referencia.Attribute("Include")?.Value;
                nome.ShouldNotBeNull();
                centralizados.ShouldContain(nome);
            }
        }
    }
}
