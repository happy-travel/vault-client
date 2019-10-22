using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HappyTravel.VaultClient.Tests
{
    public class VaultClientTests
    {
        [Fact]
        public async Task Login_ShouldLogin()
        {
            var options = new VaultOptions
            {
                BaseUrl = new Uri("https://vault.dev.happytravel.com/v1/"),
                Role = "role",
                Engine = "secrets"
            };

            using var client = new VaultClient(options, new NullLoggerFactory());

            await client.Login("");
            var secret = await client.Get("edo/connection-string");
        }
    }
}
