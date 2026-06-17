using System.Collections.Generic;
using System.Threading.Tasks;

namespace SVM.Services
{
    public interface IGeminiService
    {
        Task<string> GetChatResponseAsync(string prompt, List<ChatMessage> history);
    }

    public class ChatMessage
    {
        public string Role { get; set; } = "user"; 
        public string Content { get; set; } = "";
    }
}