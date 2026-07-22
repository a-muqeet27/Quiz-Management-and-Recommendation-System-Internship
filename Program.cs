using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using quizportal.Data;
using quizportal.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("QuizDB")
    ));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await EnsureAdminUserAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();

static async Task EnsureAdminUserAsync(ApplicationDbContext context)
{
    const string adminUsername = "admin";
    const string adminPassword = "admin123";

    var hasher = new PasswordHasher<User>();
    var admin = await context.Users.FirstOrDefaultAsync(u => u.Username == adminUsername);

    if (admin == null)
    {
        admin = new User
        {
            Username = adminUsername,
            Email = "admin@quizportal.local",
            FullName = "Administrator",
            UserRole = AppRoles.Admin,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            PasswordHash = string.Empty
        };
        admin.PasswordHash = hasher.HashPassword(admin, adminPassword);
        context.Users.Add(admin);
        await context.SaveChangesAsync();
        return;
    }

    var needsUpdate = false;

    if (admin.UserRole != AppRoles.Admin)
    {
        admin.UserRole = AppRoles.Admin;
        needsUpdate = true;
    }

    if (!admin.IsActive)
    {
        admin.IsActive = true;
        needsUpdate = true;
    }

    var verify = hasher.VerifyHashedPassword(admin, admin.PasswordHash, adminPassword);
    if (verify == PasswordVerificationResult.Failed)
    {
        admin.PasswordHash = hasher.HashPassword(admin, adminPassword);
        needsUpdate = true;
    }

    if (needsUpdate)
        await context.SaveChangesAsync();
}
