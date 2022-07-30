using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DALL_E_LAMA.DalleApi.Models
{
    public class CreateTaskRequest
    {
        [JsonProperty("task_type")]
        public string TaskType { get; set; }

        [JsonProperty("prompt")]
        public CreateTask_PromptRequest Prompt { get; set; }
    }

    public class CreateTask_PromptRequest
    {
        [JsonProperty("caption")]
        public string Caption { get; set; }

        [JsonProperty("batch_size")]
        public int BatchSize { get; set; }
    }
}