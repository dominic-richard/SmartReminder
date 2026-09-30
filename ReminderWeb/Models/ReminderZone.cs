namespace ReminderWeb.Models
{
    public class ReminderZone
    {
        public int Id { get; set; }

        public int ReminderId { get; set; }

        public int ZoneNumber { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public bool IsCompleted { get; set; } = false;

        public bool IsActive { get; set; } = false;
    }
}