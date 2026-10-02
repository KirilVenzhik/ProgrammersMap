namespace ProgChecklist.Core;

/// <summary>Generates meta descriptions for pages from catalog data.</summary>
public static class SeoText
{
    /// <summary>Maximum length of a meta description.</summary>
    public const int MaxDescriptionLength = 160;

    private const int GroupsInSectionDescription = 3;

    /// <summary>Description of the home page.</summary>
    public static string HomeDescription(ITopicCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var sections = catalog.GetSections().Count;
        var topics = catalog.TopicCount;
        return Truncate(
            $"Подробный чеклист тем программирования: {topics} {RussianPlural.Choose(topics, "тема", "темы", "тем")} в {sections} {RussianPlural.Choose(sections, "разделе", "разделах", "разделах")}, уровни Junior, Middle и Senior. Отмечайте изученное и следите за прогрессом.",
            MaxDescriptionLength);
    }

    /// <summary>Description of a section page.</summary>
    public static string SectionDescription(Section section)
    {
        ArgumentNullException.ThrowIfNull(section);
        var groups = string.Join(", ", section.Groups.Take(GroupsInSectionDescription).Select(group => group.Title));
        var count = section.TopicCount;
        return Truncate(
            $"{section.Title}: {count} {RussianPlural.Choose(count, "тема", "темы", "тем")} — {groups}. {LevelsSentence(section)}",
            MaxDescriptionLength);
    }

    /// <summary>Description of a topic page.</summary>
    public static string TopicDescription(TopicContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var first = $"{context.Topic.Title} — тема раздела «{context.Section.Title}» ({context.Group.Title}), уровень {context.Topic.Level.ToDisplayName()}.";
        const string suffix = " Отмечайте изученное на Карте программиста.";
        return first.Length + suffix.Length <= MaxDescriptionLength
            ? first + suffix
            : Truncate(first, MaxDescriptionLength);
    }

    /// <summary>
    /// Returns the text unchanged if it fits; otherwise cuts it at the last space before
    /// <paramref name="maxLength"/> - 3 (hard cut if there is none) and appends an ellipsis.
    /// </summary>
    public static string Truncate(string text, int maxLength)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length <= maxLength)
        {
            return text;
        }

        var limit = Math.Max(maxLength - 3, 0);
        var cut = limit > 0 ? text.LastIndexOf(' ', limit - 1) : -1;
        var head = cut > 0 ? text[..cut] : text[..limit];
        return head.TrimEnd() + "…";
    }

    private static string LevelsSentence(Section section)
    {
        var present = section.Groups
            .SelectMany(group => group.Topics)
            .Select(topic => topic.Level)
            .Distinct()
            .OrderBy(level => level)
            .Select(level => level.ToDisplayName())
            .ToList();

        return present.Count switch
        {
            0 => "Чеклист тем.",
            1 => $"Уровень {present[0]}.",
            _ => $"Уровни {string.Join(", ", present.Take(present.Count - 1))} и {present[^1]}.",
        };
    }
}
