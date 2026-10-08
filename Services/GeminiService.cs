using InventarioAPI.Data;
using InventarioAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace InventarioAPI.Services
{
    public class GeminiService
    {
        // Single source of truth: the prompt and the tool declaration must use the same name.
        private const string StockFunctionName = "get_stock";

        private const string SystemPrompt =
            "You are an Inventory assistant for a store. " +
            "For any question about how many units of a product are in stock, call the " + StockFunctionName + " function; never guess stock numbers. " +
            "If the product is not found, say so and ask the user to check the name. " +
            "Always reply in the same language as the user's latest message. " +
            "Keep answers short.";

        private const int MaxAttempts = 4;

        private static readonly object SystemInstruction =
            new { parts = new[] { new { text = SystemPrompt } } };

        private static readonly object[] Tools =
        {
            new
            {
                function_declarations = new[]
                {
                    new
                    {
                        name = StockFunctionName,
                        description = "Use this when the user asks how much stock is available for a product, or how many units are left.",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                productName = new
                                {
                                    type = "string",
                                    description = "The name of the product to look up"
                                }
                            },
                            required = new[] { "productName" }
                        }
                    }
                }
            }
        };

        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;
        
        private readonly AppDbContext _context;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(HttpClient httpClient, IConfiguration configuration, AppDbContext context, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"]
                ?? throw new InvalidOperationException("Gemini:ApiKey is not configured. Set it with dotnet user-secrets.");
            _model = configuration["Gemini:Model"] ?? "gemini-3.6-flash";
            
            _context = context;
            _logger = logger;
            
            _logger.LogInformation("Gemini model in use: {Model}", _model);
        }

public async Task<string> SendMessages(List<Message> history)
{
    var contents = history
        .Select(m => (object)new { role = m.Role, parts = new[] { new { text = m.Content } } })
        .ToList();

    const int MaxRounds = 4;

    for (int round = 0; round < MaxRounds; round++)
    {
        using var doc = await CallGeminiAsync(new
        {
            system_instruction = SystemInstruction,
            contents,
            tools = Tools
        });

        var part = FirstPart(doc);

        if (!part.TryGetProperty("functionCall", out JsonElement functionCall))
        {
            if (part.TryGetProperty("text", out JsonElement text))
                return text.GetString() ?? "";

            _logger.LogWarning("Gemini returned a part without text or functionCall: {Json}",
                doc.RootElement.GetRawText());
            throw new InvalidOperationException("Gemini returned an unexpected response.");
        }

        string functionName = functionCall.GetProperty("name").GetString() ?? "";
        if (functionName != StockFunctionName)
            throw new InvalidOperationException($"Gemini requested an unknown function: {functionName}");

        string productName = functionCall.GetProperty("args").GetProperty("productName").GetString() ?? "";
        int? stock = await GetStock(productName);

        object functionResult = stock is null
            ? (object)new { error = "Product not found" }
            : new { stock = stock.Value };

        // Clone: the JsonDocument is disposed at the end of each iteration
        contents.Add(new { role = "model", parts = new object[] { part.Clone() } });
        contents.Add(new
        {
            role = "user",
            parts = new object[]
            {
                new { functionResponse = new { name = functionName, response = functionResult } }
            }
        });
    }

    throw new InvalidOperationException($"Gemini kept requesting functions after {MaxRounds} rounds.");
}

        private static JsonElement FirstPart(JsonDocument doc) =>
            doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0];

        // An HttpRequestMessage cannot be resent, so a new one is built on every attempt.
        private async Task<JsonDocument> CallGeminiAsync(object body)
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent";
            string json = JsonSerializer.Serialize(body);

            for (var attempt = 1; ; attempt++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("x-goog-api-key", _apiKey);

                using var response = await _httpClient.SendAsync(request);
                string responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return JsonDocument.Parse(responseString);

                int status = (int)response.StatusCode;
                bool isTransient = status is 429 or 503 or 504;

                if (!isTransient || attempt == MaxAttempts)
                {
                    // StatusCode is passed along so the controller can tell 503/429 apart from other errors.
                    throw new HttpRequestException(
                        $"Gemini error ({status}): {responseString}", null, response.StatusCode);
                }

                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt))
                          + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500));
                _logger.LogWarning("Gemini returned {Status}, retry {Attempt}/{Max} in {Delay:F1}s",
                    status, attempt, MaxAttempts, delay.TotalSeconds);
                await Task.Delay(delay);
            }
        }

        private async Task<int?> GetStock(string productName)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Name == productName);
            return product?.Stock;
        }
    }
}