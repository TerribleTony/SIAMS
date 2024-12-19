using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using SIAMS.Data;
using SIAMS.Services;

var builder = WebApplication.CreateBuilder(args);

// Load sensitive secrets during development
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// Resolve the database connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? Environment.GetEnvironmentVariable("DATABASE_URL");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string is not set.");
}

// Configure Data Protection
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
    .SetApplicationName("SIAMS")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// Configure Database Context
if (connectionString.StartsWith("postgres://"))
{
    var uri = new Uri(connectionString);
    var dbUser = uri.UserInfo.Split(':')[0];
    var dbPass = uri.UserInfo.Split(':')[1];
    var host = uri.Host;
    var port = uri.Port;
    var dbName = uri.AbsolutePath.Trim('/');

    connectionString = $"Host={host};Port={port};Database={dbName};Username={dbUser};Password={dbPass};SSL Mode=Require;Trust Server Certificate=true;";
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, opt => opt.MigrationsAssembly("SIAMS")));

// Configure Email Services
var emailConfig = builder.Configuration.GetSection("Email").Get<EmailConfig>();
if (emailConfig == null)
{
    throw new InvalidOperationException("Email configuration is missing.");
}
builder.Services.AddSingleton(emailConfig);
builder.Services.AddScoped<IEmailService, EmailService>();

// Configure Authentication & Authorization
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// Add MVC Services
builder.Services.AddControllersWithViews();

// Configure Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Build the App
var app = builder.Build();

// Configure the HTTP Request Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();  // Detailed Errors in Development
}
else
{
    app.UseExceptionHandler("/Home/Error");  // Custom Error Page
    app.UseHsts();  // Enforce HTTPS
}

// Apply Database Migrations & Seed Data
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        DbInitializer.Seed(context, builder.Configuration);  // Initialize Database
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database seeding failed: {ex.Message}");
    }
}

// Configure Middleware
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Define Default Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
