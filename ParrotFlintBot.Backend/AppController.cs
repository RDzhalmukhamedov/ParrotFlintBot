using Microsoft.AspNetCore.Mvc;

namespace ParrotFlintBot.Backend;

public class AppController : Controller
{
    [HttpGet]
    public string Index() => "It's Back";
}
