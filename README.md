﻿# Configuration Placeholder Resolver .NET Configuration Provider

``` json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  },
  "SomeArray": ["a", "d"],
  "ResolvedPlaceholderFromEnvVariables": "${PATH??NotFound}",
  "UnresolvedPlaceholder": "${SomKeyNotFound??NotFound}",
  "ResolvedPlaceholderFromJson": "${Logging:LogLevel:System?${Logging:LogLevel:Default??NotFound}}",
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