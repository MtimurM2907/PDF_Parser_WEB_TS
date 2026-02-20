namespace PDF_Parser_WEB_TS.Server.Services;

/// <summary>
/// 
/// </summary>
public sealed record GigaChatResult(
    string StructuredJson,
    string HumanReadable);

public interface IGigaChatClient
{
    /// <summary>
    /// </summary>
    Task<GigaChatResult> GetStructuredJsonAsync(string plainText, CancellationToken cancellationToken = default);
}


