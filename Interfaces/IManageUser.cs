using System.Collections.Generic;
using System.Threading.Tasks;
using Itsomax.Module.Core.Extensions;
using Itsomax.Module.UserCore.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Itsomax.Module.UserCore.Interfaces
{
    public interface IManageUser
    {
        Task<SystemSucceededTask> EditRole(EditRoleViewModel model,string userName, params string[] subModulesAdd);
        IEnumerable<SelectListItem> GetUserRolesToSelectListItem(long userId);
        IEnumerable<SelectListItem> GetRoleModulesToSelectListItem(long roleId);
        IList<string> GetSubmodulesByRoleId(long id);
        //bool CreateUserAddDefaultClaim(long id);
        //void UpdateClaimValueForRole();
        Task<SystemSucceededTask> EditUserAsync(EditUserViewModel model, string userName, params string[] rolesAdd);
        Task<SystemSucceededTask> CreateUserAsync(CreateUserViewModel model,string userName, params string[] selectedRoles);
        Task<SystemSucceededTask> UserLoginAsync(LoginUserViewModel model);
    }
}