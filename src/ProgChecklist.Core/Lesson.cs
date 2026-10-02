namespace ProgChecklist.Core;

/// <summary>A topic lesson rendered from Markdown to HTML once at load time.</summary>
/// <param name="SectionSlug">Canonical section slug.</param>
/// <param name="TopicSlug">Canonical topic slug.</param>
/// <param name="Html">Rendered HTML (raw HTML in the source is escaped).</param>
/// <param name="Summary">Plain text of the first paragraph; empty if the lesson has no paragraph.</param>
public sealed record Lesson(string SectionSlug, string TopicSlug, string Html, string Summary);
