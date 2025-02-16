using Microsoft.AspNetCore.Mvc;

namespace ParrotFlintBot.App;

public class AppController : Controller
{
    [HttpGet]
    public string Index() => "It's RSS";
}
