using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HappyTravel.VaultClient
{
    public class VaultResponse
    {
        [JsonProperty("auth")]
        public JObject Auth { get; set; }

        [JsonProperty("data")]
        public JObject Data { get; set; }

        [JsonProperty("errors")]
        public List<string> Errors { get; set; }
    }
}
