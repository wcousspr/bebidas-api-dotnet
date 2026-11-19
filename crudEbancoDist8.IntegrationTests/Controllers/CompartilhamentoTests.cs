using System.Net;
using System.Text.RegularExpressions;
using crudEbancoDist8.DTOs;
using crudEbancoDist8.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace crudEbancoDist8.IntegrationTests.Controllers;

public sealed class CompartilhamentoTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Rota = "/compartilhar/produto/42";

    [Fact]
    public async Task ProdutoExistente_RetornaHtmlPublicoComMetadadosEDestino()
    {
        var produto = new ReadBebidaDto
        {
            Id = 42,
            Name = "Água com gás",
            Description = "  Bebida\nrefrescante.  ",
            ImageUrl = "https://res.cloudinary.com/catalogo/image/upload/agua.jpg?a=1&b=2"
        };
        using var host = ConfigurarApi(produto);
        using var cliente = CriarCliente(host);

        using var resposta = await cliente.GetAsync(Rota + "?origem=whatsapp");
        var html = await resposta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Null(resposta.Headers.Location);
        Assert.Equal("text/html", resposta.Content.Headers.ContentType?.MediaType);
        Assert.Equal("utf-8", resposta.Content.Headers.ContentType?.CharSet);
        Assert.Equal("product", LerMeta(html, "property", "og:type"));
        Assert.Equal("Água com gás | Código dos Bares", LerMeta(html, "property", "og:title"));
        Assert.Contains("<title>Água com gás | Código dos Bares</title>", WebUtility.HtmlDecode(html));
        Assert.Equal("Bebida refrescante.", LerMeta(html, "property", "og:description"));
        Assert.Equal("Bebida refrescante.", LerMeta(html, "name", "description"));
        Assert.Equal(produto.ImageUrl, LerMeta(html, "property", "og:image"));
        Assert.Contains("a=1&amp;b=2", html);
        Assert.Equal("https://api.example.test" + Rota, LerMeta(html, "property", "og:url"));
        Assert.Equal("Código dos Bares", LerMeta(html, "property", "og:site_name"));
        Assert.Equal("pt_BR", LerMeta(html, "property", "og:locale"));
        Assert.Equal("summary_large_image", LerMeta(html, "name", "twitter:card"));
        Assert.Contains("""<a id="produto-link" href="https://loja.example.test/produto/42">""", html);
        Assert.Contains("""window.location.replace(document.getElementById("produto-link").href);""", html);
    }

    [Fact]
    public async Task DadosDoProduto_ComMarcacao_SaoTratadosComoTexto()
    {
        var produto = new ReadBebidaDto
        {
            Id = 42,
            Name = """Bebida " & </title><script>alert(1)</script>""",
            Description = """Descrição "><img src=x onerror="alert(2)"> & texto"""
        };
        using var host = ConfigurarApi(produto);
        using var cliente = CriarCliente(host);

        var html = await cliente.GetStringAsync(Rota);

        Assert.Equal(produto.Name + " | Código dos Bares", LerMeta(html, "property", "og:title"));
        Assert.Equal(produto.Description, LerMeta(html, "property", "og:description"));
        Assert.DoesNotContain("</title><script>", html);
        Assert.DoesNotContain("<img", html);
        Assert.Contains("&quot;", html);
        Assert.Contains("&amp;", html);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("http://res.cloudinary.com/catalogo/image/upload/produto.jpg")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://usuario:segredo@exemplo.test/produto.jpg")]
    public async Task SemDescricaoOuImagemHttps_UsaDescricaoPadraoEOmiteImagem(string? imagem)
    {
        using var host = ConfigurarApi(new ReadBebidaDto
        {
            Id = 42, Name = "Bebida", Description = "  ", ImageUrl = imagem
        }, usarUrlsLocais: true);
        using var cliente = CriarCliente(host);

        var html = await cliente.GetStringAsync(Rota);

        Assert.Contains("Veja o preço e a disponibilidade", LerMeta(html, "property", "og:description"));
        Assert.DoesNotContain("og:image", html);
        Assert.Equal("https://localhost:7113" + Rota, LerMeta(html, "property", "og:url"));
        Assert.Contains("""<a id="produto-link" href="http://localhost:5173/produto/42">""", html);
    }

    [Fact]
    public async Task DescricaoLonga_ResumeSemDividirEmoji()
    {
        var inicio = new string('a', 158);
        using var host = ConfigurarApi(new ReadBebidaDto
        {
            Id = 42, Name = "Bebida", Description = inicio + "🥤🏷️ restante"
        });
        using var cliente = CriarCliente(host);

        var html = await cliente.GetStringAsync(Rota);

        Assert.Equal(inicio + "🥤…", LerMeta(html, "property", "og:description"));
    }

    [Fact]
    public async Task ProdutoInexistente_Retorna404()
    {
        using var host = ConfigurarApi(null);
        using var cliente = CriarCliente(host);

        using var resposta = await cliente.GetAsync(Rota);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    private WebApplicationFactory<Program> ConfigurarApi(
        ReadBebidaDto? produto, bool usarUrlsLocais = false) =>
        factory.WithWebHostBuilder(builder =>
        {
            if (!usarUrlsLocais)
            {
                builder.UseSetting("Compartilhamento:ApiBaseUrl", "https://api.example.test");
                builder.UseSetting("Compartilhamento:FrontendBaseUrl", "https://loja.example.test");
            }

            builder.ConfigureTestServices(services =>
            {
                var bebidas = new Mock<IBebidasService>(MockBehavior.Strict);
                bebidas.Setup(service => service.GetByIdAsync(42)).ReturnsAsync(produto);
                services.RemoveAll<IBebidasService>();
                services.AddScoped(_ => bebidas.Object);
            });
        });

    private static HttpClient CriarCliente(WebApplicationFactory<Program> host) =>
        host.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://host-da-requisicao.example.test"),
            AllowAutoRedirect = false
        });

    private static string LerMeta(string html, string atributo, string nome)
    {
        var meta = Regex.Match(html, $"""<meta {atributo}="{Regex.Escape(nome)}" content="([^"]*)">""");
        Assert.True(meta.Success, $"Meta tag ausente ou inválida: {nome}");
        return WebUtility.HtmlDecode(meta.Groups[1].Value);
    }
}
