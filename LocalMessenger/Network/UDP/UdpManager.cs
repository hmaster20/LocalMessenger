using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using LocalMessenger.Core.Network;
using LocalMessenger.Utilities;

namespace LocalMessenger.Network.Udp
{
    public class UdpManager : IDisposable
    {
        private bool _disposed = false;
        private readonly UdpClient _udpListener;
        private readonly UdpClient _udpSender;
        private readonly string _myIP;
        private readonly string _myLogin;
        private readonly string _myName;
        private readonly string _myStatus;
        private readonly Func<byte[]> _getPublicKey;
        private readonly Action<string, string> _handleUdpMessage;

        public UdpManager(string myLogin, string myName, string myStatus,
            Func<byte[]> getPublicKey, Action<string, string> handleUdpMessage)
        {
            _myIP = NetworkUtils.GetLocalIPAddress();
            _myLogin = myLogin;
            _myName = myName;
            _myStatus = myStatus;
            _getPublicKey = getPublicKey;
            _handleUdpMessage = handleUdpMessage;

            _udpListener = new UdpClient(new IPEndPoint(IPAddress.Any, 11000));
            _udpSender = new UdpClient();
            _udpSender.Client.Bind(new IPEndPoint(IPAddress.Parse(_myIP), 0));
            _udpSender.EnableBroadcast = true;
        }

        public async Task StartBroadcastAsync()
        {
            while (true)
            {
                try
                {
                    var publicKey = _getPublicKey();
                    var data = $"HELLO|{_myLogin}|{_myName}|{_myStatus}|{Convert.ToBase64String(publicKey)}";
                    var bytes = Encoding.UTF8.GetBytes(data);
                    await _udpSender.SendAsync(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, 11000));
                    Logger.Log($"Sent HELLO broadcast from {_myIP}: {data}");
                    await Task.Delay(15000);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Broadcast error: {ex.Message}");
                }
            }
        }

        public async Task StartListenerAsync()
        {
            try
            {
                while (true)
                {
                    var result = await _udpListener.ReceiveAsync();
                    var message = Encoding.UTF8.GetString(result.Buffer);
                    var remoteIP = result.RemoteEndPoint.Address.ToString();
                    Logger.Log($"Received UDP message from {remoteIP}: {message}");
                    _handleUdpMessage(message, remoteIP);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"UDP listener error: {ex.Message}");
            }
        }

        public async Task SendBroadcastAsync(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            await _udpSender.SendAsync(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, 11000));
            Logger.Log($"Sent broadcast: {data}");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _udpListener?.Close();
                _udpListener?.Dispose();
                _udpSender?.Close();
                _udpSender?.Dispose();
                Logger.Log("UdpManager resources disposed");
                _disposed = true;
            }
        }
    }
}


#region MainForm

//private async void StartUdpBroadcast()
//{
//    Logger.Log("Starting UDP broadcast for user discovery");
//    while (true)
//    {
//        try
//        {
//            var publicKey = GetMyPublicKey();
//            var data = $"HELLO|{myLogin}|{myName}|{myStatus}|{Convert.ToBase64String(publicKey)}";
//            var bytes = Encoding.UTF8.GetBytes(data);
//            await udpSender.SendAsync(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, 11000));
//            Logger.Log($"Sent HELLO broadcast from {myIP}: {data}");
//            await Task.Delay(15000);
//        }
//        catch (Exception ex)
//        {
//            Logger.Log($"Broadcast error: {ex.Message}");
//            MessageBox.Show($"Broadcast error: {ex.Message}");
//        }
//    }
//}

//private async void StartUdpListener()
//{
//    Logger.Log("Starting UDP listener on port 11000");
//    try
//    {
//        while (!cancellationTokenSource.Token.IsCancellationRequested)
//        {
//            try
//            {
//                var result = await Task.Run(() => udpListener.ReceiveAsync(), cancellationTokenSource.Token);
//                var message = Encoding.UTF8.GetString(result.Buffer);
//                var remoteIP = result.RemoteEndPoint.Address.ToString();
//                Logger.Log($"Received UDP message from {remoteIP}: {message}");

//                HandleUdpMessage(message, remoteIP);
//            }
//            catch (OperationCanceledException)
//            {
//                Logger.Log("UDP listener cancelled");
//                break;
//            }
//            catch (ObjectDisposedException)
//            {
//                Logger.Log("UDP listener disposed");
//                break;
//            }
//            catch (Exception ex)
//            {
//                Logger.Log($"UDP listener error: {ex.Message}");
//            }
//        }
//    }
//    catch (Exception ex)
//    {
//        Logger.Log($"Unexpected error in UDP listener: {ex.Message}");
//    }
//}


#endregion