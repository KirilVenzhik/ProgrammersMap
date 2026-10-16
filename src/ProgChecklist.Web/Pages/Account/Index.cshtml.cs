using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProgChecklist.Core;
using ProgChecklist.Data;

namespace ProgChecklist.Web.Pages.Account;

[Authorize]
public class IndexModel : PageModel
{
    // Fixed month names (genitive): independent of the host's culture data (invariant globalization).
    private static readonly string[] MonthsGenitive =
    [
        "января", "февраля", "марта", "апреля", "мая", "июня",
        "июля", "августа", "сентября", "октября", "ноября", "декабря",
    ];

    private const string StatusKey = "AccountStatus";

    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IProgressService _progress;
    private readonly IdentityErrorDescriber _describer;

    public IndexModel(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IProgressService progress,
        IdentityErrorDescriber describer)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _progress = progress;
        _describer = describer;
    }

    // Bound manually per handler (TryUpdateModelAsync) so that the other form's empty fields are not validated.
    public ChangePasswordInput ChangePassword { get; set; } = new();

    public DeleteInput Delete { get; set; } = new();

    public string Email { get; private set; } = string.Empty;

    public string RegisteredOn { get; private set; } = string.Empty;

    public string StudiedText { get; private set; } = string.Empty;

    public string? StatusMessage { get; private set; }

    public List<string> ChangePasswordErrors { get; } = [];

    public List<string> DeleteErrors { get; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        StatusMessage = TempData[StatusKey] as string;
        return await LoadAsync() ?? Page();
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return await ForgetDeletedUserAsync();
        }

        var entries = await _progress.GetEntriesAsync(user.Id, HttpContext.RequestAborted);
        var export = new
        {
            email = user.Email,
            createdAt = Iso(user.CreatedAt),
            progress = entries.Select(e => new { topicKey = e.TopicKey, completedAt = Iso(e.CompletedAt) }).ToList(),
        };
        Response.Headers.CacheControl = "no-store";
        return File(JsonSerializer.SerializeToUtf8Bytes(export, JsonSerializerOptions.Web), "application/json; charset=utf-8", "progchecklist-data.json");
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        if (!await TryUpdateModelAsync(ChangePassword, nameof(ChangePassword)))
        {
            return await LoadAsync() ?? Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return await ForgetDeletedUserAsync();
        }

        var check = await _signInManager.CheckPasswordSignInAsync(user, ChangePassword.CurrentPassword, lockoutOnFailure: true);
        if (!check.Succeeded)
        {
            ChangePasswordErrors.Add(PasswordFailure(check));
            return await LoadAsync() ?? Page();
        }

        var result = await _userManager.ChangePasswordAsync(user, ChangePassword.CurrentPassword, ChangePassword.NewPassword);
        if (!result.Succeeded)
        {
            ChangePasswordErrors.AddRange(result.Errors.Select(e => e.Description).Distinct());
            return await LoadAsync() ?? Page();
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData[StatusKey] = "Пароль изменён.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        if (!await TryUpdateModelAsync(Delete, nameof(Delete)))
        {
            return await LoadAsync() ?? Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return await ForgetDeletedUserAsync();
        }

        var check = await _signInManager.CheckPasswordSignInAsync(user, Delete.Password, lockoutOnFailure: true);
        if (!check.Succeeded)
        {
            DeleteErrors.Add(PasswordFailure(check));
            return await LoadAsync() ?? Page();
        }

        // Progress rows are removed by the cascade on the foreign key.
        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            DeleteErrors.AddRange(result.Errors.Select(e => e.Description).Distinct());
            return await LoadAsync() ?? Page();
        }

        await _signInManager.SignOutAsync();
        TempData["ClearLocalProgress"] = true;
        return Redirect("/");
    }

    private string PasswordFailure(Microsoft.AspNetCore.Identity.SignInResult result) => result.IsLockedOut
        ? "Слишком много неудачных попыток. Попробуйте снова через 15 минут."
        : _describer.PasswordMismatch().Description;

    // Returns a redirect when the account is gone (cookie outlived it); null when the page data is loaded.
    private async Task<IActionResult?> LoadAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return await ForgetDeletedUserAsync();
        }

        Email = user.Email ?? string.Empty;
        RegisteredOn = FormatDate(user.CreatedAt);
        var count = (await _progress.GetKeysAsync(user.Id, HttpContext.RequestAborted)).Count;
        StudiedText = $"{count} {RussianPlural.Choose(count, "тема", "темы", "тем")}";
        return null;
    }

    private async Task<IActionResult> ForgetDeletedUserAsync()
    {
        await _signInManager.SignOutAsync();
        return Redirect("/account/login");
    }

    private static string FormatDate(DateTimeOffset value)
    {
        var utc = value.UtcDateTime;
        return $"{utc.Day} {MonthsGenitive[utc.Month - 1]} {utc.Year.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string Iso(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

    public class ChangePasswordInput
    {
        [Required(ErrorMessage = "Введите текущий пароль.")]
        [StringLength(128, ErrorMessage = "Пароль слишком длинный.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите новый пароль.")]
        [MinLength(8, ErrorMessage = "Пароль должен содержать не менее 8 символов.")]
        [StringLength(128, ErrorMessage = "Пароль слишком длинный.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Повторите новый пароль.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Пароли не совпадают.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class DeleteInput
    {
        [Required(ErrorMessage = "Введите пароль для подтверждения.")]
        [StringLength(128, ErrorMessage = "Пароль слишком длинный.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
