namespace Pursuit.Model
{
    public class NotifyStatusRequest
    {
        public string? UserId { get; set; } = null!;

        public string? NotificationId { get; set; } = null!;

        public string? Status { get; set; } = null!;
    }
}
