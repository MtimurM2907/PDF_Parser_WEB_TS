using PDF_Parser_WEB_TS.Server.Models;
using Tesseract;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PDF_Parser_WEB_TS.Server.Services;

public class PdfParserService : IPdfParserService
{
    private readonly ILogger<PdfParserService> _logger;
    private readonly IGigaChatClient _gigaChatClient;

    public PdfParserService(ILogger<PdfParserService> logger, IGigaChatClient gigaChatClient)
    {
        _logger = logger;
        _gigaChatClient = gigaChatClient;
    }

    public async Task<ParsedDocument> ParseAndSaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
        {
            throw new InvalidOperationException("Пустой файл.");
        }

        await using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        string fullText;
        List<PageText> pages = new();

        try
        {
            using var pdf = PdfDocument.Open(memoryStream);

            foreach (var page in pdf.GetPages())
            {
                var pageText = page.Text;
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    pages.Add(new PageText(page.Number, pageText));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при чтении PDF через PdfPig.");
        }

        if (pages.Count == 0)
        {
            _logger.LogInformation("Текст не найден");
            fullText = string.Empty;
        }
        else
        {
            fullText = string.Join(Environment.NewLine + Environment.NewLine,
                pages.OrderBy(p => p.PageNumber).Select(p => p.Text));
        }

        string structuredJson;
        string aiSummary;
        try
        {
            var gigaResult = await _gigaChatClient.GetStructuredJsonAsync(fullText, cancellationToken);
            structuredJson = gigaResult.StructuredJson;
            aiSummary = string.IsNullOrWhiteSpace(gigaResult.HumanReadable)
                ? BuildFallbackSummary(fullText)
                : gigaResult.HumanReadable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обращении к GigaChat. Используется резервная структура JSON.");

            var fallback = new
            {
                fileName = file.FileName,
                pages = pages.OrderBy(p => p.PageNumber)
                    .Select(p => new { pageNumber = p.PageNumber, text = p.Text })
                    .ToList(),
                error = "GigaChat call failed, fallback structure is used."
            };

            structuredJson = System.Text.Json.JsonSerializer.Serialize(fallback,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            aiSummary = "GigaChat недоступен, показана резервная структура по страницам без AI-описания.";
        }

        var entity = new ParsedDocument
        {
            FileName = file.FileName,
            FullText = fullText,
            StructuredJson = structuredJson,
            AiSummary = aiSummary,
            UploadedAt = DateTime.UtcNow
        };

        return entity;
    }

    private static string BuildFallbackSummary(string fullText)
    {
        if (string.IsNullOrWhiteSpace(fullText))
        {
            return "Описание не получено и текст документа пуст или не распознан.";
        }

        var maxLength = 600;
        var normalized = fullText.Replace("\r\n", " ").Replace("\n", " ");

        if (normalized.Length <= maxLength)
        {
            return normalized;
        }

        var cut = normalized[..maxLength];
        var lastDot = cut.LastIndexOfAny(new[] { '.', '!', '?' });
        if (lastDot > 100)
        {
            cut = cut[..(lastDot + 1)];
        }

        return cut + " …";
    }

    private readonly record struct PageText(int PageNumber, string Text);
}


