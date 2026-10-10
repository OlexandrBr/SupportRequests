namespace SupportRequests.Models
{
    public enum Priority
    {
        Low,
        Medium,
        High
    }

    public class SupportRequest
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = "";
        public string Topic { get; set; } = "";
        public string Description { get; set; } = "";
        public Priority Priority { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}