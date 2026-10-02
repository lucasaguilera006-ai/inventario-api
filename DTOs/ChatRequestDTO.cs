namespace InventarioAPI.DTOs
{
    public class ChatRequestDTO
    {
        public string Content { get; set; } = string.Empty;
        public int? ConversationId { get; set; }
    }
}