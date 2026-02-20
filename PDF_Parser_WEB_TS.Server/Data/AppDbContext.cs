using Microsoft.EntityFrameworkCore;
using PDF_Parser_WEB_TS.Server.Models;

namespace PDF_Parser_WEB_TS.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ParsedDocument> ParsedDocuments => Set<ParsedDocument>();
}


