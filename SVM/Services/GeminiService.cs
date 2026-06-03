using System.Text;
using System.Text.Json;

namespace SVM.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiService(IConfiguration config)
        {
            _httpClient = new HttpClient();
            _apiKey = config["Gemini:ApiKey"]
                ?? throw new Exception("Gemini API key missing in appsettings.json");
        }

        public async Task<string> GetChatResponseAsync(string prompt, List<ChatMessage> history)
        {
            try
            {
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={_apiKey}";
                var contents = new List<object>();

                foreach (var msg in history)
                {
                    contents.Add(new
                    {
                        role = msg.Role == "user" ? "user" : "model",
                        parts = new[] { new { text = msg.Content } }
                    });
                }

                contents.Add(new
                {
                    role = "user",
                    parts = new[] { new { text = prompt } }
                });

                var requestBody = new
                {
                    contents = contents,
                    systemInstruction = new
                    {
                        parts = new[] { new { text = "You are SVM AI Assistant. Help students with school queries (attendance, timetable, ID card, notices, profile). Answer politely and concisely." } }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return $"AI service error: {response.StatusCode} - {responseJson}";

                using var doc = JsonDocument.Parse(responseJson);
                var reply = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return reply ?? "No response from AI.";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}