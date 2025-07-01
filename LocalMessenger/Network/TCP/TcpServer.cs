using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using LocalMessenger.Core.Security;
using LocalMessenger.Utilities;

namespace LocalMessenger.Network.Tcp
{
    public class TcpServer : IDisposable
    {
        private bool disposed = false;

        public void Dispose()
        {
            if (!disposed)
            {
                tcpListener?.Stop();
                Logger.Log("TCP server resources disposed");
                disposed = true;
            }
        }

        private readonly TcpListener tcpListener;
        private readonly Action<string, byte[]> handleKeyExchange;
        private readonly Action<string, byte[], byte[], byte[]> handleMessage;
        private readonly Action<string, string, long, string, bool> handleFile;

        public TcpServer(Action<string, byte[]> handleKeyExchange,
            Action<string, byte[], byte[], byte[]> handleMessage,
            Action<string, string, long, string, bool> handleFile)
        {
            tcpListener = new TcpListener(IPAddress.Any, 12000);
            this.handleKeyExchange = handleKeyExchange;
            this.handleMessage = handleMessage;
            this.handleFile = handleFile;
        }

        public async Task StartAsync()
        {
            try
            {
                tcpListener.Start();
                Logger.Log("TCP server started on port 12000");
                while (true)
                {
                    var client = await tcpListener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Server error: {ex.Message}");
            }
        }

        public void Stop()
        {
            tcpListener?.Stop();
            Logger.Log("TCP server stopped");
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            {
                try
                {
                    var stream = client.GetStream();
                    var buffer = new byte[4096];
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Logger.Log($"Received TCP message: {message}");
                    var parts = message.Split('|');

                    if (parts[0] == "KEY_EXCHANGE")
                    {
                        var sender = parts[1];
                        var contactPublicKey = Convert.FromBase64String(parts[2]);
                        handleKeyExchange(sender, contactPublicKey);
                    }
                    else if (parts[0] == "MESSAGE")
                    {
                        var sender = parts[1];
                        var encryptedMessage = Convert.FromBase64String(parts[2]);
                        var nonce = Convert.FromBase64String(parts[3]);
                        var tag = Convert.FromBase64String(parts[4]);
                        handleMessage(sender, encryptedMessage, nonce, tag);
                    }
                    else if (parts[0].StartsWith("FILE") || parts[0].StartsWith("IMAGE"))
                    {
                        var isChunked = parts[0].EndsWith("_CHUNKED");
                        var sender = parts[1];
                        var fileName = parts[2];
                        var fileSize = long.Parse(parts[3]);
                        var nonce = parts.Length > 3 ? Convert.FromBase64String(parts[3]) : null;
                        handleFile(sender, fileName, fileSize, nonce != null ? Convert.ToBase64String(nonce) : null, isChunked);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error handling client: {ex.Message}");
                }
            }
        }
    }
}