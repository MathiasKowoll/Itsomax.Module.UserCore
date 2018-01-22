using System;
using System.ComponentModel.DataAnnotations;
namespace Itsomax.Module.UserCore.ViewModels
{
    public class ChangePasswordViewModel
    {
        public ChangePasswordViewModel()
        {
            
        }
        [Required]
		[DataType(DataType.Password)]
		public string CurrentPassword { get; set; }
		[Required]
        [DataType(DataType.Password)]
        [StringLength(20, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        public string NewPassword { get; set; }
        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; }

    }
}
