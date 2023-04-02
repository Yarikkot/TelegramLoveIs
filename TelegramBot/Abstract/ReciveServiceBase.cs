using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Polling;

namespace Telegram.Bot.Abstract
{
    /// <summary>
    /// An abstract class to compose Receiver Service and Update Handler classes
    /// </summary>
    /// <typeparam name="TUpdateHandler">Update Handler to use in Update Receiver</typeparam>
    public abstract class ReceiverServiceBase<TUpdateHandler> : IReceiverService
        where TUpdateHandler : IUpdateHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IUpdateHandler _updateHandler;

        internal ReceiverServiceBase(ITelegramBotClient botClient, TUpdateHandler updateHandler)
        {
            _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
            _updateHandler = updateHandler;
        }

        /// <summary>
        /// Start to service Updates with provided Update Handler class
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        public async Task ReceiveAsync(CancellationToken stoppingToken)
        {
            var receiverOptions = new ReceiverOptions()
            {                
                ThrowPendingUpdates = false,
            };

            // Start receiving updates
            await _botClient.ReceiveAsync(
                updateHandler: _updateHandler,
                receiverOptions: receiverOptions,
                cancellationToken: stoppingToken);
        }
    }
}