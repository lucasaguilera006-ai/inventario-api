using InventarioAPI.Data;
using InventarioAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace InventarioAPI.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _modelo;
        private readonly AppDbContext _context;
        public GeminiService(HttpClient httpClient, IConfiguration configuration, AppDbContext context)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Falta configurar Gemini:ApiKey en user-secrets.");
            _modelo = configuration["Gemini:Modelo"] ?? "gemini-3-flash-preview";
            _context = context;
        }
        public async Task<string> EnviarMensaje(List<Mensaje> historial)
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelo}:generateContent";

            var contents = historial.Select(m => new
            {
                role = m.Rol,
                parts = new[] { new { text = m.Contenido } }
            });

            var requestBody = new
            {
                contents,
                tools = new[]
                {
            new
            {
                function_declarations = new[]
                {
                    new
                    {
                        name = "consultar_stock",
                        description = "Usar esto cuando el usuario pregunte cuánto stock hay disponible de un producto, o cuántas unidades quedan.",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                nombreProducto = new
                                {
                                    type = "string",
                                    description = "El nombre del producto a consultar"
                                }
                            },
                            required = new[] { "nombreProducto" }
                        }
                    }
                }
            }
        }
            };
            string json = JsonSerializer.Serialize(requestBody);

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-goog-api-key", _apiKey);

            var response = await _httpClient.SendAsync(request);
            string responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error de Gemini ({(int)response.StatusCode}): {responseString}");
            }

            using var doc = JsonDocument.Parse(responseString);
            var part = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0];
            if (part.TryGetProperty("functionCall", out JsonElement functionCall))
            {
                string functionName = functionCall.GetProperty("name").GetString() ?? "";
                var args = functionCall.GetProperty("args");

                if (functionName == "consultar_stock")
                {
                    string nombreProducto = args.GetProperty("nombreProducto").GetString() ?? "";
                    var stock = await ConsultarStock(nombreProducto);
                    if (stock == null)
                    {
                        var contentsConResultado = contents.Cast<object>().Concat(new object[]
                    {
                            new
                            {
                                role = "model",
                                parts = new object[] { part }
                            },
                                new
                                {
                                    role = "context",
                                    parts = new object[]
                                    {
                                        new
                                        {
                                            functionResponse = new
                                            {
                                                name = functionName,
                                                response = new { error = "Producto no encontrado" }
                                                }
                                        }
                                    }
                            }
                    });
                        var requestBody2 = new
                        {
                            contents = contentsConResultado
                        };
                        string json2 = JsonSerializer.Serialize(requestBody2);

                        var request2 = new HttpRequestMessage(HttpMethod.Post, url)
                        {
                            Content = new StringContent(json2, Encoding.UTF8, "application/json")
                        };
                        request2.Headers.Add("x-goog-api-key", _apiKey);

                        var response2 = await _httpClient.SendAsync(request2);
                        string responseString2 = await response2.Content.ReadAsStringAsync();

                        if (!response2.IsSuccessStatusCode)
                        {
                            throw new HttpRequestException($"Error de Gemini ({(int)response2.StatusCode}): {responseString2}");
                        }

                        using var doc2 = JsonDocument.Parse(responseString2);
                        var part2 = doc2.RootElement
                            .GetProperty("candidates")[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0];

                        return part2.GetProperty("text").GetString() ?? "";
                    }
                    else
                    {
                        var contentsConResultado = contents.Cast<object>().Concat(new object[]
                    {
                            new
                            {
                                role = "model",
                                parts = new object[] { part }
                            },
                                new
                                {
                                    role = "context",
                                    parts = new object[]
                                    {
                                        new
                                        {
                                            functionResponse = new
                                            {
                                                name = functionName,
                                                response = new { stock = stock}
                                                }
                                        }
                                    }
                            }
                    });
                        var requestBody2 = new
                        {
                            contents = contentsConResultado
                        };
                        string json2 = JsonSerializer.Serialize(requestBody2);

                        var request2 = new HttpRequestMessage(HttpMethod.Post, url)
                        {
                            Content = new StringContent(json2, Encoding.UTF8, "application/json")
                        };
                        request2.Headers.Add("x-goog-api-key", _apiKey);

                        var response2 = await _httpClient.SendAsync(request2);
                        string responseString2 = await response2.Content.ReadAsStringAsync();

                        if (!response2.IsSuccessStatusCode)
                        {
                            throw new HttpRequestException($"Error de Gemini ({(int)response2.StatusCode}): {responseString2}");
                        }

                        using var doc2 = JsonDocument.Parse(responseString2);
                        var part2 = doc2.RootElement
                            .GetProperty("candidates")[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0];

                        return part2.GetProperty("text").GetString() ?? "";

                    }
                }
                throw new InvalidOperationException($"Gemini pidió una función desconocida: {functionName}");   // <- FALTA ESTO
            }
            else
            {
                return part.GetProperty("text").GetString() ?? "";
            }
        }

        private async Task<int?> ConsultarStock(string nombreProducto)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Nombre == nombreProducto);
            if (producto == null)
            {
                return null;
            }
            return producto.Stock;
        }

    }
}