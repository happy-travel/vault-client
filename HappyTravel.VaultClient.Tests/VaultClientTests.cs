using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace HappyTravel.VaultClient.Tests
{
    public class VaultClientTests
    {
        [Fact]
        public async Task Login_ShouldLogin()
        {
            var httpClient = new HttpClient(new HttpClientHandler(), false);
            var clientFactoryMock = new Mock<IHttpClientFactory>();
            clientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var options = Options.Create(new VaultOptions
            {
                Engine = "secrets",
                Role = "edo",
                Url = new Uri("https://vault.dev.happytravel.com/v1/")
            });

            var client = new VaultClient(clientFactoryMock.Object, new NullLoggerFactory(), options);

            await client.Login("");
            var secret = await client.Get("edo/connection-string");
        }
    }
}
