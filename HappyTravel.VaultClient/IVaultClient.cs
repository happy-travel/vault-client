using System.Collections.Generic;
using System.Threading.Tasks;

namespace HappyTravel.VaultClient
{
    public interface IVaultClient
    {
        void Dispose();

        Task<Dictionary<string, string>> Get(string secret);

        Task Login(string token);
    }
}