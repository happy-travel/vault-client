using System;

namespace HappyTravel.VaultClient
{
    public class VaultOptions
    {
        public Uri BaseUrl { get; set; }
        public string Engine { get; set; } = "secret";
        public string Role { get; set; }
    }
}
