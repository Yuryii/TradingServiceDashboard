using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Dashboard.Data;
using Dashboard.Models;
using Dashboard.Services;
using Dashboard.Services.Interfaces;
using Dashboard.Hubs;
using Dashboard.Jobs;
using QuestPDF;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configure QuestPDF license (Community license - free for commercial use)
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

// Configure DbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure());
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// Add Memory Cache
builder.Services.AddMemoryCache();

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;

    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Add authorization policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("ExecutivePolicy", policy =>
        policy.RequireRole(SD.Role_Executive, SD.Role_Sales, SD.Role_Marketing,
            SD.Role_Inventory, SD.Role_Finance, SD.Role_HumanResources, SD.Role_CustomerService))
    .AddPolicy("SalesPolicy", policy =>
        policy.RequireRole(SD.Role_Sales, SD.Role_Executive))
    .AddPolicy("MarketingPolicy", policy =>
        policy.RequireRole(SD.Role_Marketing, SD.Role_Executive))
    .AddPolicy("InventoryPolicy", policy =>
        policy.RequireRole(SD.Role_Inventory, SD.Role_Executive))
    .AddPolicy("FinancePolicy", policy =>
        policy.RequireRole(SD.Role_Finance, SD.Role_Executive))
    .AddPolicy("HRPolicy", policy =>
        policy.RequireRole(SD.Role_HumanResources, SD.Role_Executive))
    .AddPolicy("CustomerServicePolicy", policy =>
        policy.RequireRole(SD.Role_CustomerService, SD.Role_Executive))
    .AddPolicy("SystemPolicy", policy =>
        policy.RequireRole(SD.Role_Executive));

builder.Services.ConfigureApplicationCookie(options => {
    options.LoginPath = "/Auth/LoginBasic";
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.SlidingExpiration = true;
});

// Configure SignalR (camelCase matches MVC JSON so clients can share property resolution)
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Configure Hangfire with SQL Server storage
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.FromSeconds(1),
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

// Register RoleSeeder
builder.Services.AddScoped<RoleSeeder>();

// Register DbSeeder
builder.Services.AddScoped<DbSeeder>();

// Register Dashboard Services
builder.Services.AddScoped<IExecutiveDashboardService, ExecutiveDashboardService>();
builder.Services.AddScoped<ISalesDashboardService, SalesDashboardService>();
builder.Services.AddScoped<IMarketingDashboardService, MarketingDashboardService>();
builder.Services.AddScoped<IInventoryDashboardService, InventoryDashboardService>();
builder.Services.AddScoped<IFinanceDashboardService, FinanceDashboardService>();
builder.Services.AddScoped<IHumanResourcesDashboardService, HumanResourcesDashboardService>();
builder.Services.AddScoped<ICustomerServiceDashboardService, CustomerServiceDashboardService>();

// Register CRUD Services
CrudServiceRegistry.RegisterAll(builder.Services);

// Register Excel CRUD Service
builder.Services.AddScoped<ExcelCrudService>();

// Register PDF Report Service
builder.Services.AddScoped<IPdfReportService, PdfReportService>();
builder.Services.AddScoped<GlobalSearchService>();

// Register Notification Services
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<INotificationConfigCache, NotificationConfigCache>();
builder.Services.AddSingleton<IJobSchedulerService, JobSchedulerService>();

// Register AI Chat Services
builder.Services.AddHttpClient("AIChat");
builder.Services.AddScoped<AIContextAggregator>();
builder.Services.AddScoped<IAIChatService, AIChatService>();

var app = builder.Build();

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

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();

        var roleSeeder = services.GetRequiredService<RoleSeeder>();
        await roleSeeder.SeedRolesAndUsersAsync();

        var dbSeeder = services.GetRequiredService<DbSeeder>();
        await dbSeeder.SeedAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// SignalR Hub
app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<AIChatHub>("/aiChatHub");

// Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
{
    DashboardTitle = "Dashboard Jobs"
});

// Start Hangfire recurring jobs
NotificationJobsRegistrar.RegisterJobs();

// Seed CronExpression for existing NotificationConfigs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var nullCron = db.NotificationConfigs.Where(c => c.CronExpression == null).ToList();
    if (nullCron.Any())
    {
        var defaults = NotificationConfigDefaults.CronDefaults;
        foreach (var cfg in nullCron)
        {
            cfg.CronExpression = defaults.GetValueOrDefault(cfg.NotificationCode, "*/5 * * * *");
        }
        db.SaveChanges();
    }
}

app.Run();
