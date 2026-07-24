using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using quizportal.Data;
using quizportal.Models;
using quizportal.Models.ViewModels;

namespace quizportal.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AccountController(ApplicationDbContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToHomeForRole();

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToHomeForRole();

        if (model.Role is not (AppRoles.StudentName or AppRoles.TeacherName))
            ModelState.AddModelError(nameof(model.Role), "Please select a valid role.");

        if (await _context.Users.AnyAsync(u => u.Username == model.Username.Trim()))
            ModelState.AddModelError(nameof(model.Username), "Username is already taken.");

        if (await _context.Users.AnyAsync(u => u.Email == model.Email.Trim()))
            ModelState.AddModelError(nameof(model.Email), "Email is already registered.");

        if (!ModelState.IsValid)
            return View(model);

        var user = new User
        {
            Username = model.Username.Trim(),
            Email = model.Email.Trim(),
            FullName = string.IsNullOrWhiteSpace(model.FullName) ? model.Username.Trim() : model.FullName.Trim(),
            UserRole = AppRoles.FromRoleName(model.Role),
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            PasswordHash = string.Empty
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Account Created. Please Log in.";
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToHomeForRole();

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToHomeForRole();

        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var username = model.Username.Trim();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            // Support older guest/plain passwords created before auth was added.
            if (!string.Equals(user.PasswordHash, model.Password, StringComparison.Ordinal))
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }
        }

        await SignInAsync(user);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToHomeForRole(user.UserRole);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task SignInAsync(User user)
    {
        var roleName = AppRoles.ToRoleName(user.UserRole);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, roleName),
            new("FullName", user.FullName ?? user.Username)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });
    }

    private IActionResult RedirectToHomeForRole(int? userRole = null)
    {
        return RedirectToAction("Index", "Home");
    }
}
