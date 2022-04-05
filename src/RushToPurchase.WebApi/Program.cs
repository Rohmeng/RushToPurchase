using System.Diagnostics;
using FireflySoft.RateLimit.AspNetCore;
using FireflySoft.RateLimit.Core.InProcessAlgorithm;
using FireflySoft.RateLimit.Core.Rule;
using FreeSql;
using FreeSql.Internal;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.EntityFrameworkCore;
using Nacos.AspNetCore.V2;
using RushToPurchase.Application;
using RushToPurchase.Infra.Data;
using RushToPurchase.Infra.Data.FreeSql;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {Message}{NewLine}{Exception}")
    .CreateBootstrapLogger();

Log.Information("Starting up");

try
{
    var builder = WebApplication.CreateBuilder(args);
    // Add services to the container.

    builder.Host.ConfigureAppConfiguration((c, b) =>
    {
        var config = b.Build();
        b.AddNacosV2Configuration(config.GetSection("NacosConfig"), logAction: x => x.AddSerilog(Log.Logger));
    });

    #region Serilog 日志

    // builder.Logging.ClearProviders();
    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration));

    #endregion

    #region EasyCaching Redis 缓存

    builder.Services.AddEasyCaching(option =>
    {
        option.UseCSRedis(builder.Configuration, "CSRedis", "easycaching:csredis");
        // use MessagePack
        option.WithMessagePack(x =>
        {
            x.EnableCustomResolver = true;
            x.CustomResolvers = CompositeResolver.Create(new IFormatterResolver[]
            {
                // This can solve DateTime time zone problem
                NativeDateTimeResolver.Instance,
                ContractlessStandardResolver.Instance
            });
        }, "msgpack");
    });

    #endregion

    #region RateLimit API 限流

    builder.Services.AddRateLimit(new InProcessFixedWindowAlgorithm(
        new[]
        {
            new FixedWindowRule()
            {
                ExtractTarget = context =>
                {
                    // 提取限流目标
                    // 这里是直接从请求中提取Path作为限流目标，还可以多种组合，甚至去远程查询一些数据
                    return (context as HttpContext)?.Request.Path.Value;
                },
                CheckRuleMatching = context =>
                {
                    // 检查当前请求是否要做限流
                    // 比如有些Url是不做限流的、有些用户是不做限流的
                    var rules = builder.Configuration.GetSection("RateLimitRules").Get<List<string>>();
                    bool result = false;
                    rules?.ForEach(e =>
                    {
                        if (!result && (context as HttpContext).Request.Path.Value.Contains(e))
                        {
                            result = true;
                        }
                    });
                    return result;
                },
                Name = "default limit rule",
                LimitNumber = 10, // 限流时间窗口内的最大允许请求数量
                StatWindow = TimeSpan.FromSeconds(1) // 限流计数的时间窗口
            }
        })
    );

    #endregion

    #region EF Core: DbContetxt/Repo

    builder.Services.AddDbContext<EshopContext>(options =>
        options.UseMySql(builder.Configuration.GetConnectionString("MySql"), ServerVersion.Parse("8.0.28-mysql")));

    builder.Services.AddRepoService();

    #endregion

    #region FreeSql

    var fsql = new FreeSqlBuilder()
        .UseConnectionString(DataType.MySql, builder.Configuration.GetConnectionString("Mysql"))
        .UseAutoSyncStructure(true)
        .UseNameConvert(NameConvertType.PascalCaseToUnderscoreWithLower)
        .UseMonitorCommand(cmd => Trace.WriteLine(cmd.CommandText))
        .Build();
    //UseAutoSyncStructure(true/false)【开发环境必备】自动同步实体结构到数据库，程序运行中检查实体表是否存在，然后创建或修改
    fsql.Aop.CurdAfter += (s, e) =>
    {
        Log.Information($"ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}: FullName:{e.EntityType.FullName}" +
                        $" ElapsedMilliseconds:{e.ElapsedMilliseconds}ms, {e.Sql}");
        if (e.ElapsedMilliseconds > 200)
        {
            //耗时过长可记录日志
        }
    };
    builder.Services.AddSingleton(fsql);
    builder.Services.AddScoped<UnitOfWorkManager>();

    builder.Services.AddFreeRepository(null, typeof(FreeSqlExtension).Assembly);
    // builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(GuidRepository<>));
    // builder.Services.AddScoped(typeof(BaseRepository<>), typeof(GuidRepository<>));
    //
    // builder.Services.AddScoped(typeof(IBaseRepository<,>), typeof(DefaultRepository<,>));
    // builder.Services.AddScoped(typeof(BaseRepository<,>), typeof(DefaultRepository<,>));
    //
    // foreach (var repo in typeof(FreeSqlExtension).Assembly.GetTypes().Where(a => a.IsAbstract == false && typeof(IBaseRepository).IsAssignableFrom(a)))
    //     builder.Services.AddScoped(repo);

    #endregion

    #region rabbitmq client

    builder.Services.AddRabbitMqService();

    #endregion

    #region 应用层Services

    builder.Services.AddApplicationService();

    #endregion

    // 服务注册
    // builder.Services.AddNacosAspNet(builder.Configuration);

    #region Asp.net core Framework Service

    builder.Services.AddControllers();

    #endregion

    #region Swagger

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    #endregion

    var app = builder.Build();
    app.UseSerilogRequestLogging();
    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    // app.UseHttpsRedirection();

    app.UseAuthorization();

    app.UseRateLimit();

    app.MapControllers();

    app.MapGet("/", async context => { await context.Response.WriteAsync("Hello World"); });

    // app.Run("http://*:7021;");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}

// Make the implicit Program class public so test projects can access it
public partial class Program
{
}