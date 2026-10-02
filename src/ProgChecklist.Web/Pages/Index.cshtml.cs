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

    public void OnGet()
    {
        Sections = _catalog.GetSections();
    }
}
