using System.ComponentModel.DataAnnotations;

namespace ReminderWeb.Models
{
    public class Reminder
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = "";

        [Required]
        public DateTime ReminderDateTime { get; set; }

        public bool IsCompleted { get; set; } = false;

        [MaxLength(2000)]
        public string? CompletionProof { get; set; }

        public DateTimeOffset? EmergencyPromptedAt { get; set; }

        [MaxLength(20)]
        public string? EmergencyResponse { get; set; }

        public DateTimeOffset? PixelEffectStartedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<ReminderZone> Zones { get; set; } = new();
    }
    
}