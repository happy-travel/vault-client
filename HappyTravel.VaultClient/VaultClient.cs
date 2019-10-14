using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HappyTravel.VaultClient
{
    public class VaultClient : IVaultClient
    {
        public VaultClient(VaultOptions vaultOptions, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger<VaultClient>() ?? new NullLogger<VaultClient>();
            _options = vaultOptions ?? throw new ArgumentNullException(nameof(vaultOptions));

            _client = new HttpClient {BaseAddress = _options.BaseUrl};
            _serializer = new JsonSerializer();
        }


        public void Dispose() => _client?.Dispose();


        public async Task<Dictionary<string, string>> Get(string secret)
        {
            if (_loginSemaphore.CurrentCount == 0)
                throw new Exception("Login procedure not finished");

            using var response = await _client.GetAsync($"{_options.Engine}/data/{secret}");
            var data = await GetContentData(response);
            
            return data["data"].ToObject<Dictionary<string, string>>();
        }


        public async Task<(string Certificate, string PrivateKey)> IssueCertificate(string role, string name)
        {
            if (_loginSemaphore.CurrentCount == 0)
                throw new Exception("Login procedure not finished");

            var requestContent = JsonConvert.SerializeObject(new {common_name = name});
            var result = await _client.PostAsync($"pki/issue/{role}", new StringContent(requestContent));
            var contextData = await GetContentData(result);

            return (GetStringFromData(contextData, "certificate"), GetStringFromData(contextData, "private_key"));


            static string GetStringFromData(JObject data, string key) 
                => data[key].ToObject<string>();
        }


        public async Task Login(string token, LoginMethod loginMethod = LoginMethod.Role)
        {
            await _loginSemaphore.WaitAsync();
            try
            {
                _logger.Log(LogLevel.Trace, "Logging into Vault...");
                SetAuthTokenHeader(token);
                switch (loginMethod)
                {
                    case LoginMethod.Token: return;
                    case LoginMethod.Role:
                    {
                        var roleId = await GetRoleId();
                        var secretId = await GetSecretId();
                        var roleToken = await GetToken(new LoginRequest(roleId, secretId));
                        
                        SetAuthTokenHeader(roleToken);
                        break;
                    }
                    default: throw new Exception("Invalid login method");
                }
            }
            finally
            {
                _loginSemaphore.Release();
            }
        }


        private async Task<VaultResponse> GetContent(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                throw new Exception();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var streamReader = new StreamReader(stream);
            using var jsonTextReader = new JsonTextReader(streamReader);

            var content = _serializer.Deserialize<VaultResponse>(jsonTextReader);
            if (content.Equals(default(VaultResponse)))
                throw new Exception("Vault has returned no response");

            if (content.Errors != null)
                throw new Exception(string.Join(", ", content.Errors));

            return content;
        }


        private async Task<JObject> GetContentData(HttpResponseMessage response)
        {
            var content = await GetContent(response);
            if (content.Data is null)
                throw new NullReferenceException();

            return content.Data;
        }


        private async Task<string> GetRoleId()
        {
            var response = await _client.GetAsync($"auth/approle/role/{_options.Role}/role-id");
            var data = await GetContentData(response);

            return data["role_id"].ToString();
        }


        private async Task<string> GetSecretId()
        {
            var response = await _client.PostAsync($"auth/approle/role/{_options.Role}/secret-id", null);
            var data = await GetContentData(response);

            return data["secret_id"].ToString();
        }


        private async Task<string> GetToken(LoginRequest loginRequest)
        {
            using var response = await _client.PostAsync("auth/approle/login",
                new StringContent(JsonConvert.SerializeObject(loginRequest), Encoding.UTF8, "application/json"));
            var content = await GetContent(response);
            if (content.Auth is null)
                throw new NullReferenceException("Vault returns no auth data");

            return content.Auth["client_token"].ToString();
        }


        private void SetAuthTokenHeader(string token)
        {
            if (_client.DefaultRequestHeaders.Contains(AuthHeader))
                _client.DefaultRequestHeaders.Remove(AuthHeader);

            _client.DefaultRequestHeaders.Add(AuthHeader, token);
        }


        private const string AuthHeader = "X-Vault-Token";

        private readonly HttpClient _client;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _loginSemaphore = new SemaphoreSlim(1);
        private readonly VaultOptions _options;
        private readonly JsonSerializer _serializer;
    }
}