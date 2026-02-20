using PDF_Parser_WEB_TS.Server.Models;

namespace PDF_Parser_WEB_TS.Server.Services;

public interface IPdfParserService
{
    Task<ParsedDocument> ParseAndSaveAsync(IFormFile file, CancellationToken cancellationToken = default);
}


