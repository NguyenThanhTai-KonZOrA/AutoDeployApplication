using ClientLauncher.Common.ApiClient;
using ClientLauncher.Common.MemoryCache;
using ClientLauncher.Common.SystemConfiguration;
using ClientLauncher.Implement.ApplicationDbContext;
using ClientLauncher.Implement.Repositories;
using ClientLauncher.Implement.Repositories.Interface;
using ClientLauncher.Implement.Services;
using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.UnitOfWork;
using ClientLauncher.Implement.ViewModels;
using ClientLauncherAPI.WindowHelpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;
using System.Security.Claims;
using System.Text;

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
    Console.WriteLine($"NLog configuration loaded. Targets: {string.Join(", ", config.AllTargets.Select(t => t.Name))}");
}

try
{

    logger.Info("🟢 Deployment Manager API initializing...");

    var builder = WebApplication.CreateBuilder(args);

    // ================================================================
    // 🔧 Configure Request Size Limits (FIX 413 Error)
    // ================================================================

    // Configure Kestrel
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.Limits.MaxRequestBodySize = 524288000; // 500 MB
        serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
        serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
    });

    // Configure IIS
    builder.Services.Configure<IISServerOptions>(options =>
    {
        options.MaxRequestBodySize = 524288000; // 500 MB
    });

    // Configure Form Options
    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 524288000; // 500 MB
        options.ValueLengthLimit = 524288000;
        options.MultipartHeadersLengthLimit = 524288000;
    });

    // ================================================================
    // 🔧 Configure Logging
    // ================================================================
    builder.Logging.ClearProviders();
    builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
    builder.Host.UseNLog();  // ✅ Connects NLog to ASP.NET Core pipeline

    // JWT Auth
    var jwtSection = builder.Configuration.GetSection("Jwt");
    var jwtKey = jwtSection["Key"];
    var jwtIssuer = jwtSection["Issuer"];
    var jwtAudience = jwtSection["Audience"];

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role
            };
        });

    // Database
    builder.Services.AddDbContext<DeploymentManagerDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
    builder.Services.AddMemoryCache();
    builder.Services.AddHttpClient<IApiClient, ApiClient>();
    builder.Services.Configure<DeploymentSettings>(builder.Configuration.GetSection("DeploymentSettings"));

    builder.Services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<DeploymentSettings>>().Value);
    builder.Services.AddSingleton<TokenValidationService>();
    builder.Services.AddSingleton<ISystemConfiguration, SystemConfiguration>();
    builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
    // REPOSITORIES
    builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
    builder.Services.AddScoped<IInstallationLogRepository, InstallationLogRepository>();
    builder.Services.AddScoped<IPackageVersionRepository, PackageVersionRepository>();
    builder.Services.AddScoped<IDeploymentHistoryRepository, DeploymentHistoryRepository>();
    builder.Services.AddScoped<IApplicationCategoryRepository, ApplicationCategoryRepository>();
    builder.Services.AddScoped<IDownloadStatisticRepository, DownloadStatisticRepository>();
    builder.Services.AddScoped<IIconsRepository, IconsRepository>();
    builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
    builder.Services.AddTransient<IEmployeeRepository, EmployeeRepository>();
    builder.Services.AddScoped<IRoleRepository, RoleRepository>();
    builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
    builder.Services.AddScoped<IEmployeeRoleRepository, EmployeeRoleRepository>();
    builder.Services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
    // UNIT OF WORK
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    // SERVICES
    builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
    builder.Services.AddScoped<IAppCatalogService, AppCatalogService>();
    builder.Services.AddScoped<IManifestService, ManifestService>();
    builder.Services.AddScoped<IServerManifestService, ServerManifestService>();
    builder.Services.AddScoped<IVersionService, VersionService>();
    builder.Services.AddScoped<IInstallationService, InstallationService>();
    builder.Services.AddScoped<IPackageVersionService, PackageVersionService>();
    builder.Services.AddScoped<IApplicationManagementService, ApplicationManagementService>();
    builder.Services.AddScoped<ICategoryService, CategoryService>();
    builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
    builder.Services.AddScoped<IDeploymentService, DeploymentService>();
    builder.Services.AddHttpClient();
    builder.Services.AddScoped<IApplicationManifestRepository, ApplicationManifestRepository>();
    builder.Services.AddScoped<IManifestManagementService, ManifestManagementService>();
    builder.Services.AddScoped<IInstallationLogService, InstallationLogService>();
    builder.Services.AddScoped<IIconsService, IconsService>();
    builder.Services.AddScoped<IAuditLogService, AuditLogService>();
    builder.Services.AddTransient<IEmployeeService, EmployeeService>();
    builder.Services.AddScoped<IRoleService, RoleService>();
    builder.Services.AddScoped<IPermissionService, PermissionService>();
    builder.Services.AddScoped<IEmployeeRoleService, EmployeeRoleService>();

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

    // ================================================================
    // 🧩 Register Controllers, Swagger
    // ================================================================
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please enter a valid token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "Bearer"
        });
        options.SwaggerDoc("v1", new() { Title = "Deployment Manager API", Version = "v1" });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
    });

    var app = builder.Build();

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(
            Path.Combine(builder.Environment.ContentRootPath, "Packages")),
        RequestPath = "/packages"
    });

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "Icons")),
        RequestPath = "/Icons"
    });

    // Enable CORS
    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();

    // Seed database
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<DeploymentManagerDbContext>();
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
    app.MapControllers();

    logger.Info("✅ API started successfully. Listening on configured ports...");
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