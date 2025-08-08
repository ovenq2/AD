using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ADUserManagement.Data;
using ADUserManagement.Services;
using ADUserManagement.Services.Interfaces;
using ADUserManagement.Models.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configuration binding
builder.Services.Configure<ActiveDirectoryConfig>(
    builder.Configuration.GetSection("ActiveDirectoryConfig"));

builder.Services.Configure<SmtpConfig>(
    builder.Configuration.GetSection("SmtpConfig"));

builder.Services.Configure<ApplicationConfig>(
    builder.Configuration.GetSection("ApplicationConfig"));

// Configuration validation
builder.Services.AddOptions<ActiveDirectoryConfig>()
    .Bind(builder.Configuration.GetSection("ActiveDirectoryConfig"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<SmtpConfig>()
    .Bind(builder.Configuration.GetSection("SmtpConfig"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<ApplicationConfig>()
    .Bind(builder.Configuration.GetSection("ApplicationConfig"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ ADD MEMORY CACHE SERVICE
builder.Services.AddMemoryCache();

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "ADUserManagement.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// Service registrations
builder.Services.AddScoped<IActiveDirectoryService, ActiveDirectoryService>();
builder.Services.AddScoped<IRequestService, RequestService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPasswordGeneratorService, PasswordGeneratorService>();

// ✅ ADD GROUP MANAGEMENT SERVICE (missing from original)
builder.Services.AddScoped<IGroupManagementService, GroupManagementService>();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline.
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

// Log application startup with configuration info
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var adConfig = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ActiveDirectoryConfig>>();

logger.LogInformation("🚀 ADUserManagement application starting up");
logger.LogInformation("📍 Configured Domain: {Domain}", adConfig.Value.Domain);
logger.LogInformation("👥 Authorized Groups: {Groups}", string.Join(", ", adConfig.Value.AuthorizedGroups));
logger.LogInformation("🌍 Environment: {Environment}", app.Environment.EnvironmentName);

app.Run();