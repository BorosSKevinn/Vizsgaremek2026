namespace Vizsgaremek2026.Models
{
    public class CalendarEvent
    {
        public string Text { get; set; } = "";
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string CssClass { get; set; } = "";
    }
}
