using Kaan.SecurityPlatform.Application;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Infrastructure;
using Kaan.SecurityPlatform.Infrastructure.Services;
using Kaan.SecurityPlatform.ScannerWorker;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/worker-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14)
    .CreateLogger();

try
{
    Log.Information("Kaan Security ScannerWorker başlıyor");

    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddSerilog(dispose: true);

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(
        builder.Configuration,
        registerHangfireServer: true,
        registerHangfireStorage: true);

    builder.Services.AddSingleton<ICurrentUser, WorkerCurrentUser>();
    builder.Services.AddSingleton<IActivityEventPublisher, NoopActivityEventPublisher>();

    builder.Services.AddHostedService<ScannerWorkerHealthLog>();

    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ScannerWorker başlatılırken hata oluştu");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
