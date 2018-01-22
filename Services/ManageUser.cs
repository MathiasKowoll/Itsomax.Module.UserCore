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
        private readonly SignInManager<User> _signIn;
        private readonly IRepository<User> _user;
        private readonly IRepository<Role> _role;
        private readonly IRepository<SubModule> _subModule;
        private readonly IRepository<ModuleRole> _moduleRole;
        private readonly ILogginToDatabase _logger;

        public ManageUser(UserManager<User> userManager, RoleManager<Role> roleManager, SignInManager<User> signIn,
                         IRepository<Role> role, IRepository<User> user, IRepository<SubModule> subModule,
                         IRepository<ModuleRole> moduleRole, ILogginToDatabase logger)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _signIn = signIn;
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

        public void AddSubModulesToRole (long RoleId,params string[] subModules)
        {
            var modRole = _moduleRole.Query().Where(x => x.RoleId == RoleId);
            foreach (var item in modRole)
            {
                var modrole = _moduleRole.Query().FirstOrDefault(x => x.RoleId == item.RoleId && x.SubModuleId == item.SubModuleId);
                _moduleRole.Remove(modrole);
            }
            _moduleRole.SaveChange();

            foreach (var item in subModules)
            {
                var mod = _subModule.Query().FirstOrDefault(x => x.Name.Contains(item));
                ModuleRole modrole = new ModuleRole
                {
                    RoleId = RoleId,
                    SubModuleId = mod.Id
                };
                _moduleRole.Add(modrole);
                
            }
            _moduleRole.SaveChange();
            UpdateClaimValueForRole();
        }



        public IEnumerable<SelectListItem> GetUserRolesToSelectListItem(int UserId)
        {
            var user = _userManager.FindByIdAsync(UserId.ToString()).Result;
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
            
        
        public IEnumerable<SelectListItem> GetRoleModulesToSelectListItem(long RoleId)
        {
            try
            {
                var role = _roleManager.FindByIdAsync(RoleId.ToString()).Result;
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
        

        public IList<string> GetSubmodulesByRoleId(long Id)
        {
            try
            {
                var subModRole =
                from mr in _moduleRole.Query().ToList()
                join sb in _subModule.Query().ToList() on mr.SubModuleId equals sb.Id
                where mr.RoleId == Id
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

        public bool CreateUserAddDefaultClaim(long Id)
        {
            var user = _userManager.FindByIdAsync(Id.ToString()).Result;

            var claims = new List<Claim>();
            var claimsRemove = new List<Claim>();

            //claims.Add(new Claim("", ""));
            var claimsList = _subModule.Query().Select(x => new
            {
                x.Name

            }).ToList();
            var claimExistDB = _userManager.GetClaimsAsync(user).Result;
            foreach (var item in claimsList)
            {
                var claimExistDBType = claimExistDB.FirstOrDefault(x => x.Type == item.Name);
                if (claimExistDBType == null)
                {
                    claims.Add(new Claim(item.Name, "NoAccess"));
                }

            }
            var res = _userManager.AddClaimsAsync(user, claims).Result;
            foreach (var item in claimExistDB)
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
                var rolesDB = _role.Query().Where(x => roles.Contains(x.Name)).ToList();
                var subModules = _subModule.Query().ToList();

                foreach (var subMod in subModules)
                {
                    var oldClaim = _userManager.GetClaimsAsync(user).Result.FirstOrDefault(x => x.Type == subMod.Name);
                    var newClaim = new Claim(subMod.Name, "NoAccess");
                    var res =_userManager.ReplaceClaimAsync(user, oldClaim, newClaim).Result;
                }
                
                foreach (var role in rolesDB)
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