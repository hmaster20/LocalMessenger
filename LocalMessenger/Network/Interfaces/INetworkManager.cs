using System.Threading.Tasks;

namespace LocalMessenger.Network.Interfaces
{
    public interface INetworkManager
    {
        Task StartAsync();
        void Stop();
    }
}