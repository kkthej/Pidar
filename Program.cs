using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OfficeOpenXml;
using Pidar.Areas.Identity.Data;
using Pidar.Data;
using Pidar.Helpers;
using Pidar.Services;
using System.IO;
using Hangfire;
using Hangfire.PostgreSql;
using Pidar.Infrastructure;
using Pidar.Jobs;
using System.Net.Http.Headers;
using Pidar.Integration;
using Pidar.Services.Analytics;


var builder = WebApplication.CreateBuilder(args);

ExcelPackage.License.SetNonCommercialOrganization("PIDAR");




// Register ApplicationDbContext for Identity
var identityConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(identityConnectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();


// ANALYTICS SERVICE
builder.Services.Configure<MatomoOptions>(builder.Configuration.GetSection("Matomo"));
builder.Services.AddHttpClient<IAnalyticsService, MatomoAnalyticsService>(http =>
{
    http.Timeout = TimeSpan.FromSeconds(8);
});

//XNAT UPLOADER
builder.Services.AddScoped<IDatasetExportService, DatasetExportService>();

builder.Services.AddHttpClient<IXnatUploader, XnatUploader>((sp, http) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();

    var user = cfg["Xnat:User"];
    var pass = cfg["Xnat:Pass"];

    if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        throw new InvalidOperationException("XNAT credentials not configured");

    var token = Convert.ToBase64String(
        System.Text.Encoding.UTF8.GetBytes($"{user}:{pass}")
    );

    http.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Basic", token);
});


// Register PidarDbContext with retry policy and sensitive data logging
var pidarConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<PidarDbContext>(options =>
    options.UseNpgsql(pidarConnectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    })
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()));

// Register Identity
builder.Services.AddDefaultIdentity<PidarUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Add services to the container
builder.Services.AddControllersWithViews();
// Register OntologyIndexService
builder.Services.AddScoped<OntologyIndexService>();
builder.Services.AddScoped<OntologySearchService>();
// Register DatasetXnatSyncJob
builder.Services.AddScoped<DatasetXnatSyncJob>();



// Register CategoryProvider for use in all Razor pages
builder.Services.AddSingleton<ICategoryProvider, CategoryProvider>();

builder.Services.AddControllers()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

// Configure Data Protection to persist keys
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/root/.aspnet/DataProtection-Keys"))
    .SetApplicationName("PidarWeb"); 

// Swagger configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "PIDAR Dataset API",
        Version = "v1",
        Description = "A Web API for managing Dataset",
    });
    // Ignore non-API controllers (e.g. MVC views)
    c.DocInclusionPredicate((docName, apiDesc) =>
        apiDesc.RelativePath?.StartsWith("api/", StringComparison.OrdinalIgnoreCase) ?? false);
});

// Add to your services configuration
builder.Services.AddHealthChecks()
    // Entity Framework Core health checks only
    .AddDbContextCheck<PidarDbContext>()
    .AddDbContextCheck<ApplicationDbContext>();

builder.Services.AddHangfire(cfg =>
{
    var cs =
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? builder.Configuration["Hangfire:ConnectionString"];

    cfg.UseSimpleAssemblyNameTypeSerializer()
       .UseRecommendedSerializerSettings()
       .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(cs));
});

builder.Services.AddHangfireServer();



var app = builder.Build();
// Simple health endpoint
app.MapHealthChecks("/health");


// Apply migrations ONLY when explicitly enabled
if (builder.Configuration.GetValue<bool>("ApplyMigrations"))
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogWarning("ApplyMigrations=true → applying EF Core migrations");

        services.GetRequiredService<PidarDbContext>().Database.Migrate();
        services.GetRequiredService<ApplicationDbContext>().Database.Migrate();

        logger.LogInformation("EF Core migrations completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Migration failed while ApplyMigrations=true");
        throw; // FAIL FAST only when explicitly requested
    }
}
else
{
    app.Logger.LogInformation("ApplyMigrations=false → skipping EF Core migrations");
}



// Run startup jobs
await StartupJobs.RunAsync(app.Services);


// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment() || app.Environment.IsStaging() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}




app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAllowAllDashboardAuthorizationFilter() }
});




app.MapControllerRoute(
    name: "root",
    pattern: "",
    defaults: new { controller = "Datasets", action = "Index" });


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.MapRazorPages();

app.Run();