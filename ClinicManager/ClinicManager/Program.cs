using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ClinicManager.Data;
using ClinicManager.Mappers;
using ClinicManager.Models;
using ClinicManager.Models.Configuration;
using ClinicManager.Services;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

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

builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

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
