namespace SecureMessagingApp.Models
{
    public class Contact
    {
        public string Name { get; set; }
        public string PublicKey { get; set; }
        public string IPAddress { get; set; }
        public int Port { get; set; }
    }
}