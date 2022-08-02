using DALL_E_LAMA.DalleApi;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using DALL_E_LAMA.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Exceptions;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;

namespace DALL_E_LAMA
{
    internal class UpdateHandler : IUpdateHandler
    {
        private static readonly Regex _messageRegex = new Regex(@"^/(?<command>[^\s$@]+)(?<botname>@[^\s$]+)?\s?(?<arguments>.*)$");

        private static readonly string DALLE_API_TOKEN = Environment.GetEnvironmentVariable("DALLE_API_TOKEN");
        private readonly DalleApiClient _dalleClient = new DalleApiClient(DALLE_API_TOKEN);
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly Font _font;

        public UpdateHandler()
        {
            FontCollection collection = new();
            FontFamily family = collection.Add("waltographUI.ttf");
            _font = family.CreateFont(36, FontStyle.Regular);
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            Console.WriteLine($"{update.Message?.From?.Username}: {update.Message?.Text}");

            try
            {
                await ProcessCallbackQueries(botClient, update);
                await ProcessCommands(botClient, update, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await botClient.SendTextMessageAsync(update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id, ex.Message, null, null, null, null, null, update.Message?.MessageId, true);
            }
        }

        private async Task ProcessCallbackQueries(ITelegramBotClient botClient, Update update)
        {
            if (update.CallbackQuery == null && update.CallbackQuery?.Data == null)
                return;

            using var db = new DalleDbContext();
            var generation = await db.Generations.FirstOrDefaultAsync(x => x.Id == update.CallbackQuery.Data);
            if (generation == null)
                throw new Exception("Sorry, this image is not in my database.");

            if (string.IsNullOrEmpty(generation.TaskId))
                throw new Exception("Sorry, I don't have the corresponding TaskId for that image, check with Max hehe");

            var task = await _dalleClient.GetTask(generation.TaskId);

            var generationEntry = task.Generations.Data.FirstOrDefault(x => x.Id == generation.Id);
            var webpBytes = await _httpClient.GetByteArrayAsync(generationEntry.Generation.ImagePath);
            using var image = Image.Load(webpBytes);

            var stream = new MemoryStream();
            image?.SaveAsPng(stream);
            stream.Seek(0, SeekOrigin.Begin);

            await botClient.SendDocumentAsync(update.CallbackQuery?.From.Id,
                new InputOnlineFile(stream, $"{generation.Id}.png"));

            return;
        }

        private async Task ProcessCommands(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(update.Message?.Text) || !update.Message.Text.StartsWith("/") ||
                update.Message.ForwardFrom != null)
                return;

            var messageMatch = _messageRegex.Match(update.Message.Text);
            var command = messageMatch.Groups["command"].Value;
            var arguments = messageMatch.Groups["arguments"].Value;

            switch (command)
            {
                case "dalle":

                    await ProcessDalleCommand(botClient, update, arguments, cancellationToken);
                    break;

                case "credits":
                    await ProcessCreditsCommand(botClient, update);
                    break;

                case "download":
                case "get":
                    await ProcessGetCommand(botClient, update);
                    break;

                default:
                    break;
            }
        }

        private async Task ProcessCreditsCommand(ITelegramBotClient botClient, Update update)
        {
            var credits = await _dalleClient.GetRemainingCredits();

            await botClient.SendTextMessageAsync(update.Message.Chat.Id, $"Credits left: {credits.AggregateCredits}", null,
                null, null, null, null, update.Message.MessageId);
        }

        private async Task ProcessGetCommand(ITelegramBotClient botClient, Update update)
        {
            if (update.Message.ReplyToMessage == null || update.Message.ReplyToMessage.Photo == null)
                throw new Exception("You need to reply to an image I sent.");

            using var db = new DalleDbContext();
            var generation = db.Generations.FirstOrDefault(x => x.MessageId == update.Message.ReplyToMessage.MessageId);
            if (generation == null)
                throw new Exception("Sorry, this image is not in my database.");

            await botClient.SendChatActionAsync(update.Message.Chat.Id, ChatAction.UploadPhoto);

            var imageBytes = await _dalleClient.DownloadGeneration(generation.Id);
            using var stream = new MemoryStream(imageBytes);

            await botClient.SendDocumentAsync(update.Message.Chat.Id, new InputOnlineFile(stream, $"{generation.Id}.jpg"), null,
                null, null, null, null, null, null, update.Message.MessageId);
        }

        private async Task ProcessDalleCommand(ITelegramBotClient botClient, Update update, string arguments,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(arguments))
                return;

            var task = await _dalleClient.CreateText2ImageTask(arguments);
            while (task?.Status == "pending")
            {
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
                task = await _dalleClient.GetTask(task.Id);
            }

            if (task?.Status == "rejected")
                throw new Exception(task?.StatusInformation?.Message);

            if (task?.Status == "cancelled")
                throw new Exception("The request was cancelled by OpenAI (Maintenance?)");

            await botClient.SendChatActionAsync(update.Message.Chat.Id, ChatAction.UploadPhoto);

            var generationIds = task.Generations.Data.Select(x => x.Id).ToList();

            using var fullImage = new Image<Rgb24>(1024, 1024);

            var imageBytes1 = await _httpClient.GetByteArrayAsync(task.Generations.Data[0].Generation.ImagePath);
            using var image1 = Image.Load(imageBytes1);
            image1.Mutate(o => o
                .Resize(new Size(512, 512))
                .DrawText("1", _font, Color.White, new PointF(10 - 2, 10))
                .DrawText("1", _font, Color.White, new PointF(10 - 2, 10 - 2))
                .DrawText("1", _font, Color.White, new PointF(10, 10 - 2))
                .DrawText("1", _font, Color.White, new PointF(10 + 2, 10 - 2))
                .DrawText("1", _font, Color.White, new PointF(10 + 2, 10))
                .DrawText("1", _font, Color.White, new PointF(10 + 2, 10 + 2))
                .DrawText("1", _font, Color.White, new PointF(10, 10 + 2))
                .DrawText("1", _font, Color.White, new PointF(10 - 2, 10 + 2))
                .DrawText("1", _font, Color.Black, new PointF(10, 10)));

            var imageBytes2 = await _httpClient.GetByteArrayAsync(task.Generations.Data[1].Generation.ImagePath);
            using var image2 = Image.Load(imageBytes2);
            image2.Mutate(o => o
                .Resize(new Size(512, 512))
                .DrawText("2", _font, Color.White, new PointF(10 - 2, 10))
                .DrawText("2", _font, Color.White, new PointF(10 - 2, 10 - 2))
                .DrawText("2", _font, Color.White, new PointF(10, 10 - 2))
                .DrawText("2", _font, Color.White, new PointF(10 + 2, 10 - 2))
                .DrawText("2", _font, Color.White, new PointF(10 + 2, 10))
                .DrawText("2", _font, Color.White, new PointF(10 + 2, 10 + 2))
                .DrawText("2", _font, Color.White, new PointF(10, 10 + 2))
                .DrawText("2", _font, Color.White, new PointF(10 - 2, 10 + 2))
                .DrawText("2", _font, Color.Black, new PointF(10, 10)));

            var imageBytes3 = await _httpClient.GetByteArrayAsync(task.Generations.Data[2].Generation.ImagePath);
            using var image3 = Image.Load(imageBytes3);
            image3.Mutate(o => o
                .Resize(new Size(512, 512))
                .DrawText("3", _font, Color.White, new PointF(10 - 2, 10))
                .DrawText("3", _font, Color.White, new PointF(10 - 2, 10 - 2))
                .DrawText("3", _font, Color.White, new PointF(10, 10 - 2))
                .DrawText("3", _font, Color.White, new PointF(10 + 2, 10 - 2))
                .DrawText("3", _font, Color.White, new PointF(10 + 2, 10))
                .DrawText("3", _font, Color.White, new PointF(10 + 2, 10 + 2))
                .DrawText("3", _font, Color.White, new PointF(10, 10 + 2))
                .DrawText("3", _font, Color.White, new PointF(10 - 2, 10 + 2))
                .DrawText("3", _font, Color.Black, new PointF(10, 10)));

            var imageBytes4 = await _httpClient.GetByteArrayAsync(task.Generations.Data[3].Generation.ImagePath);
            using var image4 = Image.Load(imageBytes4);
            image4.Mutate(o => o
                .Resize(new Size(512, 512))
                .DrawText("4", _font, Color.White, new PointF(10 - 2, 10))
                .DrawText("4", _font, Color.White, new PointF(10 - 2, 10 - 2))
                .DrawText("4", _font, Color.White, new PointF(10, 10 - 2))
                .DrawText("4", _font, Color.White, new PointF(10 + 2, 10 - 2))
                .DrawText("4", _font, Color.White, new PointF(10 + 2, 10))
                .DrawText("4", _font, Color.White, new PointF(10 + 2, 10 + 2))
                .DrawText("4", _font, Color.White, new PointF(10, 10 + 2))
                .DrawText("4", _font, Color.White, new PointF(10 - 2, 10 + 2))
                .DrawText("4", _font, Color.Black, new PointF(10, 10)));

            fullImage.Mutate(o =>
            {
                o.DrawImage(image1, new Point(0, 0), 1f);
                o.DrawImage(image2, new Point(512, 0), 1f);
                o.DrawImage(image3, new Point(0, 512), 1f);
                o.DrawImage(image4, new Point(512, 512), 1f);
            });

            using var stream = new MemoryStream();
            await fullImage.SaveAsPngAsync(stream);
            stream.Seek(0, SeekOrigin.Begin);

            var credits = await _dalleClient.GetRemainingCredits();

            var caption = $"Credits left: {credits.AggregateCredits}{Environment.NewLine}Download images:";
            var buttons = generationIds.Select((x, i) => InlineKeyboardButton.WithCallbackData($"{i + 1}", x));
            var downloadKeyboard = new InlineKeyboardMarkup(buttons);

            var message =
                await botClient.SendPhotoAsync(update.Message.Chat.Id, new InputOnlineFile(stream), caption, null, null, null, null, update.Message.MessageId, null, downloadKeyboard);

            using var db = new DalleDbContext();
            for (int i = 0; i < generationIds.Count(); i++)
            {
                db.Generations.Add(new Data.Models.Generation
                {
                    Id = generationIds[i],
                    TaskId = task.Id,
                    MessageId = message.MessageId
                });
            }

            await db.SaveChangesAsync();
        }

        public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var currentColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(exception);
            Console.ForegroundColor = currentColor;

            if (exception is ApiRequestException apiRequestException)
            {
                await Task.Delay(TimeSpan.FromSeconds(apiRequestException.Parameters?.RetryAfter ?? 5), cancellationToken);
            }
        }
    }
}