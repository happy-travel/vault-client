using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HappyTravel.VaultClient
{
    public readonly struct VaultResponse
    {
        [JsonConstructor]
        public VaultResponse(JObject? auth, JObject? data, List<string>? errors) 
            => (Auth, Data, Errors) = (auth, data, errors);
        

        [JsonProperty("auth")]
        public JObject? Auth { get; }

        [JsonProperty("data")]
        public JObject? Data { get; }

        [JsonProperty("errors")]
        public List<string>? Errors { get; }
    }
}
