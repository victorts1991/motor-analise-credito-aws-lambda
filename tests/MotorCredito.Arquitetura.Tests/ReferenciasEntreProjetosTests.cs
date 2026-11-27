using System.Xml.Linq;
using Shouldly;

namespace MotorCredito.Arquitetura.Tests;

[Trait("Category", "Arquitetura")]
public sealed class ReferenciasEntreProjetosTests
{
    // Spec 01 - cobre RN-05
    [Fact]
    public void Deve_nao_referenciar_nenhum_projeto_nem_pacote_Quando_projeto_e_Dominio()
    {
        var projeto = EncontrarProjeto("MotorCredito.Dominio");
        var documento = Repositorio.CarregarProjeto(projeto);

        documento.Descendants("ProjectReference").ShouldBeEmpty();
        documento.Descendants("PackageReference").ShouldBeEmpty();
    }

    // Spec 02 - cobre RN-13
    [Fact]
    public void Deve_referenciar_apenas_Dominio_Quando_projeto_e_Dominio_Tests()
    {
        var projeto = EncontrarProjeto("MotorCredito.Dominio.Tests");
        var documento = Repositorio.CarregarProjeto(projeto);
        var referencias = documento.ReferenceNames("ProjectReference");

        referencias.ShouldBe(["MotorCredito.Dominio"]);
    }

    // Spec 01 - cobre RN-05
    [Fact]
    public void Deve_referenciar_apenas_Dominio_Quando_projeto_e_Infraestrutura()
    {
        var projeto = EncontrarProjeto("MotorCredito.Infraestrutura");
        var documento = Repositorio.CarregarProjeto(projeto);
        var referencias = documento.ReferenceNames("ProjectReference");

        referencias.ShouldBe(["MotorCredito.Dominio"]);
    }

    // Spec 01 - cobre RN-05
    [Fact]
    public void Deve_referenciar_apenas_Dominio_Quando_projeto_e_MotorRegrasScore()
    {
        var projeto = EncontrarProjeto("MotorCredito.Lambda.MotorRegrasScore");
        var documento = Repositorio.CarregarProjeto(projeto);
        var referencias = documento.ReferenceNames("ProjectReference");

        referencias.ShouldBe(["MotorCredito.Dominio"]);
    }

    // Spec 01 - cobre RN-05
    [Theory]
    [InlineData("MotorCredito.Lambda.MotorRegrasScore")]
    [InlineData("MotorCredito.Lambda.ProcessadorProposta")]
    [InlineData("MotorCredito.Lambda.DespachanteNotificacao")]
    [InlineData("MotorCredito.Lambda.ConsultaProposta")]
    public void Deve_nao_referenciar_outra_Lambda_Quando_projeto_e_Lambda(string nomeProjeto)
    {
        var projeto = EncontrarProjeto(nomeProjeto);
        var documento = Repositorio.CarregarProjeto(projeto);
        var referencias = documento.Descendants("ProjectReference")
            .Select(referencia => referencia.Attribute("Include")?.Value)
            .Where(caminho => caminho is not null)
            .Select(caminho => Path.GetFileNameWithoutExtension(caminho!.Replace('\\', '/')))
            .ToArray();

        referencias.ShouldNotContain(caminho => caminho.StartsWith("MotorCredito.Lambda.", StringComparison.Ordinal));
        referencias.ShouldContain("MotorCredito.Dominio");
    }

    private static string EncontrarProjeto(string nomeProjeto)
    {
        return Repositorio.ListarProjetos()
            .Single(caminho => Path.GetFileNameWithoutExtension(caminho) == nomeProjeto);
    }
}

internal static class XmlExtensions
{
    public static string[] ReferenceNames(this XDocument documento, string elemento)
    {
        return documento.Descendants(elemento)
            .Select(referencia => referencia.Attribute("Include")?.Value)
            .Where(caminho => caminho is not null)
            .Select(caminho => NomeProjeto(caminho!))
            .ToArray();
    }

    private static string NomeProjeto(string caminho)
    {
        var separado = caminho.Replace('\\', '/');
        return Path.GetFileNameWithoutExtension(separado);
    }
}
