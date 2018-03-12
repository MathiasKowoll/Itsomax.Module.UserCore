using Itsomax.Module.UserCore.Interfaces;
using Itsomax.Module.Core.Extensions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Itsomax.Module.Core.Models;
using System;
using Itsomax.Module.UserCore.ViewModels;
using Itsomax.Data.Infrastructure.Data;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using Itsomax.Module.Core.Interfaces;

namespace Itsomax.Module.UserCore.Services
{
    public class ManageUser : IManageUser
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IRepository<User> _user;
        private readonly IRepository<Role> _role;
        private readonly IRepository<SubModule> _subModule;
        private readonly IRepository<ModuleRole> _moduleRole;
        private readonly ILogginToDatabase _logger;

        public ManageUser(UserManager<User> userManager, RoleManager<Role> roleManager,
                         IRepository<Role> role, IRepository<User> user, IRepository<SubModule> subModule,
                         IRepository<ModuleRole> moduleRole, ILogginToDatabase logger)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _user = user;
            _role = role;
            _subModule = subModule;
            _moduleRole = moduleRole;
            _logger = logger;

        }


        public async Task<SucceededTask> EditRole(EditRoleViewModel model, params string [] subModulesAdd)
        {
            var role = _roleManager.FindByIdAsync(model.Id.ToString()).Result;
            role.Name = model.RoleName;

            var res = await _roleManager.UpdateAsync(role);
            if (res.Succeeded)
            {
                AddSubModulesToRole(role.Id,subModulesAdd);
                return SucceededTask.Success;
            }
            else
                return SucceededTask.Failed("FailedUpdateRole");

        }

        public void AddSubModulesToRole (long roleId,params string[] subModules)
        {
            var modRole = _moduleRole.Query().Where(x => x.RoleId == roleId);
            foreach (var item in modRole)
            {
                var modrole = _moduleRole.Query().FirstOrDefault(x => x.RoleId == item.RoleId && x.SubModuleId == item.SubModuleId);
                _moduleRole.Remove(modrole);
            }
            _moduleRole.SaveChange();

            foreach (var item in subModules)
            {
                var mod = _subModule.Query().FirstOrDefault(x => x.Name.Contains(item));
                if (mod != null)
                {
                    ModuleRole modrole = new ModuleRole
                    {
                        RoleId = roleId,
                        SubModuleId = mod.Id
                    };
                    _moduleRole.Add(modrole);
                }
            }
            _moduleRole.SaveChange();
            UpdateClaimValueForRole();
        }



        public IEnumerable<SelectListItem> GetUserRolesToSelectListItem(long userId)
        {
            var user = _userManager.FindByIdAsync(userId.ToString()).Result;
            if (user == null)
            {
                return null;
            }
            var userRoles = _userManager.GetRolesAsync(user).Result;
            if (userRoles == null)
            {
                return null;
            }
            try
            {
                var roles = _roleManager.Roles.ToList().Select(x => new SelectListItem()
                {
                    Selected = userRoles.Contains(x.Name),
                    Text = x.Name,
                    Value = x.Name
                });

                return roles;
            }
            catch (Exception ex)
            {
                _logger.ErrorLog(ex.Message, "GetUserRolesToSelectListItem", ex.InnerException.Message);
                return null;
            }
        }
            
        
        public IEnumerable<SelectListItem> GetRoleModulesToSelectListItem(long roleId)
        {
            try
            {
                var role = _roleManager.FindByIdAsync(roleId.ToString()).Result;
                if(role == null)
                {
                    return null;
                }
                var subModuleRole = GetSubmodulesByRoleId(role.Id);
                if(subModuleRole == null)
                {
                    return null;
                }
                var subModule = _subModule.Query().ToList().Select(x => new SelectListItem
                {
                    Selected = subModuleRole.Contains(x.Name),
                    Text = x.Name,
                    Value = x.Name
                });
                return (subModule);
            }
            catch(Exception ex)
            {
                _logger.ErrorLog(ex.Message, "GetRoleModulesToSelectListItem", ex.InnerException.Message);
                return null;
            }
            

            
        }
        

        public IList<string> GetSubmodulesByRoleId(long id)
        {
            try
            {
                var subModRole =
                from mr in _moduleRole.Query().ToList()
                join sb in _subModule.Query().ToList() on mr.SubModuleId equals sb.Id
                where mr.RoleId == id
                select (sb.Name);

                return (subModRole.ToList());
            }
            catch(Exception ex)
            {
                _logger.ErrorLog(ex.Message, "GetSubmodulesByRoleId", ex.InnerException.Message);
                var subModule = new List<string> ();
                return null;
            }

            
        }

        public void AddDefaultClaimAllUsers()
        {
            var users = _user.Query().ToList();
            foreach (var item in users)
            {
                CreateUserAddDefaultClaim(item.Id);
            }
        }

        public bool CreateUserAddDefaultClaim(long id)
        {
            var user = _userManager.FindByIdAsync(id.ToString()).Result;

            var claims = new List<Claim>();
            var claimsRemove = new List<Claim>();

            //claims.Add(new Claim("", ""));
            var claimsList = _subModule.Query().Select(x => new
            {
                x.Name

            }).ToList();
            var claimExistDb = _userManager.GetClaimsAsync(user).Result;
            foreach (var item in claimsList)
            {
                var claimExistDbType = claimExistDb.FirstOrDefault(x => x.Type == item.Name);
                if (claimExistDbType == null)
                {
                    claims.Add(new Claim(item.Name, "NoAccess"));
                }

            }
            var res = _userManager.AddClaimsAsync(user, claims).Result;
            foreach (var item in claimExistDb)
            {
                var claimExistsDll = claimsList.FirstOrDefault(x => x.Name == item.Type);
                if (claimExistsDll == null)
                {
                    claims.Remove(new Claim(item.Type, item.Value));
                }

            }
            var resRem = _userManager.RemoveClaimsAsync(user, claimsRemove).Result;
            return true;
        }

        public void UpdateClaimValueForRole()
        {
            var users = _user.Query().ToList();
            foreach (var itemUser in users)
            {
                var user = _userManager.FindByIdAsync(itemUser.Id.ToString()).Result;
                var roles = _userManager.GetRolesAsync(user).Result;
                var rolesDb = _role.Query().Where(x => roles.Contains(x.Name)).ToList();
                var subModules = _subModule.Query().ToList();

                foreach (var subMod in subModules)
                {
                    var oldClaim = _userManager.GetClaimsAsync(user).Result.FirstOrDefault(x => x.Type == subMod.Name);
                    var newClaim = new Claim(subMod.Name, "NoAccess");
                    var res =_userManager.ReplaceClaimAsync(user, oldClaim, newClaim).Result;
                }
                
                foreach (var role in rolesDb)
                {
                    var subModulesUser = GetSubmodulesByRoleId(role.Id);
                    foreach (var item in subModulesUser)
                    {
                        var oldClaim = _userManager.GetClaimsAsync(user).Result.FirstOrDefault(x => x.Type == item);
                        var newClaim = new Claim(item, "HasAccess");
                        var res = _userManager.ReplaceClaimAsync(user, oldClaim, newClaim).Result;
                    }
                }
            }

        }
    }
}