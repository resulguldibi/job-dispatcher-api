using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace job_dispatcher_api.Models
{
    [Serializable]
    public class SelectionSummaryKafkaMessage
    {
        [JsonPropertyName("name")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "name field is required")]
        public string Name { get; set; }

        [JsonPropertyName("count")]

        public int Count { get; set; }


        [JsonPropertyName("user_id")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "user_id field is required")]
        public string UserId { get; set; }
    }
}
