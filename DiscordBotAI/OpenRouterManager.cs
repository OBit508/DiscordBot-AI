using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoelhoBot
{
    internal static class OpenRouterManager
    {
        public static HttpClient Http2;
        public static async Task<string> SendChatAsync(List<Message> messages)
        {
            try
            {
                HttpResponseMessage response = await Http2.PostAsync("https://openrouter.ai/api/v1/chat/completions", new StringContent(JsonSerializer.Serialize(new OpenRouterRequest
                {
                    Model = "mistralai/devstral-2512:free",
                    Messages = messages
                }), System.Text.Encoding.UTF8, "application/json"));
                string responseContent = await response.Content.ReadAsStringAsync();
                OpenRouterResponse result = JsonSerializer.Deserialize<OpenRouterResponse>(responseContent);
                if (result?.Choices != null && result.Choices.Count > 0)
                {
                    return result.Choices[0].Message.Content;
                }
            }
            catch { }
            return "...";
        }
    }
}
