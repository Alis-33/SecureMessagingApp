using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SecureMessagingApp.Services
{
    public class NetworkService
    {
        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private readonly int _port;

        
        // Occurs when a complete message (as a byte array) is received.
        
        public event Action<byte[]> OnMessageReceived;

        
        // Initializes a new instance of the NetworkService class.
        
        public NetworkService(int port = 5000)
        {
            _port = port;
        }

        
        // Starts the TCP listener asynchronously on the specified port.
        // Incoming messages are processed and passed to the OnMessageReceived event.
        
        public async Task StartListenerAsync()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _cts = new CancellationTokenSource();

            Console.WriteLine($"Listening for incoming messages on port {_port}...");

            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error accepting client: " + ex.Message);
                }
            }
        }

        
        // Stops the listener and cancels any pending operations.
        
        public void StopListener()
        {
            _cts?.Cancel();
            _listener?.Stop();
        }

        
        // Handles an individual client connection, reading a complete message from the stream.
        // The protocol expects a 4-byte length prefix followed by the message bytes.
        
        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            {
                try
                {
                    NetworkStream stream = client.GetStream();

                    // Read the 4-byte length prefix
                    byte[] lengthBuffer = new byte[4];
                    int bytesRead = await ReadExactAsync(stream, lengthBuffer, 0, 4);
                    if (bytesRead < 4)
                    {
                        Console.WriteLine("Failed to read message length.");
                        return;
                    }
                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

                    // Read the full message based on the length prefix
                    byte[] messageBuffer = new byte[messageLength];
                    bytesRead = await ReadExactAsync(stream, messageBuffer, 0, messageLength);
                    if (bytesRead < messageLength)
                    {
                        Console.WriteLine("Failed to read full message.");
                        return;
                    }

                    // Trigger the message received event
                    OnMessageReceived?.Invoke(messageBuffer);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error handling client: " + ex.Message);
                }
            }
        }

        
        // Reads the exact number of bytes from the stream.
        
        private async Task<int> ReadExactAsync(NetworkStream stream, byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead);
                if (read == 0)
                {
                    // End of stream reached before expected
                    break;
                }
                totalRead += read;
            }
            return totalRead;
        }

        
        // Sends a message (byte array) to the specified IP address and port.
        // The message is prefixed with its length (4 bytes) to facilitate proper reading on the receiver's end.
        
        public async Task SendMessageAsync(string ipAddress, int port, byte[] message)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(ipAddress, port);
                    NetworkStream stream = client.GetStream();

                    // Write the length prefix
                    byte[] lengthPrefix = BitConverter.GetBytes(message.Length);
                    await stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length);

                    // Write the actual message
                    await stream.WriteAsync(message, 0, message.Length);
                    await stream.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending message: " + ex.Message);
            }
        }
    }
}
