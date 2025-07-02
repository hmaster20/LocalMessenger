using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using LocalMessenger.Utilities;

namespace LocalMessenger.Network.Tcp
{
    public class TcpServer : IDisposable
    {
        private bool _disposed = false;
        private readonly TcpListener _tcpListener;
        private readonly Action<string, byte[]> _handleKeyExchange;
        private readonly Action<string, byte[], byte[], byte[]> _handleMessage;
        private readonly Action<string, string, long, string, bool> _handleFile;

        public TcpServer(Action<string, byte[]> handleKeyExchange,
            Action<string, byte[], byte[], byte[]> handleMessage,
            Action<string, string, long, string, bool> handleFile)
        {
            _tcpListener = new TcpListener(IPAddress.Any, 12000);
            _handleKeyExchange = handleKeyExchange;
            _handleMessage = handleMessage;
            _handleFile = handleFile;
        }

        public async Task StartAsync()
        {
            try
            {
                _tcpListener.Start();
                Logger.Log("TCP server started on port 12000");
                while (true)
                {
                    var client = await _tcpListener.AcceptTcpClientAsync();
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
            _tcpListener?.Stop();
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
                        _handleKeyExchange(sender, contactPublicKey);
                    }
                    else if (parts[0] == "MESSAGE")
                    {
                        var sender = parts[1];
                        var encryptedMessage = Convert.FromBase64String(parts[2]);
                        var nonce = Convert.FromBase64String(parts[3]);
                        var tag = Convert.FromBase64String(parts[4]);
                        _handleMessage(sender, encryptedMessage, nonce, tag);
                    }
                    else if (parts[0].StartsWith("FILE") || parts[0].StartsWith("IMAGE"))
                    {
                        var isChunked = parts[0].EndsWith("_CHUNKED");
                        var sender = parts[1];
                        var fileName = parts[2];
                        var fileSize = long.Parse(parts[3]);
                        var nonce = parts.Length > 3 ? Convert.FromBase64String(parts[3]) : null;
                        _handleFile(sender, fileName, fileSize, nonce != null ? Convert.ToBase64String(nonce) : null, isChunked);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error handling client: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _tcpListener?.Stop();
                Logger.Log("TCP server resources disposed");
                _disposed = true;
            }
        }
    }
}