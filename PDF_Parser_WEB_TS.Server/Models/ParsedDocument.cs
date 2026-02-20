namespace PDF_Parser_WEB_TS.Server.Models;

public class ParsedDocument
{
    public int Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// </summary>
    public string FullText { get; set; } = string.Empty;

    /// <summary>
    /// </summary>
    public string StructuredJson { get; set; } = string.Empty;

    /// <summary>
    /// </summary>
    public string? AiSummary { get; set; }
}


