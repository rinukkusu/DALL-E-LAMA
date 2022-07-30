using Newtonsoft.Json;

namespace DALL_E_LAMA.DalleApi.Models
{
    public class CreateTaskResponse
    {
        [JsonProperty("object")]
        public string ObjectType { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("created")]
        public int CreatedTimestamp { get; set; }

        [JsonProperty("task_type")]
        public string TaskType { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("status_information")]
        public StatusInformation? StatusInformation { get; set; }

        [JsonProperty("prompt_id")]
        public string PromptId { get; set; }

        [JsonProperty("prompt")]
        public Prompt Prompt { get; set; }

        [JsonProperty("generations")]
        public Generations? Generations { get; set; }
    }

    public class StatusInformation
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public class Prompt
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("object")]
        public string ObjectType { get; set; }

        [JsonProperty("created")]
        public int CreatedTimestamp { get; set; }

        [JsonProperty("prompt_type")]
        public string PromptType { get; set; }

        [JsonProperty("prompt")]
        public PromptCaption PromptCaption { get; set; }

        [JsonProperty("parent_generation_id")]
        public string? ParentGenerationId { get; set; }
    }

    public class PromptCaption
    {
        [JsonProperty("caption")]
        public string Caption { get; set; }
    }

    public class Generations
    {
        [JsonProperty("object")]
        public string ObjectType { get; set; }

        [JsonProperty("data")]
        public GenerationEntry[] Data { get; set; }
    }

    public class GenerationEntry
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("object")]
        public string ObjectType { get; set; }

        [JsonProperty("created")]
        public int CreatedTimestamp { get; set; }

        [JsonProperty("generation_type")]
        public string GenerationType { get; set; }

        [JsonProperty("generation")]
        public Generation Generation { get; set; }

        [JsonProperty("task_id")]
        public string TaskId { get; set; }

        [JsonProperty("prompt_id")]
        public string PromptId { get; set; }

        [JsonProperty("is_public")]
        public bool IsPublic { get; set; }
    }

    public class Generation
    {
        [JsonProperty("image_path")]
        public string ImagePath { get; set; }
    }
}