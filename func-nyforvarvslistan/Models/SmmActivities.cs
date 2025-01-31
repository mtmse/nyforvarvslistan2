using Newtonsoft.Json;

namespace func_nyforvarvslistan.Models
{

    public class SmmActivities
    {

        [JsonProperty("ActivityNumber")]
        public string ActivityNumber { get; set; }
    }
}