using DALL_E_LAMA;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

var TELEGRAM_BOT_TOKEN = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
var botClient = new TelegramBotClient(TELEGRAM_BOT_TOKEN);

using var cts = new CancellationTokenSource();

botClient.StartReceiving<UpdateHandler>(new ReceiverOptions
{
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
}, cts.Token);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();