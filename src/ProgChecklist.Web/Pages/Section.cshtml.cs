using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProgChecklist.Core;

namespace ProgChecklist.Web.Pages;

public class SectionModel : PageModel
{
    private readonly ITopicCatalog _catalog;

    public SectionModel(ITopicCatalog catalog)
    {
        _catalog = catalog;
    }

    public Section Section { get; private set; } = null!;

    public IActionResult OnGet(string sectionSlug)
    {
        var section = _catalog.FindSection(sectionSlug);
        if (section is null)
        {
            return NotFound();
        }

        if (!string.Equals(sectionSlug, section.Slug, StringComparison.Ordinal))
        {
            return RedirectPermanent($"/{section.Slug}");
        }

        Section = section;
        return Page();
    }
}
