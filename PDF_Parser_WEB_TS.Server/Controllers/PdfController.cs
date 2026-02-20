using Microsoft.AspNetCore.Mvc;
using PDF_Parser_WEB_TS.Server.Models;
using PDF_Parser_WEB_TS.Server.Services;

namespace PDF_Parser_WEB_TS.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PdfController : ControllerBase
{
    private readonly IPdfParserService _parserService;

    public PdfController(IPdfParserService parserService)
    {
        _parserService = parserService;
    }

    /// <summary>
    /// </summary>
    [HttpPost("parse")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<ActionResult<ParsedDocument>> Parse([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Некорректные данные запроса.");
        }

        if (file == null)
        {
            return BadRequest("Файл не передан.");
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Ожидается PDF-файл.");
        }

        try
        {
            var result = await _parserService.ParseAndSaveAsync(file, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при парсинге PDF: {ex}");
            return StatusCode(StatusCodes.Status500InternalServerError, ex.ToString());
        }
    }

}


