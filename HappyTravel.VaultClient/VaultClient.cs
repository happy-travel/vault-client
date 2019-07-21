using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace HappyTravel.VaultClient
{
    public class VaultClient : IDisposable, IVaultClient
    {
        public VaultClient(HttpClient client, ILoggerFactory loggerFactory, IOptions<VaultOptions> vaultOptions)
        {
            _client = client;
            _logger = loggerFactory?.CreateLogger<VaultClient>() ?? new NullLogger<VaultClient>();
            _options = vaultOptions?.Value ?? throw new ArgumentNullException(nameof(vaultOptions));

            _serializer = new JsonSerializer();

            _client.BaseAddress = _options.Url;
        }


        public void Dispose() => _client?.Dispose();


        public async Task<Dictionary<string, string>> Get(string secret)
        {
            var response = await _client.GetAsync($"{_options.Engine}/data/{secret}");
            var context = await GetContent(response);

            return context.Data["data"].ToObject<Dictionary<string, string>>();
        }


        public async Task Login(string token)
        {
            _logger.Log(LogLevel.Trace, "Logging in into Vault...");

            _client.DefaultRequestHeaders.Add(AuthHeader, token);

            var roleId = await GetRoleId();
            var secretId = await GetSecretId();

            var roleToken = await GetToken(new LoginRequest(roleId, secretId));

            _client.DefaultRequestHeaders.Remove(AuthHeader);
            _client.DefaultRequestHeaders.Add(AuthHeader, roleToken);
        }


        private async Task<VaultResponse> GetContent(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception();
            }

            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var streamReader = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                var content = _serializer.Deserialize<VaultResponse>(jsonTextReader);
                if (content.Errors != null)
                { }

                return content;
            }
        }


        private async Task<string> GetRoleId()
        {
            var response = await _client.GetAsync($"auth/approle/role/{_options.Role}/role-id");
            var content = await GetContent(response);
            
            return content.Data["role_id"].ToString();
        }


        private async Task<string> GetSecretId()
        {
            var response = await _client.PostAsync($"auth/approle/role/{_options.Role}/secret-id", null);
            var content = await GetContent(response);

            return content.Data["secret_id"].ToString();
        }


        private async Task<string> GetToken(LoginRequest loginRequest)
        {
            var response = await _client.PostAsync("auth/approle/login",
                new StringContent(JsonConvert.SerializeObject(loginRequest), Encoding.UTF8, "application/json"));
            var content = await GetContent(response);

            return content.Auth["client_token"].ToString();
        }


        private const string AuthHeader = "X-Vault-Token";

        
        private readonly HttpClient _client;
        private readonly ILogger _logger;
        private readonly VaultOptions _options;
        private readonly JsonSerializer _serializer;
    }
}
