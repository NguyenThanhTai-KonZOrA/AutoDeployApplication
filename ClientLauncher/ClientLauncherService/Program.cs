using ClientLauncherService;

var builder = Host.CreateApplicationBuilder(args);

// Add Windows Service support
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "ClientLauncher Deployment Service";
});

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Add the worker
builder.Services.AddHostedService<DeploymentWorker>();

var host = builder.Build();
host.Run();
