using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace CorrigeTesCours.Api.Ai;

/// <summary>Erreur non récupérable côté IA (down, quota, réponse invalide) — traduite en 502 par les contrôleurs.</summary>
public class AiUnavailableException : Exception
{
    public AiUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}

public interface IAiClient
{
    /// <summary>Envoie un prompt système + utilisateur et retourne un objet JSON désérialisé de type T.</summary>
    Task<T> CompleteJsonAsync<T>(string systemPrompt, string userPrompt, CancellationToken ct);
}

public class OpenRouterAiClient : IAiClient
{
    private readonly HttpClient _http;
    private readonly AiOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public OpenRouterAiClient(HttpClient http, IOptions<AiOptions> options)
    {
        _options = options.Value;
        _http = http;
        _http.BaseAddress = new Uri(_options.BaseUrl);
        _http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        _http.DefaultRequestHeaders.Add("HTTP-Referer", "https://corrige-tes-cours.app");
        _http.DefaultRequestHeaders.Add("X-Title", "Corrige tes cours");
    }

    public async Task<T> CompleteJsonAsync<T>(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        var payload = new
        {
            model = _options.Model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            response_format = new { type = "json_object" },
            temperature = 0.4
        };

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync("chat/completions", payload, JsonOptions, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new AiUnavailableException("Le service IA est injoignable pour le moment.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new AiUnavailableException($"Le service IA a répondu avec une erreur ({(int)response.StatusCode}).");
        }

        var completion = await response.Content.ReadFromJsonAsync<ChatCompletion>(JsonOptions, ct);
        var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
            throw new AiUnavailableException("Réponse IA vide.");

        try
        {
            return JsonSerializer.Deserialize<T>(content, JsonOptions)
                   ?? throw new AiUnavailableException("Réponse IA invalide.");
        }
        catch (JsonException ex)
        {
            throw new AiUnavailableException("La réponse IA n'a pas pu être interprétée.", ex);
        }
    }

    private class ChatCompletion
    {
        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }
    }

    private class Choice
    {
        [JsonPropertyName("message")]
        public Message? Message { get; set; }
    }

    private class Message
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
