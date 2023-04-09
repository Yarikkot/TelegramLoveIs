using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Telegram.Bot.Abstract
{
    // A background service consuming a scoped service.
    /// <summary>
    /// An abstract class to compose Polling background service and Receiver implementation classes
    /// </summary>
    /// <typeparam name="TReceiverService">Receiver implementation class</typeparam>
    public abstract class PollingServiceBase<TReceiverService> : BackgroundService
        where TReceiverService : IReceiverService
    {
        private readonly IServiceProvider _serviceProvider;

        internal PollingServiceBase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await DoWork(stoppingToken);
        }

        private async Task DoWork(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var receiver = scope.ServiceProvider.GetRequiredService<TReceiverService>();

                    await receiver.ReceiveAsync(stoppingToken);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
    }
}