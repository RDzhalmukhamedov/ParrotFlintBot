namespace ParrotFlintBot.Backend;

public class CronConfiguration
{
    public static readonly string Configuration = "CronConfig";
    
    public string FullCrawlExpression { get; set; } = "0 12 */2 * *";

    public string LiteCrawlExpression { get; set; } = "0 */2 * * *";
}