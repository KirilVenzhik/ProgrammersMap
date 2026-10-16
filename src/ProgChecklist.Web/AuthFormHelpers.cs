using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ProgChecklist.Web;

/// <summary>Accessibility attribute values for auth forms (null means "omit the attribute").</summary>
public static class AuthFormHelpers
{
    public static string? Invalid(ModelStateDictionary modelState, string key) =>
        HasErrors(modelState, key) ? "true" : null;

    public static string? DescribedBy(ModelStateDictionary modelState, string key, string errorId, string? hintId = null)
    {
        var ids = new List<string>();
        if (hintId is not null)
        {
            ids.Add(hintId);
        }

        if (HasErrors(modelState, key))
        {
            ids.Add(errorId);
        }

        return ids.Count == 0 ? null : string.Join(' ', ids);
    }

    private static bool HasErrors(ModelStateDictionary modelState, string key) =>
        modelState.TryGetValue(key, out var entry) && entry.Errors.Count > 0;
}
