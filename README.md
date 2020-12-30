# Vault Client
An implementation of Simple Secret Storage client for Hashicorp's Vault for .Net Core


## Usage
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var vaultOptions = new VaultOptions
        {
            BaseUrl = new Uri(configuration["Vault:Endpoint"]),
            Engine = configuration["Vault:Engine"],
            Role = configuration["Vault:Role"]
        };
        using var vaultClient = new VaultClient.VaultClient(vaultOptions);
        vaultClient.Login(EnvironmentVariableHelper.Get("Vault:Token", configuration)).GetAwaiter().GetResult();
        
        ...
    }
}
```


## Automation

New package version is automatically published to [github packages](https://github.com/features/packages) after changes in the master branch.
