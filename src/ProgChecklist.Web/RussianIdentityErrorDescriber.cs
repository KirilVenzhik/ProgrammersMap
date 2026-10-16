using Microsoft.AspNetCore.Identity;

namespace ProgChecklist.Web;

/// <summary>Russian texts for Identity errors that can reach the user.</summary>
public class RussianIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => Make(nameof(DefaultError), "Произошла неизвестная ошибка. Попробуйте ещё раз.");

    public override IdentityError ConcurrencyFailure() => Make(nameof(ConcurrencyFailure), "Данные были изменены другим запросом. Попробуйте ещё раз.");

    public override IdentityError PasswordMismatch() => Make(nameof(PasswordMismatch), "Неверный пароль.");

    public override IdentityError InvalidToken() => Make(nameof(InvalidToken), "Недействительный токен.");

    public override IdentityError RecoveryCodeRedemptionFailed() => Make(nameof(RecoveryCodeRedemptionFailed), "Не удалось использовать код восстановления.");

    public override IdentityError LoginAlreadyAssociated() => Make(nameof(LoginAlreadyAssociated), "Этот способ входа уже привязан к другому аккаунту.");

    public override IdentityError InvalidUserName(string? userName) => Make(nameof(InvalidUserName), "Некорректный адрес электронной почты.");

    public override IdentityError InvalidEmail(string? email) => Make(nameof(InvalidEmail), "Некорректный адрес электронной почты.");

    public override IdentityError DuplicateUserName(string userName) => Make(nameof(DuplicateUserName), "Аккаунт с такой почтой уже существует.");

    public override IdentityError DuplicateEmail(string email) => Make(nameof(DuplicateEmail), "Аккаунт с такой почтой уже существует.");

    public override IdentityError InvalidRoleName(string? role) => Make(nameof(InvalidRoleName), "Некорректное имя роли.");

    public override IdentityError DuplicateRoleName(string role) => Make(nameof(DuplicateRoleName), "Такая роль уже существует.");

    public override IdentityError UserAlreadyHasPassword() => Make(nameof(UserAlreadyHasPassword), "Пароль уже задан.");

    public override IdentityError UserLockoutNotEnabled() => Make(nameof(UserLockoutNotEnabled), "Блокировка для этого пользователя не включена.");

    public override IdentityError UserAlreadyInRole(string role) => Make(nameof(UserAlreadyInRole), "Пользователь уже состоит в этой роли.");

    public override IdentityError UserNotInRole(string role) => Make(nameof(UserNotInRole), "Пользователь не состоит в этой роли.");

    public override IdentityError PasswordTooShort(int length) =>
        Make(nameof(PasswordTooShort), $"Пароль должен содержать не менее {length} символов.");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        Make(nameof(PasswordRequiresUniqueChars), $"Пароль должен содержать не менее {uniqueChars} различных символов.");

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        Make(nameof(PasswordRequiresNonAlphanumeric), "Пароль должен содержать хотя бы один специальный символ.");

    public override IdentityError PasswordRequiresDigit() => Make(nameof(PasswordRequiresDigit), "Пароль должен содержать хотя бы одну цифру.");

    public override IdentityError PasswordRequiresLower() => Make(nameof(PasswordRequiresLower), "Пароль должен содержать хотя бы одну строчную букву.");

    public override IdentityError PasswordRequiresUpper() => Make(nameof(PasswordRequiresUpper), "Пароль должен содержать хотя бы одну заглавную букву.");

    private static IdentityError Make(string code, string description) => new() { Code = code, Description = description };
}
