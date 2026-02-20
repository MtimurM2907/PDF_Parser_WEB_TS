using System.Security.Cryptography.X509Certificates;
using PDF_Parser_WEB_TS.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins("https://localhost:53671")
                .AllowAnyHeader()
                .AllowAnyMethod());
    }
    else
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
            ?? new[] { "https://yourdomain.com" };
        
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod());
    }
});

builder.Services.Configure<GigaChatOptions>(builder.Configuration.GetSection(GigaChatOptions.SectionName));

builder.Services.AddHttpClient<IGigaChatClient, GigaChatClient>()
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var certPath = Path.Combine(AppContext.BaseDirectory, "certs", "russian_trusted_root_ca_pem.crt");
        
        var handler = new HttpClientHandler();
        
        if (File.Exists(certPath))
        {
            try
            {
                var rootCert = X509Certificate2.CreateFromPemFile(certPath);
                
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    if (errors == System.Net.Security.SslPolicyErrors.None)
                        return true;
                    
                    var chain2 = new X509Chain();
                    chain2.ChainPolicy.ExtraStore.Add(rootCert);
                    chain2.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    chain2.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                    
                    if (cert != null && chain2.Build(new X509Certificate2(cert)))
                    {
                        if (chain2.ChainElements.Count > 0)
                        {
                            var root = chain2.ChainElements[chain2.ChainElements.Count - 1].Certificate;
                            if (root.Thumbprint == rootCert.Thumbprint)
                                return true;
                        }
                    }
                    
                    return false;
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не удалось загрузить сертификат Минцифры: {ex.Message}");
            }
        }
        
        return handler;
    });

builder.Services.AddScoped<IPdfParserService, PdfParserService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
