using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProgChecklist.Core;

namespace ProgChecklist.Web.Pages;

public class TopicModel : PageModel
{
    private readonly ITopicCatalog _catalog;

    public TopicModel(ITopicCatalog catalog)
    {
        _catalog = catalog;
    }

    public TopicContext Context { get; private set; } = null!;

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
        return Page();
    }
}
