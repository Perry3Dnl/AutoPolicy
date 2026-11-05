using Microsoft.AspNetCore.Mvc;

namespace AutoPolicy.TestApp.Controllers;

[ApiController]
[Route("mvc/probe")]
public sealed class ProbeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok("mvc");
}
