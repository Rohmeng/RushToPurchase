# rush to purchase 
😃 A simple web API implemented by [ASP.NET Core 6](https://docs.microsoft.com/zh-cn/aspnet/core/introduction-to-aspnet-core?view=aspnetcore-6.0), for the business about rush to purchase.

#### Technical Terms
- ASP.NET Core 6
- Serilog
- FreeSql
- EF Core
- Rabbitmq
- Nacos
- Docker
- Kubernetes

#### Dependencies
| Package name                              | Version                                                      |
| ----------------------------------------- | ------------------------------------------------------------ |
| AutoMapper                                | [![NuGet](https://img.shields.io/badge/nuget-v11.0.1-blue)](https://www.nuget.org/packages/AutoMapper/) |
| MediatR                                   | [![nuget](https://img.shields.io/badge/nuget-v10.0.1-blue)](https://www.nuget.org/packages/MediatR/) |
| Ardalis.Specification                     | [![nuget](https://img.shields.io/badge/nuget-v6.0.1-blue)](https://www.nuget.org/packages/Ardalis.Specification/) |
| Ardalis.Specification.EntityFrameworkCore | [![nuget](https://img.shields.io/badge/nuget-v6.0.1-blue)](https://www.nuget.org/packages/Ardalis.Specification.EntityFrameworkCore/) |
| CSRedisCore                               | [![nuget](https://img.shields.io/badge/nuget-v3.6.9-blue)](https://www.nuget.org/packages/CSRedisCore/) |
| EasyCaching.Core                          | [![nuget](https://img.shields.io/badge/nuget-v1.5.1-blue)](https://www.nuget.org/packages/EasyCaching.Core/) |
| EasyCaching.CSRedis                       | [![nuget](https://img.shields.io/badge/nuget-v1.5.1-blue)](https://www.nuget.org/packages/EasyCaching.CSRedis/) |
| EasyCaching.Serialization.MessagePack     | [![nuget](https://img.shields.io/badge/nuget-v1.5.1-blue)](https://www.nuget.org/packages/EasyCaching.Serialization.MessagePack/) |
| FireflySoft.RateLimit.AspNetCore          | [![nuget](https://img.shields.io/badge/nuget-v2.0.2rc1-blue)](https://www.nuget.org/packages/FireflySoft.RateLimit.AspNetCore/) |
| FreeSql                                   | [![nuget](https://img.shields.io/badge/nuget-v3.0.100-blue)](https://www.nuget.org/packages/FreeSql/) |
| FreeSql.Provider.MySqlConnector           | [![nuget](https://img.shields.io/badge/nuget-v3.0.100-blue)](https://www.nuget.org/packages/FreeSql.Provider.MySqlConnector/) |
| FreeSql.Repository                        | [![nuget](https://img.shields.io/badge/nuget-v3.0.100-blue)](https://www.nuget.org/packages/FreeSql.Repository/) |
| Pomelo.EntityFrameworkCore.MySql          | [![nuget](https://img.shields.io/badge/nuget-v6.0.1-blue)](https://www.nuget.org/packages/Pomelo.EntityFrameworkCore.MySql/) |
| Serilog                                   | [![nuget](https://img.shields.io/badge/nuget-v2.10.0-blue)](https://www.nuget.org/packages/Serilog/) |
| Serilog.AspNetCore                        | [![nuget](https://img.shields.io/badge/nuget-v5.0.0-blue)](https://www.nuget.org/packages/Serilog.AspNetCore/) |
| Serilog.Sinks.File                        | [![nuget](https://img.shields.io/badge/nuget-v5.0.0-blue)](https://www.nuget.org/packages/Serilog.Sinks.File/) |
| Serilog.Sinks.MariaDB                     | [![nuget](https://img.shields.io/badge/nuget-v1.0.1-blue)](https://www.nuget.org/packages/Serilog.Sinks.MariaDB/) |
| RabbitMQ.Client                           | [![nuget](https://img.shields.io/badge/nuget-v6.2.4-blue)](https://www.nuget.org/packages/RabbitMQ.Client/) |
| nacos-sdk-csharp.aspnetcore               | [![nuget](https://img.shields.io/badge/nuget-v1.3.2-blue)](https://www.nuget.org/packages/nacos-sdk-csharp.aspnetcore/) |
| nacos-sdk-csharp.extensions.configuration | [![nuget](https://img.shields.io/badge/nuget-v1.3.2-blue)](https://www.nuget.org/packages/nacos-sdk-csharp.extensions.configuration/) |
| Swashbuckle.AspNetCore                    | [![nuget](https://img.shields.io/badge/nuget-v6.2.3-blue)](https://www.nuget.org/packages/Swashbuckle.AspNetCore/) |
| xunit                                     | [![nuget](https://img.shields.io/badge/nuget-v2.4.1-blue)](https://www.nuget.org/packages/xuint/) |
| BenchmarkDotNet                           | [![nuget](https://img.shields.io/badge/nuget-v0.13.1-blue)](https://www.nuget.org/packages/BenchmarkDotNet/) |

### Attention

- There is no docker0 bridge on macOS

  Because of the way networking is implemented in Docker Desktop for Mac, you cannot see a `docker0` interface on the host. This interface is actually within the virtual machine.
  The host has a changing IP address (or none if you have no network access). We recommend that you connect to the special DNS name `host.docker.internal` which resolves to the internal IP address used by the host. 


- Configure endpoints for the ASP.NET Core Kestrel web server
```dockerfile
ENV ASPNETCORE_URLS=http://*:7021
```

- Supporting integration tests with WebApplicationFactory in .NET 6

  [Code samples migrated to the new minimal hosting model in 6.0 | Microsoft Docs](https://docs.microsoft.com/en-us/aspnet/core/migration/50-to-60-samples?view=aspnetcore-6.0#test-with-webapplicationfactory-or-testserver)
