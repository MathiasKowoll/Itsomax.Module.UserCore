using System.ComponentModel.DataAnnotations;

namespace Itsomax.Module.UserCore.ViewModels
{
    public class LoginUserViewModel
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [Required]
        public bool RememberMe { get; set; }
    }
}