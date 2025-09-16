namespace TheVinylCountdownLikes.ViewModels;

public class AlbumItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string? Genre { get; set; }
    public int? ReleaseYear { get; set; }

    public string Username { get; set; } = string.Empty;

    public int LikeCount { get; set; }
    public bool LikedByMe { get; set; }
}
