using RestSharp;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


namespace TelegramBot
{
    public class BotController
    {
        private readonly TelegramBotClient _botClient = new TelegramBotClient(Constants.Token);
        private readonly CancellationToken _cancellationToken = new CancellationToken();
        private readonly ReceiverOptions _receiverOptions = new ReceiverOptions { AllowedUpdates = { } };
        private readonly RestClient _restClient = new RestClient(Constants.BaseURL);
        
        public async Task Start()
        {
            _botClient.StartReceiving(HandlerUpdateAsync, HandlerErrorAsync, _receiverOptions, _cancellationToken);
            var botMe = await _botClient.GetMeAsync();
            Console.WriteLine(botMe.Username + "started");
        }

        private Task HandlerErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Помилка в телеграм бот АПІ: \n{apiRequestException.ErrorCode}" + $"{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        private async Task HandlerUpdateAsync(ITelegramBotClient client, Update update, CancellationToken token)
        {
            if (update.Type == UpdateType.Message && update?.Message?.Text != null)
            {
                await HandlerMessageAsync(_botClient, update.Message);
            }
        }

        private async Task HandlerMessageAsync(ITelegramBotClient botClient, Message message)
        {
            if (message.Text == "/start")
            {
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Вітаю у боті для пошуку і перегляду статистики футбольних матчів. Для ознайомства із функціями бота натисніть на кнопку Menu, або введіть команду /help");
                return;
            }

            else if (message.Text == "/help")
            {
                await _botClient.SendTextMessageAsync(message.Chat.Id,
                    "/checklive - пошук матчів в лайві " +
                    "\n/checkteaminseason - пошук матчів команди в сезоні " +
                    "\n/checkdate - пошук матчів за датою");
            }

            else if (message.Text == "/checklive")
            {
                var request = new RestRequest($"/GetFixturesAll", Method.Get);
                
                var content = _restClient.Execute<string>(request).Content.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\"", "");
                await _botClient.SendTextMessageAsync(message.Chat.Id, content);
                return;
            }

            else if (message.Text.StartsWith("/checkteaminseason"))
            {
                await CheckTeamInCeason(message);
                return;
            }

            else if (message.Text.StartsWith("/checkdate"))
            {
                await CheckDate(message, false);
                return;
            }

            else if (message.Text.StartsWith("/checktoday"))
            {
                await CheckDate(message, true);
                return;
            }

            else
            {
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Невідома команда. Будь ласка використайте команду /help для ознайомлення зі списком доступних команд.");
                return;
            }
        }
        
        private async Task CheckDate(Message message, bool IsToday)
        {
            try
            {
                if (IsToday)
                {
                    DateTime date = DateTime.Now;
                    await _botClient.SendTextMessageAsync(message.Chat.Id, _restClient.Execute<string>(new RestRequest($"/GetFixturesByDate?date={date.ToString("yyyy-MM-dd")}&IsToday={IsToday}", Method.Get)).Content.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\"", ""));
                    return;
                }

                string[] answer = message.Text.Split(" ");
                if (answer.Length < 2)
                {
                    await _botClient.SendTextMessageAsync(message.Chat.Id, "Будь ласка вкажіть корректну дату в форматі рррр-мм-дд");
                    return;
                }

                var request = new RestRequest($"/GetFixturesByDate?date={answer[1]}&IsToday={IsToday}", Method.Get);
                string content = _restClient.Execute<string>(request).Content.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\"", "");
                
                
                await _botClient.SendTextMessageAsync(message.Chat.Id, content);
                return;

            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Виникла помилка. Перевірте вказані дані.");
                return;
            }
        }

        private async Task CheckTeamInCeason(Message message)
        {
            try
            {
                string[] answer = message.Text.Split(" ");
                string teamName = "";
                for (int i = 1; i < answer.Length - 1; i++)
                {

                    teamName += answer[i];
                    if (i != answer.Length - 1) teamName += " ";
                }

                ushort season = 0;
                if (answer.Length < 3)
                {
                    await _botClient.SendTextMessageAsync(message.Chat.Id, "Будь ласка вкажіть повну назву команди (наприклад Manchester City) і сезон, як 4-значне число (наприклад 2023)");
                    return;
                }

                try
                {
                    season = Convert.ToUInt16(answer[answer.Length - 1]);
                }
                catch (Exception ex)
                {
                    await _botClient.SendTextMessageAsync(message.Chat.Id, "Вкажіть сезон, як 4-значне число (наприклад 2023)");
                    return;
                }

                var request = new RestRequest($"{Constants.BaseURL}/GetFixturesByTeamInSeason?teamName={teamName}&season={season}", Method.Get);
                string content = _restClient.Execute<string>(request).Content.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\"", "");

                await _botClient.SendTextMessageAsync(message.Chat.Id, content);
                return;
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Виникла помилка. Перевірте вказані дані.");
                return;
            }
        }


    }
}
