using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Itsomax.Module.UserCore.ViewModels
{
    public class EditUserViewModel
    {
        [Required]
        public long Id { get; set; }
        [Required]
        public string UserName { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public bool IsLocked { get; set; }
        public bool IsDeleted { get; set; }
        public IEnumerable<SelectListItem> RolesList { get; set; }
    }
}