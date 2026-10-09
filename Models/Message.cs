using System.ComponentModel.DataAnnotations.Schema;

namespace InventarioAPI.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;
    }
}