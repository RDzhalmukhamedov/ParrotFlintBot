using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParrotFlintBot.Domain;

/// <summary>
/// Table for storing info about user subscriptions
/// </summary>
[Table("Users")]
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public long ChatId { get; set; }

    public ICollection<Project> Projects { get; } = new List<Project>();
}
