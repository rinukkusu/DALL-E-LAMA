using DALL_E_LAMA.DalleApi.Models;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DALL_E_LAMA.DalleApi
{
    public class DalleApiClient
    {
        private readonly HttpClient _client;
        private readonly string _bearerToken;

        public DalleApiClient(string bearerToken)
        {
            if (string.IsNullOrWhiteSpace(bearerToken))
                throw new ArgumentNullException(nameof(bearerToken));

            _bearerToken = bearerToken;
            _client = new HttpClient
            {
                BaseAddress = new Uri("https://labs.openai.com/api/labs/")
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
        }

        private async Task<string> Try(Func<Task<HttpResponseMessage>> func)
        {
            HttpResponseMessage? response = null;
            string? json = null;
            string? errors = null;

            for (var i = 0; i < 3; i++)
            {
                try
                {
                    response = await func();
                    if (response != null)
                    {
                        json = await response.Content.ReadAsStringAsync();
                    }

                    if (response != null && json != null && !json.StartsWith("<"))
                        break;
                }
                catch (Exception ex)
                {
                    errors += $"{ex.Message}{Environment.NewLine}";
                }
            }

            if (json == null)
            {
                throw new Exception($"Tried to connect 3 times, but got an error.{Environment.NewLine}{errors}".Trim());
            }

            return json;
        }

        public async Task<CreditSummaryResponse?> GetRemainingCredits()
        {
            string creditJson = await Try(() => _client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "billing/credit_summary")));
            return JsonConvert.DeserializeObject<CreditSummaryResponse>(creditJson);
        }

        public async Task<CreateTaskResponse?> CreateText2ImageTask(string prompt)
        {
            var request = new CreateTaskRequest
            {
                TaskType = "text2im",
                Prompt = new CreateTask_PromptRequest
                {
                    Caption = prompt,
                    BatchSize = 4
                }
            };

            var requestJson = JsonConvert.SerializeObject(request);

            var stringContent = new StringContent(requestJson, UnicodeEncoding.UTF8, "application/json");
            string taskJson = await Try(() => _client.PostAsync("tasks", stringContent));

            return JsonConvert.DeserializeObject<CreateTaskResponse>(taskJson);
        }

        public async Task<CreateTaskResponse?> GetTask(string taskId)
        {
            string creditJson = await Try(() => _client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"tasks/{taskId}")));
            return JsonConvert.DeserializeObject<CreateTaskResponse>(creditJson);
        }

        public async Task<byte[]> DownloadGeneration(string generationId)
        {
            return await _client.GetByteArrayAsync($"generations/{generationId}/download");
        }
    }
}