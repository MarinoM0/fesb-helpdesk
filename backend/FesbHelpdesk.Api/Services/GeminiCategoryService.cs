using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FesbHelpdesk.Api.Services;

public class GeminiCategoryService : IGeminiCategoryService
{
    private const string FallbackCategory = "Ostalo";
    private const string Model = "gemini-2.5-flash";

    private readonly HttpClient _http;
    private readonly ILogger<GeminiCategoryService> _logger;
    private readonly string _apiKey;

    public GeminiCategoryService(HttpClient http, IConfiguration config, ILogger<GeminiCategoryService> logger)
    {
        _http = http;
        _logger = logger;
        _apiKey = config["Gemini:ApiKey"] ?? string.Empty;
    }

    public async Task<string> ClassifyAsync(string title, string description, IEnumerable<string> categories, CancellationToken ct = default)
    {
        var allowedCategories = NormalizeCategories(categories);

        if (allowedCategories.Count == 0)
        {
            return FallbackCategory;
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("GEMINI_API_KEY missing — returning fallback category");
            return FallbackCategory;
        }

        try
        {
            var prompt = BuildPrompt(title, description, allowedCategories);
            var requestBody = BuildRequestBody(prompt);
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={_apiKey}";

            using var response = await _http.PostAsJsonAsync(url, requestBody, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Gemini API error {Status}: {Err}", response.StatusCode, errorBody);
                return FallbackCategory;
            }

            var rawBody = await response.Content.ReadAsStringAsync(ct);
            var parsed = ParseResponse(rawBody);

            if (parsed is null)
            {
                return FallbackCategory;
            }

            var rawText = parsed.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;
            var cleanedText = CleanupResponseText(rawText);

            var matchedCategory = FindMatchingCategory(allowedCategories, cleanedText);

            if (matchedCategory is null)
            {
                var finishReason = parsed.Candidates?.FirstOrDefault()?.FinishReason ?? "<none>";
                _logger.LogWarning(
                    "Gemini returned invalid category. Raw: '{Raw}' | finish: {Finish} | allowed: [{Allowed}] | body: {Body}",
                    rawText, finishReason, string.Join(", ", allowedCategories), rawBody);
                return FallbackCategory;
            }

            _logger.LogInformation("Gemini classified ticket as '{Category}'", matchedCategory);
            return matchedCategory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini classification failed");
            return FallbackCategory;
        }
    }

    private static List<string> NormalizeCategories(IEnumerable<string> categories)
    {
        var result = new List<string>();
        foreach (var category in categories)
        {
            var trimmed = category.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                result.Add(trimmed);
            }
        }
        return result;
    }

    private static GeminiRequest BuildRequestBody(string prompt)
    {
        return new GeminiRequest
        {
            Contents = new[]
            {
                new GeminiContent
                {
                    Parts = new[] { new GeminiPart { Text = prompt } }
                }
            },
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = 0.0,
                TopP = 0.1,
                TopK = 1,
                MaxOutputTokens = 64,
                ThinkingConfig = new GeminiThinkingConfig { ThinkingBudget = 0 }
            }
        };
    }

    private GeminiResponse? ParseResponse(string rawBody)
    {
        try
        {
            return JsonSerializer.Deserialize<GeminiResponse>(rawBody);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Gemini response JSON parse failed. Body: {Body}", rawBody);
            return null;
        }
    }

    private static string? FindMatchingCategory(List<string> allowedCategories, string cleanedText)
    {
        foreach (var category in allowedCategories)
        {
            if (string.Equals(category, cleanedText, StringComparison.OrdinalIgnoreCase))
            {
                return category;
            }
        }
        return null;
    }

    private static string BuildPrompt(string title, string description, IEnumerable<string> categories)
    {
        var categoryList = string.Join("\n", categories.Select(c => $"- {c}"));
        return $@"Ti si asistent koji klasificira studentske upite FESB-a u JEDNU kategoriju.

Dostupne kategorije (odgovor MORA biti točno jedna od ovih vrijednosti, bez ikakvog dodatnog teksta):
{categoryList}

Pravila:
- Odgovori ISKLJUČIVO imenom kategorije, bez ikakvog dodatnog teksta, bez navodnika, bez interpunkcije na kraju.
- Ako nijedna kategorija ne odgovara sadržaju, odgovori: Ostalo.

Naslov upita: {title}
Opis upita: {description}

Kategorija:";
    }

    private static string CleanupResponseText(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var text = raw.Trim();
        text = text.Trim('"', '\'', '`', '.', ',', ';', ':', '!', '?');
        text = text.Replace("\n", " ").Replace("\r", " ");

        while (text.Contains("  "))
        {
            text = text.Replace("  ", " ");
        }

        return text.Trim();
    }

    private class GeminiRequest
    {
        [JsonPropertyName("contents")] public GeminiContent[] Contents { get; set; } = Array.Empty<GeminiContent>();
        [JsonPropertyName("generationConfig")] public GeminiGenerationConfig GenerationConfig { get; set; } = new();
    }

    private class GeminiContent
    {
        [JsonPropertyName("parts")] public GeminiPart[] Parts { get; set; } = Array.Empty<GeminiPart>();
    }

    private class GeminiPart
    {
        [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;
    }

    private class GeminiGenerationConfig
    {
        [JsonPropertyName("temperature")] public double Temperature { get; set; }
        [JsonPropertyName("topP")] public double TopP { get; set; }
        [JsonPropertyName("topK")] public int TopK { get; set; }
        [JsonPropertyName("maxOutputTokens")] public int MaxOutputTokens { get; set; }
        [JsonPropertyName("thinkingConfig")] public GeminiThinkingConfig? ThinkingConfig { get; set; }
    }

    private class GeminiThinkingConfig
    {
        [JsonPropertyName("thinkingBudget")] public int ThinkingBudget { get; set; }
    }

    private class GeminiResponse
    {
        [JsonPropertyName("candidates")] public GeminiCandidate[]? Candidates { get; set; }
    }

    private class GeminiCandidate
    {
        [JsonPropertyName("content")] public GeminiContent? Content { get; set; }
        [JsonPropertyName("finishReason")] public string? FinishReason { get; set; }
    }
}
