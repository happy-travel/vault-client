﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HappyTravel.VaultClient
{
    public interface IVaultClient : IDisposable
    {
        Task<Dictionary<string, string>> Get(string secret);
        Task Login(string token, LoginMethod loginMethod = LoginMethod.Role);
        Task<(string Certificate, string PrivateKey)> IssueCertificate(string role, string name);
    }
}