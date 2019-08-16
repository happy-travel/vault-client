using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace HappyTravel.VaultClient
{
    public class VaultClient : IDisposable, IVaultClient
    {
        public VaultClient(IHttpClientFactory clientFactory, ILoggerFactory loggerFactory, IOptions<VaultOptions> vaultOptions)
        {
            _client = clientFactory.CreateClient();
            _logger = loggerFactory?.CreateLogger<VaultClient>() ?? new NullLogger<VaultClient>();
            _options = vaultOptions?.Value ?? throw new ArgumentNullException(nameof(vaultOptions));

            _serializer = new JsonSerializer();

            _client.BaseAddress = _options.Url;
        }


        public void Dispose() => _client?.Dispose();


        public async Task<Dictionary<string, string>> Get(string secret)
        {
            if (_loginSemaphore.CurrentCount == 0)
                throw new Exception("Login procedure not finished");

            var response = await _client.GetAsync($"{_options.Engine}/data/{secret}");
            var context = await GetContent(response);

            return context.Data["data"].ToObject<Dictionary<string, string>>();
        }


        public async Task<(string Certificate, string PrivateKey)> IssueCertificate(string role, string name)
        {
            if (_loginSemaphore.CurrentCount == 0)
                throw new Exception("Login procedure not finished");

            var requestContent = JsonConvert.SerializeObject(new {common_name = name});
            var result = await _client.PostAsync($"pki/issue/{role}", new StringContent(requestContent));
            var context = await GetContent(result);

            return (GetStringFromData(context, "certificate"), GetStringFromData(context, "private_key"));

            string GetStringFromData(VaultResponse response, string key) => response.Data[key].ToObject<string>();
        }


        public async Task Login(string token, LoginMethod loginMethod = LoginMethod.Role)
        {
            await _loginSemaphore.WaitAsync();
            try
            {
                _logger.Log(LogLevel.Trace, "Logging in into Vault...");
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

        private void SetAuthTokenHeader(string token)
        {
            if (_client.DefaultRequestHeaders.Contains(AuthHeader))
                _client.DefaultRequestHeaders.Remove(AuthHeader);

            _client.DefaultRequestHeaders.Add(AuthHeader, token);
        }


        private async Task<VaultResponse> GetContent(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                throw new Exception();

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
        private readonly SemaphoreSlim _loginSemaphore = new SemaphoreSlim(1);
        private readonly VaultOptions _options;
        private readonly JsonSerializer _serializer;
    }
}