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

        private const string InvalidText = "Нельзя устанавливать пустые строки и строки которые начинаются с \'/\'";
        private const string adminInfoPath = "admin.yar";

        private string buttonText = "Комплиментик для красотки";
        private string usageText = "Тебе нужно нажать кнопочку которая есть на клавиатуре и будет счастье :)";
        private string noComplimentText = "Комплименты закончились, но скоро появятся новые!";

        private string changeButtonTextCommand = "/changeButtonText";
        private string changeUsageTextCommand = "/changeUsageText";
        private string addCommand = "/add";
        private string removeLastAddedCommand = "/removeLastAdded";
        private string showCommand = "/show";
        private string adminCommand = "/admin";
        private string clearAdminCommand = "/clearAdmin";

        private TimeSpan complimentCooldown = TimeSpan.FromHours(1);
        private DateTime lastCompliment = DateTime.MinValue;

        private long? adminId;

        private ReplyMarkupBase replyKeyboardMarkup =>
            new ReplyKeyboardMarkup(new KeyboardButton(buttonText))
            {
                ResizeKeyboard = true
            };
        private ReplyMarkupBase replyKeyboardRemove = new ReplyKeyboardRemove();

        public UpdateHandler(ITelegramBotClient botClient, IComplimentService complimentService)
        {
            if (System.IO.File.Exists(adminInfoPath) 
                && long.TryParse(System.IO.File.ReadAllText(adminInfoPath), out var id))
            {
                adminId = id;
            }

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
            else if (message.Text == clearAdminCommand)
            {
                await ClearAdmin(message, cancellationToken);
            }
            else if (message.Text == removeLastAddedCommand)
            {
                await RemoveLastAdded(message, cancellationToken);
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

        private async Task MakeСompliment(Message message, CancellationToken cancellationToken)
        {
            string compliment;
            var timePassed = DateTime.Now - lastCompliment;
            if (DateTime.Now - lastCompliment > complimentCooldown)
            {
                compliment = complimentService.GetCompliment() ?? noComplimentText;
                if (adminId != null)
                {
                    var count = complimentService.GetComplimentCount();
                    if (count == 0)
                    {
                        await SendMessage($"❗️❗️❗️Осталось {count} комплиментов!❗️❗️❗️\nУ вас час чтоб добавить их, иначе кто знает что будет...", adminId.Value, cancellationToken);
                    }
                    else
                    {
                        await SendMessage($"Осталось комплиментов: {count}!", adminId.Value, cancellationToken);
                    }
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

        private async Task AddCompliment(Message message, CancellationToken cancellationToken)
        {
            if (Auth(message.Chat.Id))
            {
                var compliment = CleanTextFromCommand(message.Text!, addCommand);
                if (ValidateText(compliment))
                {
                    complimentService.AddCompliment(compliment);
                    await SendMessage($"\"{compliment}\" успешно добавлен, теперь их {complimentService.GetComplimentCount()} в запасе!", message.Chat.Id, cancellationToken);
                }
                else
                {
                    await SendMessage(InvalidText, message.Chat.Id, cancellationToken);
                }
            }
        }

        private async Task ShowCompliments(Message message, CancellationToken cancellationToken)
        {
            if (Auth(message.Chat.Id))
            {
                var compliments = complimentService.GetAllCompliments();
                await SendMessage($"Вот все комплименты которые остались:\n{string.Join("\n", compliments)}", message.Chat.Id, cancellationToken);
            }
        }

        private async Task Admin(Message message, CancellationToken cancellationToken)
        {
            if (adminId is null)
            {
                adminId = message.Chat.Id;
                System.IO.File.WriteAllText(adminInfoPath, adminId.ToString());
            }

            if (Auth(message.Chat.Id))
            {
                var text = "Доступные команды для админа:\n" +
                    $"{changeButtonTextCommand} новый текст - изменить текст кнопки (сейчас {buttonText})\n" +
                    $"{changeUsageTextCommand} новый текст - изменить текст сообщения (сейчас {usageText})\n" +
                    $"{addCommand} новый комплимент - добавить новый комплимент (сейчас осталось комплиментов: {complimentService.GetComplimentCount()} )\n" +
                    $"{removeLastAddedCommand} - удаляет последний добавленный комплимент\n" +
                    $"{showCommand} - посмотреть все оставшиеся комплименты\n" +
                    $"{clearAdminCommand} - удаляем данные об админе. Можно заново назначить командой {adminCommand}\n" +
                    $"Так же при выполнении команды {adminCommand} - ваш ID запомнился для оповещения об оставшихся комплиментах.";
                await SendMessage(text, message.Chat.Id, cancellationToken);
            }
        }

        private async Task RemoveLastAdded(Message message, CancellationToken cancellationToken)
        {
            if (complimentService.RemoveLastAdded())
            {
                await SendMessage("Успешно удалён последний добавленный комплимент", message.Chat.Id, cancellationToken);
            }
            else
            {
                await SendMessage("Нечего удалять :С", message.Chat.Id, cancellationToken);
            }
        }

        private async Task ClearAdmin(Message message, CancellationToken cancellationToken)
        {
            if (Auth(message.Chat.Id))
            {
                if (System.IO.File.Exists(adminInfoPath))
                {
                    System.IO.File.Delete(adminInfoPath);
                }

                adminId = null;
                await SendMessage($"Админ успешно удалён! Можете заново его переназначит командой {adminCommand}", message.Chat.Id, cancellationToken);
            }
        }

        private async Task ChangeButtonText(Message message, CancellationToken cancellationToken)
        {
            if (Auth(message.Chat.Id))
            {
                var newText = CleanTextFromCommand(message.Text!, changeButtonTextCommand);

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
        }

        private async Task ChangeUsageText(Message message, CancellationToken cancellationToken)
        {
            if (Auth(message.Chat.Id))
            {
                var newText = CleanTextFromCommand(message.Text!, changeUsageTextCommand);
                if (ValidateText(newText))
                {
                    usageText = newText;
                    await SendMessage($"Новый текст сообщения успешно установлен на {newText}", message.Chat.Id, cancellationToken);
                }
                else
                {
                    await SendMessage(InvalidText, message.Chat.Id, cancellationToken);
                }
            }
        }

        private bool ValidateText(string text)
        {
            return !string.IsNullOrEmpty(text) && !text.StartsWith('/');
        }

        private string CleanTextFromCommand(string message, string command)
        {
            return message.Replace(command, "").Trim();
        }

        private async Task SendMessage(string message, long id, CancellationToken cancellationToken)
        {
            IReplyMarkup replyKeyboarb = Auth(id) ? replyKeyboardRemove : replyKeyboardMarkup;
            await _botClient.SendTextMessageAsync(
               chatId: id,
               text: message,
               replyMarkup: replyKeyboarb,
               cancellationToken: cancellationToken);
        }

        private string GetMinutStringByNumber(int minuts)
        {
            switch (minuts % 10)
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

        private bool Auth(long id)
        {
            if (adminId == null)
                return false;

            return id == adminId;
        }

        public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            if (exception is RequestException)
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

    }
}