using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProgChecklist.Web.Pages;

public class ErrorModel : PageModel
{
    public int Code { get; private set; }

    public bool IsNotFound => Code == StatusCodes.Status404NotFound;

    public void OnGet(int code)
    {
        Code = code is >= 400 and <= 599 ? code : StatusCodes.Status404NotFound;
        Response.StatusCode = Code;
    }
}
