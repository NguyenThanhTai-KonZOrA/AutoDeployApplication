using ClientLancher.Implement.ApplicationDbContext;
using ClientLancher.Implement.Services;
using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.UnitOfWork;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<ClientLancherDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services
builder.Services.AddScoped<IManifestService, ManifestService>();
builder.Services.AddScoped<IUpdateService, UpdateService>();
builder.Services.AddScoped<IVersionService, VersionService>();
builder.Services.AddScoped<IAppCatalogService, AppCatalogService>();
builder.Services.AddScoped<IInstallationService, InstallationService>();
builder.Services.AddHttpClient();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ClientLancherDbContext>();
    await context.Database.MigrateAsync();
    await DbSeeder.SeedAsync(context);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ApiMiddleware>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();