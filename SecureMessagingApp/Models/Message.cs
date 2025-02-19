using System;

namespace SecureMessagingApp.Models
{
    public class Message
    {
        public string Sender { get; set; }
        public DateTime Timestamp { get; set; }
        public string Content { get; set; }
    }
}