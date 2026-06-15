using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ClinicManager.Data;
using ClinicManager.Mappers;
using ClinicManager.Models;
using ClinicManager.Models.Configuration;
using ClinicManager.Services;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Web;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

DockerDbManager.EnsureSqlServerContainerRunning();

var builder = WebApplication.CreateBuilder(args);

// Configure NLog programmatically
var nlogConfig = new LoggingConfiguration();
var logfile = new FileTarget("errorsLog")
{
    FileName = "/logs/errors.log",
    Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}${onexception:inner=${newline}${exception:format=tostring}}",
    CreateDirs = true
};
nlogConfig.AddRule(NLog.LogLevel.Error, NLog.LogLevel.Fatal, logfile);

var logconsole = new ConsoleTarget("logconsole")
{
    Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}${onexception:inner=${newline}${exception:format=tostring}}"
};
nlogConfig.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logconsole);

LogManager.Configuration = nlogConfig;

builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure());

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
        options.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
    }
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<PatientMapper>();
builder.Services.AddSingleton<MedicalRecordMapper>();
builder.Services.AddSingleton<VisitMapper>();
builder.Services.AddSingleton<VisitProcedureMedicationMapper>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IMedicalRecordAccessLogger, MedicalRecordAccessLogger>();
builder.Services.AddScoped<IMedicalRecordService, MedicalRecordService>();
builder.Services.AddScoped<IVisitService, VisitService>();
builder.Services.AddScoped<IVisitProcedureMedicationService, VisitProcedureMedicationService>();
builder.Services.AddScoped<IClinicalNoteService, ClinicalNoteService>();
builder.Services.AddScoped<IPdfReportService, PdfReportService>();
builder.Services.AddHostedService<UpcomingVisitsReportBackgroundService>();

builder.Services.AddControllersWithViews();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Unhandled exception (HTTP 500) occurred during request processing for path: {Path}", context.Request.Path);
        throw;
    }
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    await DataSeeder.SeedAsync(scope.ServiceProvider);

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in Roles.All)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    if (app.Environment.IsDevelopment())
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var seedUsers = app.Configuration.GetSection("SeedUsers").Get<List<SeedUserOptions>>() ?? new();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        foreach (var seed in seedUsers)
        {
            if (string.IsNullOrWhiteSpace(seed.Email) || string.IsNullOrWhiteSpace(seed.Password))
                continue;

            if (!Roles.All.Contains(seed.Role))
            {
                logger.LogWarning("Seed user {Email} has unknown role '{Role}', skipping.", seed.Email, seed.Role);
                continue;
            }

            if (await userManager.FindByEmailAsync(seed.Email) is not null)
                continue;

            var user = new ApplicationUser
            {
                UserName = seed.Email,
                Email = seed.Email,
                EmailConfirmed = true,
                FirstName = seed.FirstName,
                LastName = seed.LastName
            };

            var result = await userManager.CreateAsync(user, seed.Password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, seed.Role);
                logger.LogInformation("Seeded user {Email} with role {Role}.", seed.Email, seed.Role);
            }
            else
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                logger.LogError("Failed to seed user {Email}: {Errors}", seed.Email, errors);
            }
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
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
