using System.Globalization;
using System.Text.Encodings.Web;
using crudEbancoDist8.Interfaces;
using crudEbancoDist8.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace crudEbancoDist8.Controllers;

[ApiController]
public sealed class CompartilhamentoController(
    IBebidasService bebidasService,
    IOptions<CompartilhamentoOptions> options) : ControllerBase
{
    private const string NomeSite = "Código dos Bares";
    private const string RotaProduto = "/compartilhar/produto";

    [AllowAnonymous]
    [HttpGet(RotaProduto + "/{id:int}")]
    public async Task<IActionResult> CompartilharProduto(int id)
    {
        var produto = await bebidasService.GetByIdAsync(id);
        if (produto is null)
        {
            return NotFound();
        }

        var configuracao = options.Value;
        var produtoId = produto.Id.ToString(CultureInfo.InvariantCulture);
        var urlCompartilhamento = new Uri(new Uri(configuracao.ApiBaseUrl), $"{RotaProduto}/{produtoId}");
        var urlProduto = new Uri(new Uri(configuracao.FrontendBaseUrl), $"/produto/{produtoId}");
        var html = HtmlEncoder.Default;
        var titulo = html.Encode($"{produto.Name} | {NomeSite}");
        var descricao = html.Encode(ResumirDescricao(produto.Description));
        var nomeSite = html.Encode(NomeSite);
        var imagem = Uri.TryCreate(produto.ImageUrl, UriKind.Absolute, out var urlImagem) &&
                     urlImagem.Scheme == Uri.UriSchemeHttps && urlImagem.UserInfo.Length == 0
            ? $"""<meta property="og:image" content="{html.Encode(urlImagem.AbsoluteUri)}">"""
            : "";

        return Content($"""
            <!doctype html>
            <html lang="pt-BR">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>{titulo}</title>
                <meta name="description" content="{descricao}">
                <meta property="og:type" content="product">
                <meta property="og:title" content="{titulo}">
                <meta property="og:description" content="{descricao}">
                {imagem}
                <meta property="og:url" content="{html.Encode(urlCompartilhamento.AbsoluteUri)}">
                <meta property="og:site_name" content="{nomeSite}">
                <meta property="og:locale" content="pt_BR">
                <meta name="twitter:card" content="summary_large_image">
            </head>
            <body>
                <a id="produto-link" href="{html.Encode(urlProduto.AbsoluteUri)}">Ver produto no catálogo</a>
                <script>
                    window.location.replace(document.getElementById("produto-link").href);
                </script>
            </body>
            </html>
            """, "text/html; charset=utf-8");
    }

    private static string ResumirDescricao(string? descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao))
        {
            return $"Veja o preço e a disponibilidade desta bebida no catálogo do {NomeSite}.";
        }

        const int limite = 160;
        var texto = string.Join(" ", descricao.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        var elementos = StringInfo.ParseCombiningCharacters(texto);
        return elementos.Length > limite
            ? texto[..elementos[limite - 1]].TrimEnd() + "…"
            : texto;
    }
}
