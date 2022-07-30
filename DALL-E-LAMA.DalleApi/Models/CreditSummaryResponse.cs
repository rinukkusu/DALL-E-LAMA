using Newtonsoft.Json;

namespace DALL_E_LAMA.DalleApi.Models
{
    public class CreditSummaryResponse
    {
        [JsonProperty("aggregate_credits")]
        public int AggregateCredits { get; set; }

        [JsonProperty("next_grant_ts")]
        public int NextGrantTimestamp { get; set; }

        [JsonProperty("breakdown")]
        public Breakdown Breakdown { get; set; }

        [JsonProperty("object")]
        public string ObjectType { get; set; }
    }

    public class Breakdown
    {
        [JsonProperty("free")]
        public int Free { get; set; }

        [JsonProperty("paid_dalle_15_115")]
        public int Paid { get; set; }
    }
}