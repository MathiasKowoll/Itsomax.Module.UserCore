using System.Collections.Generic;
using System.Threading.Tasks;
using Itsomax.Module.Core.Extensions;
using Itsomax.Module.UserCore.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Itsomax.Module.UserCore.Interfaces
{
    public interface IManageUser
    {
        Task<SucceededTask> EditRole(EditRoleViewModel model, params string[] subModulesAdd);
        IEnumerable<SelectListItem> GetUserRolesToSelectListItem(long userId);
        IEnumerable<SelectListItem> GetRoleModulesToSelectListItem(long roleId);
        IList<string> GetSubmodulesByRoleId(long id);
        void AddDefaultClaimAllUsers();
        //bool CreateUserAddDefaultClaim(long id);
        //void UpdateClaimValueForRole();
        Task<SucceededTask> EditUserAsync(EditUserViewModel model, params string[] rolesAdd);
        Task<SucceededTask> CreateUserAsync(CreateUserViewModel model, params string[] selectedRoles);
        Task<SucceededTask> UserLoginAsync(LoginUserViewModel model);
    }
}