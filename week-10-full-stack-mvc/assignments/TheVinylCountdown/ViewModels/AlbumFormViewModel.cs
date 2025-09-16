using System.ComponentModel.DataAnnotations;

namespace TheVinylCountdown.ViewModels;

public class AlbumFormViewModel
{
    [Required(ErrorMessage = "Title is required")]
    [MinLength(1, ErrorMessage = "Title must be at least 1 character")]
    [Display(Name = "Album Title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Artist is required")]
    [MinLength(1, ErrorMessage = "Artist must be at least 1 character")]
    [Display(Name = "Artist Name")]
    public string Artist { get; set; } = string.Empty;

    [Display(Name = "Release Year")]
    [Range(1900, 2100, ErrorMessage = "Please enter a valid year between 1900 and 2100")]
    public int? ReleaseYear { get; set; }

    [Display(Name = "Genre")]
    public string? Genre { get; set; }
}
