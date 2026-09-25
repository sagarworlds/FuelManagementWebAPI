using System.ComponentModel.DataAnnotations;

namespace WebAPI.Model
{
    /// <summary>
    /// Body of <c>POST api/user/changepassword</c>.
    /// </summary>
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "CurrentPassword is required.")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "NewPassword is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "NewPassword must be 8 to 100 characters.")]
        public string NewPassword { get; set; }
    }
}
