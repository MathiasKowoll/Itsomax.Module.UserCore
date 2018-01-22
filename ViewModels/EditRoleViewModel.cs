using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Itsomax.Module.UserCore.ViewModels
{
    public class EditRoleViewModel
    {
        public long Id { get; set; }
        public string RoleName { get; set; }
        public IEnumerable<SelectListItem> ModuleList { get; set; }
    }
}