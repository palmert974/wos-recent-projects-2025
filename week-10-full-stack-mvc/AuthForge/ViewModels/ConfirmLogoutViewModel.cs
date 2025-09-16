using System.ComponentModel.DataAnnotations;

namespace AuthForge.ViewModels;

// Optional ViewModel if you want to post back a specific user Id on logout
public class ConfirmLogoutViewModel
{
    [Required]
    public int Id { get; set; }
}

