using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SIAMS.Data;

var builder = WebApplication.CreateBuilder(args);

// Configure PostgreSQL database connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                     ?? Environment.GetEnvironmentVariable("DATABASE_URL");

// Register the database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, opt =>
        opt.MigrationsAssembly("SIAMS"))); // Ensure migrations work

// Register authentication services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

// Add services to the container
builder.Services.AddControllersWithViews();

// Enable detailed logging
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
        context.Database.Migrate(); // Ensure the database is updated
        DbInitializer.Seed(context); // Seed initial data
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database migration or seeding failed: {ex.Message}");
    }
}

// Configure the HTTP request pipeline
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
