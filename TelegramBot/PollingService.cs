﻿using System;
using Telegram.Bot.Abstract;

namespace Telegram.Bot.Services
{
    // Compose Polling and ReceiverService implementations
    public class PollingService : PollingServiceBase<ReceiverService>
    {
        public PollingService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }
    }
}