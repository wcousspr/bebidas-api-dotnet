namespace crudEbancoDist8.Options;

public sealed class CompartilhamentoOptions
{
    public const string SectionName = "Compartilhamento";

    public string ApiBaseUrl { get; set; } = "https://localhost:7113";
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";

    public static bool IsHttpOrigin(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme is "http" or "https") &&
        uri.UserInfo.Length == 0 &&
        uri.PathAndQuery == "/" &&
        uri.Fragment.Length == 0;
}
