using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace HappyTravel.VaultClient.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddVaultClient(this IServiceCollection services, Action<VaultOptions> options)
        {
            services.AddHttpClient<VaultClient>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(10))
                .AddPolicyHandler(GetDefaultRetryPolicy());

            return services
                .RegisterOptions(options)
                .AddTransient<IVaultClient, VaultClient>();
        }


        private static IServiceCollection RegisterOptions(this IServiceCollection services, Action<VaultOptions> options)
        {
            if (options != null)
                services.Configure(options);

            return services;
        }


        private static IAsyncPolicy<HttpResponseMessage> GetDefaultRetryPolicy()
        {
            var jitter = new Random();

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, attempt 
                    => TimeSpan.FromMilliseconds(Math.Pow(500, attempt)) + TimeSpan.FromMilliseconds(jitter.Next(0, 100)));
        }
    }
}
