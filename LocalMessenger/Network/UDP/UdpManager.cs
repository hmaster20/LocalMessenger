using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using LocalMessenger.Utilities;

namespace LocalMessenger.Network.Udp
{
    public class UdpManager : IDisposable
    {
        private bool disposed = false;

        public void Dispose()
        {
            if (!disposed)
            {
                udpListener?.Close();
                udpListener?.Dispose();
                udpSender?.Close();
                udpSender?.Dispose();
                Logger.Log("UdpManager resources disposed");
                disposed = true;
            }
        }

        private readonly UdpClient udpListener;
        private readonly UdpClient udpSender;
        private readonly string myIP;
        private readonly string myLogin;
        private readonly string myName;
        private readonly string myStatus;
        private readonly Func<byte[]> getPublicKey;
        private readonly Action<string, string> handleUdpMessage;

        public UdpManager(string myIP, string myLogin, string myName, string myStatus,
            Func<byte[]> getPublicKey, Action<string, string> handleUdpMessage)
        {
            this.myIP = myIP;
            this.myLogin = myLogin;
            this.myName = myName;
            this.myStatus = myStatus;
            this.getPublicKey = getPublicKey;
            this.handleUdpMessage = handleUdpMessage;

            udpListener = new UdpClient(new IPEndPoint(IPAddress.Any, 11000));
            udpSender = new UdpClient();
            udpSender.Client.Bind(new IPEndPoint(IPAddress.Parse(myIP), 0));
            udpSender.EnableBroadcast = true;
        }

        public async Task StartBroadcastAsync()
        {
            while (true)
            {
                try
                {
                    var publicKey = getPublicKey();
                    var data = $"HELLO|{myLogin}|{myName}|{myStatus}|{Convert.ToBase64String(publicKey)}";
                    var bytes = Encoding.UTF8.GetBytes(data);
                    await udpSender.SendAsync(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, 11000));
                    Logger.Log($"Sent HELLO broadcast from {myIP}: {data}");
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
                    var result = await udpListener.ReceiveAsync();
                    var message = Encoding.UTF8.GetString(result.Buffer);
                    var remoteIP = result.RemoteEndPoint.Address.ToString();
                    Logger.Log($"Received UDP message from {remoteIP}: {message}");
                    handleUdpMessage(message, remoteIP);
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
            await udpSender.SendAsync(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, 11000));
            Logger.Log($"Sent broadcast: {data}");
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
    }
}