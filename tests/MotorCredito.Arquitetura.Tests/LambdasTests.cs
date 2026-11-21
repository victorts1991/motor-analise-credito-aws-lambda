using System.Text.Json;
using System.Xml.Linq;
using Shouldly;

namespace MotorCredito.Arquitetura.Tests;

[Trait("Category", "Arquitetura")]
public sealed class LambdasTests
{
    // Spec 01 - cobre RN-06
    [Theory]
    [InlineData("MotorCredito.Lambda.MotorRegrasScore")]
    [InlineData("MotorCredito.Lambda.ProcessadorProposta")]
    [InlineData("MotorCredito.Lambda.DespachanteNotificacao")]
    [InlineData("MotorCredito.Lambda.ConsultaProposta")]
    public void Deve_ter_propriedades_de_Lambda_Quando_projeto_e_Lambda(string nomeProjeto)
    {
        var projeto = EncontrarProjeto(nomeProjeto);
        var documento = Repositorio.CarregarProjeto(projeto);

        documento.Descendants("AWSProjectType").Single().Value.ShouldBe("Lambda");
        documento.Descendants("GenerateRuntimeConfigurationFiles").Single().Value.ShouldBe("true");
        documento.Descendants("CopyLocalLockFileAssemblies").Single().Value.ShouldBe("true");
    }

    // Spec 01 - cobre RN-06
    [Theory]
    [InlineData("MotorCredito.Lambda.MotorRegrasScore")]
    [InlineData("MotorCredito.Lambda.ProcessadorProposta")]
    [InlineData("MotorCredito.Lambda.DespachanteNotificacao")]
    [InlineData("MotorCredito.Lambda.ConsultaProposta")]
    public void Deve_ter_runtime_dotnet10_e_handler_esperado_Quando_defaults_da_Lambda_e_lido(string nomeProjeto)
    {
        var projeto = EncontrarProjeto(nomeProjeto);
        var diretorio = Path.GetDirectoryName(projeto);
        diretorio.ShouldNotBeNull();
        var defaults = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(diretorio, "aws-lambda-tools-defaults.json")));

        defaults.RootElement.GetProperty("function-runtime").GetString().ShouldBe("dotnet10");
        defaults.RootElement.GetProperty("framework").GetString().ShouldBe("net10.0");
        defaults.RootElement.GetProperty("configuration").GetString().ShouldBe("Release");
        defaults.RootElement.GetProperty("function-handler").GetString().ShouldBe(
            $"{nomeProjeto}::{nomeProjeto}.Function::FunctionHandler");
    }

    private static string EncontrarProjeto(string nomeProjeto)
    {
        return Repositorio.ListarProjetos()
            .Single(caminho => Path.GetFileNameWithoutExtension(caminho) == nomeProjeto);
    }
}
