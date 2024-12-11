namespace SIAMS.Models
{
    public class Log
    {
        public int LogId { get; set; }
        public string Action { get; set; }
        public DateTime Timestamp { get; set; }
        public string PerformedBy { get; set; }
    }
}
