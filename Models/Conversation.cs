using System.ComponentModel.DataAnnotations.Schema;

namespace InventarioAPI.Models
{
    public class Conversation 
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}