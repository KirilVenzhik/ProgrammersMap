using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProgChecklist.Core;

namespace ProgChecklist.Web.Pages;

public class TopicModel : PageModel
{
    private readonly ITopicCatalog _catalog;
    private readonly ISiteUrls _siteUrls;
    private readonly ILessonStore _lessons;
    private readonly IResourceCatalog _resources;

    public TopicModel(ITopicCatalog catalog, ISiteUrls siteUrls, ILessonStore lessons, IResourceCatalog resources)
    {
        _catalog = catalog;
        _siteUrls = siteUrls;
        _lessons = lessons;
        _resources = resources;
    }

    public TopicContext Context { get; private set; } = null!;

    public Lesson? Lesson { get; private set; }

    public IReadOnlyList<Resource> Resources { get; private set; } = [];

    public string BreadcrumbJsonLd { get; private set; } = "";

    public IActionResult OnGet(string sectionSlug, string topicSlug)
    {
        var context = _catalog.FindTopicContext(sectionSlug, topicSlug);
        if (context is null)
        {
            return NotFound();
        }

        if (!string.Equals(sectionSlug, context.Section.Slug, StringComparison.Ordinal)
            || !string.Equals(topicSlug, context.Topic.Slug, StringComparison.Ordinal))
        {
            return RedirectPermanent($"/{context.Section.Slug}/{context.Topic.Slug}");
        }

        Context = context;
        Lesson = _lessons.FindLesson(context.Section.Slug, context.Topic.Slug);
        Resources = _resources.GetResources(context.Section.Slug, context.Topic.Slug);
        BreadcrumbJsonLd = Core.BreadcrumbJsonLd.Build(context, _siteUrls.Absolute);
        return Page();
    }
}
