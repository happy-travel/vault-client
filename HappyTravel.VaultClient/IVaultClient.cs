using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HappyTravel.VaultClient
{
    public interface IVaultClient : IDisposable
    {
        /// <summary>
        ///     Gets the secret's value.
        /// </summary>
        /// <param name="secret">Vault secret</param>
        /// <returns></returns>
        Task<Dictionary<string, string>> Get(string secret);


        /// <summary>
        ///     Gets the secret's value. In case of unsuccessful response default value will return.
        /// </summary>
        /// <param name="secret">Vault secret</param>
        /// <returns></returns>
        Task<Dictionary<string, string>> GetOrDefault(string secret);


        Task<(string Certificate, string PrivateKey)> IssueCertificate(string role, string name);


        /// <summary>
        ///     Logs in Vault.
        /// </summary>
        /// <param name="token">Vault token</param>
        /// <param name="loginMethod">Login method</param>
        /// <returns></returns>
        Task Login(string token, LoginMethods loginMethod = LoginMethods.Role);
    }
}