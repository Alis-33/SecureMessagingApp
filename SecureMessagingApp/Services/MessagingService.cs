using System;
using System.Text;
using System.Threading.Tasks;
using SecureMessagingApp.Models;
using System.Text.Json;

namespace SecureMessagingApp.Services
{
    public class MessagingService
    {
        private readonly RSAEncryptionService _encryptionService;
        private readonly NetworkService _networkService;
        private readonly string _masterPassword;
        private readonly string _senderName;

        // Event triggered when a new decrypted message is received.
        public event Action<Message> OnNewMessage;

        // Initializes a new instance of the MessagingService.
        // Subscribes to the NetworkService message events.
        public MessagingService(
            RSAEncryptionService encryptionService,
            NetworkService networkService,
            string masterPassword,
            string senderName)
        {
            _encryptionService = encryptionService;
            _networkService = networkService;
            _masterPassword = masterPassword;
            _senderName = senderName;

            // Subscribe to network messages
            _networkService.OnMessageReceived += HandleIncomingMessage;
        }

        
        // Handles incoming encrypted messages from the network.
        // Decrypts the message, deserializes the JSON into a Message object,
        // and raises the OnNewMessage event.
        private void HandleIncomingMessage(byte[] encryptedMessage)
        {
            try
            {
                // Decrypt the incoming message using the private key.
                string json = _encryptionService.DecryptMessage(encryptedMessage, _masterPassword);

                // Deserialize the JSON into a Message object.
                Message message = JsonSerializer.Deserialize<Message>(json);
                if (message != null)
                {
                    OnNewMessage?.Invoke(message);
                }
                else
                {
                    Console.WriteLine("Received an invalid message format.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error processing incoming message: " + ex.Message);
            }
        }

        
        // Sends a message using a recipient public key file (existing method).
        
        public async Task SendMessageAsync(Message message, string recipientPublicKeyPath, string recipientIpAddress, int recipientPort)
        {
            try
            {
                message.Sender = _senderName;
                message.Timestamp = DateTime.UtcNow;
                string json = JsonSerializer.Serialize(message);
                byte[] encryptedMessage = _encryptionService.EncryptMessage(json, recipientPublicKeyPath);
                await _networkService.SendMessageAsync(recipientIpAddress, recipientPort, encryptedMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending message: " + ex.Message);
            }
        }

        
        // Sends a message using a recipient public key provided as an XML string.
        
        public async Task SendMessageWithPublicKeyAsync(Message message, string recipientPublicKeyXml, string recipientIpAddress, int recipientPort)
        {
            try
            {
                message.Sender = _senderName;
                message.Timestamp = DateTime.UtcNow;
                string json = JsonSerializer.Serialize(message);
                byte[] encryptedMessage = _encryptionService.EncryptMessageWithPublicKey(json, recipientPublicKeyXml);
                await _networkService.SendMessageAsync(recipientIpAddress, recipientPort, encryptedMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending message: " + ex.Message);
            }
        }
    }
}
