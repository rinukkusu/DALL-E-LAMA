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

namespace DALL_E_LAMA
{
    internal class UpdateHandler : IUpdateHandler
    {
        private static readonly Regex _messageRegex = new Regex(@"^/(?<command>[^\s$@]+)(?<botname>@[^\s$]+)?\s?(?<arguments>.*)$");

        private static readonly string DALLE_API_TOKEN = Environment.GetEnvironmentVariable("DALLE_API_TOKEN");
        private readonly DalleApiClient _dalleClient = new DalleApiClient(DALLE_API_TOKEN);
        private readonly HttpClient _httpClient = new HttpClient();

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(update.Message?.Text) || !update.Message.Text.StartsWith("/") || update.Message.ForwardFrom != null)
                return;

            Console.WriteLine($"{update.Message.From.Username}: {update.Message.Text}");

            try
            {
                var messageMatch = _messageRegex.Match(update.Message.Text);
                var command = messageMatch.Groups["command"].Value;
                var arguments = messageMatch.Groups["arguments"].Value;

                switch (command)
                {
                    case "dalle":
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
                            {
                                throw new Exception(task?.StatusInformation?.Message);
                            }
                            else
                            {
                                var generationIds = task.Generations.Data.Select(x => x.Id).ToList();
                                List<InputMediaPhoto> images = new();
                                List<MemoryStream> streams = new();

                                foreach (var generation in task.Generations.Data)
                                {
                                    var imageBytes = await _httpClient.GetByteArrayAsync(generation.Generation.ImagePath);
                                    using var image = Image.Load(imageBytes);

                                    var stream = new MemoryStream();
                                    image.SaveAsJpeg(stream);
                                    stream.Seek(0, SeekOrigin.Begin);

                                    streams.Add(stream);
                                    images.Add(new InputMediaPhoto(new InputMedia(stream, $"{generation.Id}.jpg")));
                                }

                                var credits = await _dalleClient.GetRemainingCredits();

                                images.First().Caption = $"Credits left: {credits.AggregateCredits}";

                                var messages = await botClient.SendMediaGroupAsync(update.Message.Chat.Id, images, null, null, update.Message.MessageId);

                                using var db = new DalleDbContext();
                                for (int i = 0; i < messages.Length; i++)
                                {
                                    db.Generations.Add(new Data.Models.Generation
                                    {
                                        Id = generationIds[i],
                                        MessageId = messages[i].MessageId
                                    });
                                }
                                await db.SaveChangesAsync();

                                foreach (var s in streams)
                                {
                                    s.Dispose();
                                }
                            }

                            break;
                        }

                    case "credits":
                        {
                            var credits = await _dalleClient.GetRemainingCredits();

                            await botClient.SendTextMessageAsync(update.Message.Chat.Id, $"Credits left: {credits.AggregateCredits}", null, null, null, null, null, update.Message.MessageId);

                            break;
                        }

                    case "download":
                    case "get":
                        {
                            if (update.Message.ReplyToMessage == null || update.Message.ReplyToMessage.Photo == null)
                                throw new Exception("You need to reply to an image I sent.");

                            using var db = new DalleDbContext();
                            var generation = db.Generations.FirstOrDefault(x => x.MessageId == update.Message.ReplyToMessage.MessageId);
                            if (generation == null)
                                throw new Exception("Sorry, this image is not in my database.");

                            var imageBytes = await _dalleClient.DownloadGeneration(generation.Id);
                            using var stream = new MemoryStream(imageBytes);

                            await botClient.SendDocumentAsync(update.Message.Chat.Id, new InputOnlineFile(stream, $"{generation.Id}.jpg"), null, null, null, null, null, null, null, update.Message.MessageId);

                            break;
                        }

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await botClient.SendTextMessageAsync(update.Message.Chat.Id, ex.Message, null, null, null, null, null, update.Message.MessageId);
            }
        }

        public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var currentColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(exception);
            Console.ForegroundColor = currentColor;

            return Task.CompletedTask;
        }
    }
}