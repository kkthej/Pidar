using Microsoft.EntityFrameworkCore;
using Pidar.Areas.Identity.Data;
using Pidar.Data;
using Microsoft.AspNetCore.Identity;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// Set EPPlus license context
ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // For non-commercial use
// ExcelPackage.LicenseContext = LicenseContext.Commercial; // For commercial use

// Register ApplicationDbContext for Identity
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Register PidarDbContext for application-specific entities  ========================================
builder.Services.AddDbContext<PidarDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PidarDbContext") ?? throw new InvalidOperationException("Connection string 'PidarDbContext' not found.")));


// Register Identity with PidarUser
builder.Services.AddDefaultIdentity<PidarUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

//Adding Swagger services:
builder.Services.AddControllersWithViews();

builder.Services.AddControllers()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "PIDAR Metadata API",
        Version = "v1",
        Description = "A Web API for managing Metadata",
    });
});




var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
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



app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication(); // Ensure this is called before UseAuthorization
app.UseAuthorization();






app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();