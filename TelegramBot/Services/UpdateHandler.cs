using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Abstract;

namespace Telegram.Bot.Services
{
    public class UpdateHandler : IUpdateHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IComplimentService complimentService;

        private string buttonText = "Комплиментик для красотки";
        private string usageText = "Тебе нужно нажать кнопочку которая есть на клавиатуре и будет счастье :)";
        private string noComplimentText = "Комплименты закончились, но скоро появятся новые!";

        private string changeButtonTextCommand = "/changeButtonText";
        private string changeUsageTextCommand = "/changeUsageText";
        private string addCommand = "/add";
        private string showCommand = "/show";
        private string adminCommand = "/admin";

        private TimeSpan complimentCooldown = TimeSpan.FromHours(1);
        private DateTime lastCompliment = DateTime.MinValue;

        private long? adminId;

        private ReplyKeyboardMarkup replyKeyboardMarkup =>
            new ReplyKeyboardMarkup(new KeyboardButton(buttonText))
            {
                ResizeKeyboard = true
            };

        public UpdateHandler(ITelegramBotClient botClient, IComplimentService complimentService)
        {
            this._botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
            this.complimentService = complimentService ?? throw new ArgumentNullException(nameof(complimentService));
        }

        public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
        {
            var handler = update switch
            {
                { Message: { } message } => BotOnMessageReceived(message, cancellationToken),
                _ => UnknownUpdateHandlerAsync(update, cancellationToken)
            };

            await handler;
        }

        private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
        {
            if (message.Text is null)
                return;

            if (message.Text.StartsWith(buttonText))
            {
                await MakeСompliment(message, cancellationToken);
            }
            else if (message.Text == adminCommand)
            {
                await Admin(message, cancellationToken);
            }
            else if (message.Text.StartsWith(addCommand))
            {
                await AddCompliment(message, cancellationToken);
            }
            else if (message.Text == showCommand)
            {
                await ShowCompliments(message, cancellationToken);
            }
            else if (message.Text.StartsWith(changeButtonTextCommand))
            {
                await ChangeButtonText(message, cancellationToken);
            }
            else if (message.Text.StartsWith(changeUsageTextCommand))
            {
                await ChangeUsageText(message, cancellationToken);
            }
            else
            {
                await Usage(message, cancellationToken);
            }
        }

        private async Task Usage(Message message, CancellationToken cancellationToken)
        {
            await SendMessage(usageText, message.Chat.Id, cancellationToken);
        }

        private async Task AddCompliment(Message message, CancellationToken cancellationToken)
        {
            var compliment = CleanFromCommand(message.Text!, addCommand);
            if (string.IsNullOrEmpty(compliment))
            {
                await SendMessage($"Нельзя добавить пустоту!", message.Chat.Id, cancellationToken);
            }

            complimentService.AddCompliment(compliment);
            await SendMessage($"\"{compliment}\" успешно добавлен, теперь их {complimentService.GetComplimentCount()} в запасе!", message.Chat.Id, cancellationToken);

        }

        private async Task ShowCompliments(Message message, CancellationToken cancellationToken)
        {
            var compliments = complimentService.GetAllCompliments();
            await SendMessage($"Вот все комплименты которые остались:\n{string.Join("\n", compliments)}", message.Chat.Id, cancellationToken);
        }

        private async Task MakeСompliment(Message message, CancellationToken cancellationToken)
        {
            if(complimentService.GetComplimentCount() == 0)
            {
                await SendMessage(noComplimentText, message.Chat.Id, cancellationToken);
                return;
            }

            string compliment;
            var timePassed = DateTime.Now - lastCompliment;
            if (DateTime.Now - lastCompliment > complimentCooldown)
            {
                compliment = complimentService.GetCompliment();
                if (adminId != null)
                {
                    await SendMessage($"Осталось комплиментов: {complimentService.GetComplimentCount()}!", adminId.Value, cancellationToken);
                }
            }
            else
            {
                var minuts = (int)Math.Ceiling((complimentCooldown - timePassed).TotalMinutes);
                compliment = $"Следующий комплимент будет доступен через {minuts} {GetMinutStringByNumber(minuts)} \u2764";
            }

            await SendMessage(compliment, message.Chat.Id, cancellationToken);

            lastCompliment = DateTime.Now;
        }
        public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            // Cooldown in case of network connection error
            if (exception is RequestException)
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        private async Task Admin(Message message, CancellationToken cancellationToken)
        {
            adminId = message.Chat.Id;
            var text = "Доступные команды для админа:\n" +
                $"{changeButtonTextCommand} новый текст - изменить текст кнопки (сейчас {buttonText})\n" +
                $"{changeUsageTextCommand} новый текст - изменить текст сообщения (сейчас {usageText})\n" +
                $"{addCommand} новый комплимент - добавить новый комплимент (сейчас осталось комплиментов: {complimentService.GetComplimentCount()} )\n" +
                $"{showCommand} - посмотреть все оставшиеся комплименты\n" +
                $"Так же при выполнении команды {adminCommand} - ваш ID запомнился для оповещения об оставшихся комплиментах.";
            await SendMessage(text, message.Chat.Id, cancellationToken);
        }

        private async Task ChangeButtonText(Message message, CancellationToken cancellationToken)
        {
            var newText = CleanFromCommand(message.Text!, changeButtonTextCommand);

            if (ValidateText(newText))
            {
                buttonText = newText;
                await SendMessage($"Новый текст для кнопки успешно установлен на {newText}", message.Chat.Id, cancellationToken);
            }
            else
            {
                await SendMessage($"Нельзя устанавливать пустые строки и строки которые начинаются с \'/\'", message.Chat.Id, cancellationToken);
            }
        }

        private async Task ChangeUsageText(Message message, CancellationToken cancellationToken)
        {
            var newText = CleanFromCommand(message.Text!, changeUsageTextCommand);
            if (ValidateText(newText))
            {
                usageText = newText;
                await SendMessage($"Новый текст сообщения успешно установлен на {newText}", message.Chat.Id, cancellationToken);
            }
            else
            {
                await SendMessage($"Нельзя устанавливать пустые строки и строки которые начинаются с \'/\'", message.Chat.Id, cancellationToken);
            }
        }

        private bool ValidateText(string text)
        {
            return !string.IsNullOrEmpty(text) && !text.StartsWith('/');
        }

        private string CleanFromCommand(string message, string command)
        {
            return message.Replace(command, "").Trim();
        }

        private async Task SendMessage(string message, long id, CancellationToken cancellationToken)
        {
            await _botClient.SendTextMessageAsync(
               chatId: id,
               text: message,
               replyMarkup: replyKeyboardMarkup,
               cancellationToken: cancellationToken);
        }

        private string GetMinutStringByNumber(int minuts)
        {
            switch (minuts%10)
            {
                case 1:
                    return "минуту";
                case 2:
                case 3:
                case 4:
                    return "минуты";
                default:
                    return "минут";
            }
        }

        private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

    }
}