using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParrotFlintBot.Domain;

/// <summary>
/// Table of values for different application settings
/// </summary>
[Table("AppSettings")]
public class AppSettings
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public DateTime DateOfLastCrawl { get; set; }
}