using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SIAMS.Data;
using SIAMS.Services;  

var builder = WebApplication.CreateBuilder(args);

// Determine the correct connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                     ?? Environment.GetEnvironmentVariable("DATABASE_URL");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string is not set.");
}


// Parse connection string from Render if needed
if (!string.IsNullOrEmpty(connectionString) && connectionString.StartsWith("postgres://"))
{
    var uri = new Uri(connectionString);
    var dbUser = uri.UserInfo.Split(':')[0];
    var dbPass = uri.UserInfo.Split(':')[1];
    var host = uri.Host;
    var port = uri.Port;
    var dbName = uri.AbsolutePath.Trim('/');

    connectionString = $"Host={host};Port={port};Database={dbName};Username={dbUser};Password={dbPass};SSL Mode=Require;Trust Server Certificate=true;";
}

// Register database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, opt =>
        opt.MigrationsAssembly("SIAMS")));

// Load Email Configuration
var emailConfig = builder.Configuration.GetSection("Email").Get<EmailConfig>();

// Register email-related services
builder.Services.AddSingleton(emailConfig);
builder.Services.AddScoped<IEmailService, EmailService>();



// Register authentication services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

// Add MVC services
builder.Services.AddControllersWithViews();

// Enable logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Build the app
var app = builder.Build();

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate(); // Ensure DB schema is up-to-date
        DbInitializer.Seed(context); // Seed default data
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database migration or seeding failed: {ex.Message}");
    }
}

// Configure HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
