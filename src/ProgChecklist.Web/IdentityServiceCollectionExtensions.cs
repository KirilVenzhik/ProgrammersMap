using Microsoft.AspNetCore.Identity;
using ProgChecklist.Data;

namespace ProgChecklist.Web;

public static class IdentityServiceCollectionExtensions
{
    public const string AuthCookieName = ".ProgChecklist.Auth";

    /// <summary>Cookie-based Identity without the default UI (ADR-13).</summary>
    public static IServiceCollection AddAppIdentity(this IServiceCollection services)
    {
        services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();

        services.AddIdentityCore<AppUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 1;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddErrorDescriber<RussianIdentityErrorDescriber>();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = AuthCookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true;
            options.LoginPath = "/account/login";
            options.LogoutPath = "/account/logout";
            options.AccessDeniedPath = "/account/login";

            var onRedirectToLogin = options.Events.OnRedirectToLogin;
            var onRedirectToAccessDenied = options.Events.OnRedirectToAccessDenied;
            options.Events.OnRedirectToLogin = context => IsApi(context.Request)
                ? StatusOnly(context.Response, StatusCodes.Status401Unauthorized)
                : onRedirectToLogin(context);
            options.Events.OnRedirectToAccessDenied = context => IsApi(context.Request)
                ? StatusOnly(context.Response, StatusCodes.Status403Forbidden)
                : onRedirectToAccessDenied(context);
        });

        return services;
    }

    private static bool IsApi(HttpRequest request) => request.Path.StartsWithSegments("/api");

    private static Task StatusOnly(HttpResponse response, int statusCode)
    {
        response.StatusCode = statusCode;
        return Task.CompletedTask;
    }
}
