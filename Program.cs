using Microsoft.EntityFrameworkCore;
using Pidar.Areas.Identity.Data;
using Pidar.Data;
using OfficeOpenXml;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;


var builder = WebApplication.CreateBuilder(args);

// Set EPPlus license context
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

// Register ApplicationDbContext for Identity
var identityConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(identityConnectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

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

builder.Services.AddControllers()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

// Swagger configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "PIDAR Metadata API",
        Version = "v1",
        Description = "A Web API for managing Metadata",
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

var app = builder.Build();
// Simple health endpoint
app.MapHealthChecks("/health");



// Apply pending migrations on startup
// Update your migration code to handle container startup delays
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var retryCount = 5;
    var retryDelay = TimeSpan.FromSeconds(10);

    void ApplyMigrations(DbContext context, string contextName)
    {
        for (int i = 0; i < retryCount; i++)
        {
            try
            {
                logger.LogInformation("Applying migrations for {Context} (Attempt {Attempt}/{Total})", contextName, i + 1, retryCount);
                context.Database.Migrate();
                logger.LogInformation("Migrations applied for {Context}", contextName);
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Migration attempt {Attempt}/{Total} failed for {Context}", i + 1, retryCount, contextName);
                if (i == retryCount - 1)
                {
                    logger.LogCritical("All migration attempts failed for {Context}", contextName);
                    throw;
                }
                Thread.Sleep(retryDelay);
            }
        }
    }

    ApplyMigrations(services.GetRequiredService<PidarDbContext>(), "PidarDbContext");
    ApplyMigrations(services.GetRequiredService<ApplicationDbContext>(), "ApplicationDbContext");
}





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




app.MapControllerRoute(
    name: "root",
    pattern: "",
    defaults: new { controller = "Metadatas", action = "Index" });


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.MapRazorPages();

app.Run();