using ClientLancher.Implement.ApplicationDbContext;
using ClientLancher.Implement.Services;
using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using NLog;
using NLog.Web;

// ================================================================
// 🟢 NLog initialization (before builder)
// ================================================================
var logger = LogManager.Setup()
    .LoadConfigurationFromFile("NLog.config")
    .GetCurrentClassLogger();

var config = LogManager.Configuration;
if (config == null)
{
    Console.WriteLine("❌ NLog configuration is NULL — failed to load.");
}
else
{
    Console.WriteLine($"✅ NLog configuration loaded. Targets: {string.Join(", ", config.AllTargets.Select(t => t.Name))}");
}

try
{

    logger.Info("🟢 ClientLauncher API initializing...");

    var builder = WebApplication.CreateBuilder(args);

    // ================================================================
    // 🔧 Configure Logging
    // ================================================================
    builder.Logging.ClearProviders();
    builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
    builder.Host.UseNLog();

    // Database
    builder.Services.AddDbContext<ClientLancherDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Unit of Work
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    // Services
    builder.Services.AddScoped<IManifestService, ManifestService>();
    builder.Services.AddScoped<IServerManifestService, ServerManifestService>();
    builder.Services.AddScoped<IUpdateService, UpdateService>();
    builder.Services.AddScoped<IVersionService, VersionService>();
    builder.Services.AddScoped<IAppCatalogService, AppCatalogService>();
    builder.Services.AddScoped<IInstallationService, InstallationService>();
    builder.Services.AddHttpClient();

    // Add CORS if needed
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Client Launcher API", Version = "v1" });
    });

    var app = builder.Build();

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(
            Path.Combine(builder.Environment.ContentRootPath, "Packages")),
        RequestPath = "/packages"
    });
    // Enable CORS
    app.UseCors("AllowAll");

    // Seed database
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ClientLancherDbContext>();
        await context.Database.MigrateAsync();
        await DbSeeder.SeedAsync(context);
    }

    //if (app.Environment.IsDevelopment())
    //{
    app.UseSwagger();
    app.UseSwaggerUI();
    //}

    app.UseMiddleware<ApiMiddleware>();
    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    logger.Fatal(ex, "❌ API startup terminated unexpectedly.");
    throw;
}
finally
{
    LogManager.Shutdown();
}