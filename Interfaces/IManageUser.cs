using System.Collections.Generic;
using System.Threading.Tasks;
using Itsomax.Module.Core.Extensions;
using Itsomax.Module.Core.Models;
using Itsomax.Module.UserCore.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Itsomax.Module.UserCore.Interfaces
{
    public interface IManageUser
    {
        Task<SucceededTask> EditRole(EditRoleViewModel model, params string[] subModulesAdd);
        IEnumerable<SelectListItem> GetUserRolesToSelectListItem(int UserId);
        IEnumerable<SelectListItem> GetRoleModulesToSelectListItem(long RoleId);
        IList<string> GetSubmodulesByRoleId(long Id);
        void AddDefaultClaimAllUsers();
        bool CreateUserAddDefaultClaim(long Id);
        void UpdateClaimValueForRole();
    }
}