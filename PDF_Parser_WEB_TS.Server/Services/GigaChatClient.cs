using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace PDF_Parser_WEB_TS.Server.Services;

public class GigaChatClient : IGigaChatClient
{
    private readonly HttpClient _httpClient;
    private readonly GigaChatOptions _options;

    public GigaChatClient(HttpClient httpClient, IOptions<GigaChatOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<GigaChatResult> GetStructuredJsonAsync(string plainText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return new GigaChatResult("{}", "Текст документа пуст, описание не сформировано.");
        }

        var maxChars = 12000;
        var truncated = plainText.Length > maxChars ? plainText[..maxChars] : plainText;

        var token = await GetAccessTokenAsync(cancellationToken);

        var descriptionPrompt =
            "Ты эксперт по анализу документов. На вход ты получаешь ПОЛНЫЙ текст PDF-документа. " +
            "Сформируй ОЧЕНЬ ПОДРОБНОЕ, РАЗВЁРНУТОЕ текстовое описание этого документа на русском языке. " +
            "Описание должно быть ДЛИННЫМ (ориентируйся минимум на 1500–2000 слов) и охватывать КАЖДЫЙ важный фрагмент текста. " +
            "Обязательно:\n" +
            "1) Подробно опиши общую цель и назначение документа.\n" +
            "2) Перечисли ВСЕ разделы и подпункты, для каждого сделай небольшой пересказ, НЕ пропускай пункты.\n" +
            "3) Развёрнуто распиши обязанности, права, ограничения и ответственность сторон.\n" +
            "4) Опиши требования к условиям труда, режиму работы, внешнему виду, санитарным нормам, технике безопасности и т.п.\n" +
            "5) Укажи все важные числовые параметры: сроки, суммы, проценты, интервалы времени, штрафы, уровни ответственности и т.д.\n" +
            "6) Опиши, какие действия должен выполнять сотрудник в типичных рабочих ситуациях, приведённых в документе.\n" +
            "7) Если есть разделы про ответственность, дисциплинарные меры, порядок увольнения — опиши их максимально подробно.\n" +
            "Пиши в виде обычного связного текста с абзацами, НЕ используй формат JSON и не оформляй ответ как блок кода.";

        var description = await SendChatRequestAsync(token, descriptionPrompt, truncated, cancellationToken);

        if (string.IsNullOrWhiteSpace(description))
        {
            // Если GigaChat не дал описания — дальше нет смысла пытаться строить JSON.
            return new GigaChatResult("{}", string.Empty);
        }

        var jsonPrompt =
            "На вход ты получаешь ПОДРОБНОЕ текстовое описание документа. " +
            "Преобразуй его в структурированный JSON, подходящий для дальнейшей обработки программой. " +
            "Верни ТОЛЬКО ОДИН JSON‑объект без комментариев и объяснений. ";

        var jsonRaw = await SendChatRequestAsync(token, jsonPrompt, description, cancellationToken);

        if (string.IsNullOrWhiteSpace(jsonRaw))
        {
            return new GigaChatResult("{}", description);
        }

        string structuredJson;
        try
        {
            using var doc = JsonDocument.Parse(jsonRaw);
            structuredJson = JsonSerializer.Serialize(doc.RootElement,
                new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            structuredJson = jsonRaw;
        }

        return new GigaChatResult(structuredJson, description);
    }

    private async Task<string> SendChatRequestAsync(string token, string systemPrompt, string userContent, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.ApiUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new ChatRequest
        {
            Model = _options.Model,
            Messages =
            [
                new ChatMessage { Role = "system", Content = systemPrompt },
                new ChatMessage { Role = "user", Content = userContent }
            ]
        };

        var json = JsonSerializer.Serialize(payload);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var chatResponse = await JsonSerializer.DeserializeAsync<ChatResponse>(stream, cancellationToken: cancellationToken);

        return chatResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.AuthUrl);
        request.Headers.Authorization = AuthenticationHeaderValue.Parse(_options.Authorization);
        request.Headers.Add("RqUID", Guid.NewGuid().ToString());

        request.Content = new StringContent($"scope={_options.Scope}", Encoding.UTF8, "application/x-www-form-urlencoded");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var tokenResponse = await JsonSerializer.DeserializeAsync<TokenResponse>(stream, cancellationToken: cancellationToken);

        if (tokenResponse is null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("Не удалось получить токен доступа GigaChat.");
        }

        return tokenResponse.AccessToken;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
    }

    private sealed class ChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "GigaChat";

        [JsonPropertyName("messages")]
        public List<ChatMessage> Messages { get; set; } = new();
    }

    private sealed class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class ChatResponse
    {
        [JsonPropertyName("choices")]
        public List<ChatChoice> Choices { get; set; } = new();
    }

    private sealed class ChatChoice
    {
        [JsonPropertyName("message")]
        public ChatMessage? Message { get; set; }
    }
}


