# Configuration Placeholder Resolver .NET Configuration Provider

> dotnet add package PW.Extensions.Configuration.Placeholder

``` json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  },
  "SomeArray": ["a", "d"],
  "EmptyKey": "",
  "ResolvedPlaceholderFromEnvVariables": "${PATH??NotFound}",
  "UnresolvedPlaceholder": "${SomKeyNotFound??NotFound}",
  "ResolvedPlaceholderFromJson": "${Logging:LogLevel:System??${Logging:LogLevel:Default??NotFound}}",
  "ResolvedEmpty": "${EmptyKey||abc}",
  "IndexPolaceholder": "${SomeArray[1]}abc"
}
```

``` C#
using Microsoft.Extensions.Configuration;
...

var builder = new ConfigurationBuilder()
    .AddXXX()
    // Add Placeholder resolver
    .AddPlaceholderResolver();
Configuration = builder.Build();
...
```
