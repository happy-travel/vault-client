using System;

namespace HappyTravel.VaultClient
{
    public class VaultOptions
    {
        public string Engine { get; set; } = "secret";
        public string Role { get; set; }
        public Uri Url { get; set; }
    }
}
