using System.Xml.Linq;

namespace MotorCredito.Arquitetura.Tests;

internal static class Repositorio
{
    public static DirectoryInfo LocalizarRaiz()
    {
        var diretorio = new DirectoryInfo(AppContext.BaseDirectory);

        while (diretorio is not null)
        {
            if (File.Exists(Path.Combine(diretorio.FullName, "MotorAnaliseCredito.slnx")))
                return diretorio;

            diretorio = diretorio.Parent;
        }

        throw new InvalidOperationException(
            "Não foi possível localizar MotorAnaliseCredito.slnx subindo a partir de AppContext.BaseDirectory.");
    }

    public static IReadOnlyList<string> ListarProjetos()
    {
        var raiz = LocalizarRaiz();

        return Directory.EnumerateFiles(raiz.FullName, "*.csproj", SearchOption.AllDirectories)
            .Where(caminho => !caminho.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !caminho.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();
    }

    public static XDocument CarregarProjeto(string caminho)
    {
        return XDocument.Load(caminho, LoadOptions.PreserveWhitespace);
    }
}
