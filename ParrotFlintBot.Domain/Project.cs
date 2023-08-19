using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ParrotFlintBot.Shared;

#pragma warning disable CS8618

namespace ParrotFlintBot.Domain;

/// <summary>
/// Table for storing info about crowdfunding projects and their updates
/// </summary>
[Table("Projects")]
public class Project
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MinLength(2), MaxLength(300)]
    public string Name { get; set; }

    [Required]
    [MinLength(2), MaxLength(300)]
    public string ProjectSlug { get; set; }

    [Required]
    [MinLength(2), MaxLength(100)]
    public string CreatorSlug { get; set; }

    [Required]
    public ProjectStatus Status { get; set; }

    public short UpdatesCount { get; set; }

    public short PrevUpdatesCount { get; set; }

    public long? LastUpdateId { get; set; }

    public string? LastUpdateTitle { get; set; }
    
    public string Site { get; set; }

    public ICollection<User> Users { get; } = new List<User>();
}