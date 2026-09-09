using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutoPolicy.TestApp.Pages;

[AllowAnonymous]
public sealed class StandardAnonymousModel : PageModel
{
}
