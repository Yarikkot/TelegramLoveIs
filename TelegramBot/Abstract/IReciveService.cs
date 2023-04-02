using System.Threading;
using System.Threading.Tasks;

namespace Telegram.Bot.Abstract
{
    /// <summary>
    /// A marker interface for Update Receiver service
    /// </summary>
    public interface IReceiverService
    {
        Task ReceiveAsync(CancellationToken stoppingToken);
    }
}