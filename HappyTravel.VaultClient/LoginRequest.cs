using Newtonsoft.Json;

namespace HappyTravel.VaultClient
{
    public readonly struct LoginRequest
    {
        public LoginRequest(string roleId, string secretId)
        {
            RoleId = roleId;
            SecretId = secretId;
        }


        [JsonProperty("role_id")]
        public string RoleId { get; }

        [JsonProperty("secret_id")]
        public string SecretId { get; }
    }
}
