using Microsoft.AspNetCore.Mvc.RazorPages;
using ProgChecklist.Core;

namespace ProgChecklist.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ITopicCatalog _catalog;

    public IndexModel(ITopicCatalog catalog)
    {
        _catalog = catalog;
    }

    public IReadOnlyList<Section> Sections { get; private set; } = [];

    public int TopicCount { get; private set; }

    public string Description { get; private set; } = "";

    public void OnGet()
    {
        Sections = _catalog.GetSections();
        TopicCount = _catalog.TopicCount;
        Description = SeoText.HomeDescription(_catalog);
    }
}
