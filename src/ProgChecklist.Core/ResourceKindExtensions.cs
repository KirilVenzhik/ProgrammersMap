namespace ProgChecklist.Core;

/// <summary>Presentation helpers for <see cref="ResourceKind"/>.</summary>
public static class ResourceKindExtensions
{
    /// <summary>Russian label of the resource kind.</summary>
    public static string ToDisplayName(this ResourceKind kind) => kind switch
    {
        ResourceKind.Docs => "Документация",
        ResourceKind.Article => "Статья",
        ResourceKind.Video => "Видео",
        ResourceKind.Course => "Курс",
        ResourceKind.Book => "Книга",
        ResourceKind.Practice => "Практика",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}
