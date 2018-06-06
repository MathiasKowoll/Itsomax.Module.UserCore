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
using Itsomax.Module.Core.Data;
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
        private readonly ItsomaxDbContext _context;
        private readonly ILogginToDatabase _logger;
        private readonly SignInManager<User> _signIn;

        public ManageUser(UserManager<User> userManager, RoleManager<Role> roleManager,
                         IRepository<Role> role, IRepository<User> user, IRepository<SubModule> subModule,
                         ItsomaxDbContext context, ILogginToDatabase logger, SignInManager<User> signIn)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _user = user;
            _role = role;
            _subModule = subModule;
            _context = context;
            _logger = logger;
            _signIn = signIn;

        }

        public async Task<SystemSucceededTask> UserLoginAsync(LoginUserViewModel model)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(model.UserName);
                if (user == null)
                {
                    _logger.ErrorLog("User does not exists or tried to enter null value for user.", "Login User",
                        string.Empty, model.UserName);
                    return SystemSucceededTask.Failed("User does not exists or tried to enter null value for user.",
                        string.Empty, false, true);
                }
                if (user.IsDeleted)
                {
                    _logger.ErrorLog("User: " + user.UserName + " has been disabled", "Login User", string.Empty,
                        model.UserName);
                    return SystemSucceededTask.Failed("User: " + user.UserName + " has been disabled", "Login User",
                        false, true);
                }

                try
                {
                    var res = await _signIn.PasswordSignInAsync(user,model.Password,model.RememberMe,true);
                    if (res.Succeeded)
                    {
                        CreateUserAddDefaultClaimForUser(user);
                        UpdateClaimValueForRoleForUser(user);
                        _logger.InformationLog("User: " + user.UserName + " logged successfully", "Login User",
                            string.Empty, model.UserName);
                        return SystemSucceededTask.Success("");
                    }
                    if (res.IsLockedOut)
                    {
                        _logger.ErrorLog("User: " + user.UserName + " has been locked out", "Login User", string.Empty,
                            model.UserName);
                        return SystemSucceededTask.Failed("User " + user.UserName + " is lockout", "Login User", false,
                            true);
                    }

                    _logger.ErrorLog("User: " + user.UserName + " could not be logged in", "Login User", string.Empty,
                        model.UserName);
                    return SystemSucceededTask.Failed("User: " + user.UserName + " could not be logged in",
                        "Login User", false, true);
                }
                catch (Exception ex)
                {
                    _logger.ErrorLog(ex.Message,"Login User",ex.InnerException.Message,model.UserName);
                    return SystemSucceededTask.Failed("User: " + model.UserName + " could not be logged in",
                        "Login User", true, false);
                }
                
            }
            catch (Exception ex)
            {
                _logger.ErrorLog(ex.Message,"Login User",ex.InnerException.Message,model.UserName);
                return SystemSucceededTask.Failed("User: " + model.UserName + " could not be logged in", "Login User",
                    true, false);
            }
        }

        public async Task<SystemSucceededTask> EditRole(EditRoleViewModel model,string userName, 
            params string [] subModulesAdd)
        {
            try
            {
                var role = await _roleManager.FindByIdAsync(model.Id.ToString());
                role.Name = model.RoleName;

                try
                {
                    var res = await _roleManager.UpdateAsync(role);
                    if (res.Succeeded)
                    {
                        AddSubModulesToRole(role.Id,subModulesAdd);
                        _logger.InformationLog("Role: " + role.Name + " updated successfully", "Role Edit",
                            string.Empty, userName);
                        return SystemSucceededTask.Success("Role: "+role.Name+ " updated successfully");
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorLog(ex.Message,"Edit Role",ex.InnerException.Message,userName);
                    return SystemSucceededTask.Failed("Role: " + model.RoleName + " updated unsuccessfully",
                        ex.InnerException.Message, true, false);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorLog(ex.Message,"Edit Role",ex.InnerException.Message,userName);
                return SystemSucceededTask.Failed("Role: " + model.RoleName + " updated unsuccessfully",
                    ex.InnerException.Message, true, false);
            }
            _logger.ErrorLog("Unhandled error","Edit Role",string.Empty,userName);
            return SystemSucceededTask.Failed("Role: "+model.RoleName+ " updated successfully",string.Empty,true,false);
        }

        public async Task<SystemSucceededTask> CreateUserAsync(CreateUserViewModel model,string userName,
            params string[] selectedRoles)
        {
            var user = new User()
            {
                Email = model.Email,
                UserName = model.UserName,
                CreatedOn = DateTimeOffset.Now
            };
            var resCreateUser = await _userManager.CreateAsync(user, model.Password);
            if (resCreateUser.Succeeded)
            {
                var resAddRole = await _userManager.AddToRolesAsync(user, selectedRoles);
                if (resAddRole.Succeeded)
                {
                    CreateUserAddDefaultClaimForUser(user);
                    UpdateClaimValueForRoleForUser(user);
                    _logger.InformationLog("User " + model.UserName + " has been created succesfully", "Create user",
                        string.Empty, userName);
                    return SystemSucceededTask.Success("User: " +model.UserName+" created successfully");
                }

                await _userManager.DeleteAsync(user);
                _logger.ErrorLog("Error while creating user ", "Create user", string.Empty, userName);
                return SystemSucceededTask.Failed("Error while creating user " + model.UserName, string.Empty, false,
                    true);
            }
            _logger.ErrorLog("Error while creating user ", "Create user", string.Empty, userName);
            return SystemSucceededTask.Failed("Error while creating user " + model.UserName,string.Empty,false,true);
        }

        public async Task<SystemSucceededTask> EditUserAsync(EditUserViewModel model,string userName, 
            params string[] rolesAdd)
        {
            var user = await _userManager.FindByIdAsync(model.Id.ToString());
            if ((user == null) || (user.Id != model.Id))
            {
                _logger.ErrorLog("User: " + model.UserName + " not found","Edit User",string.Empty,userName);
                return SystemSucceededTask.Failed("User: " + model.UserName + " not found",string.Empty,false,true);
            }

            switch (user.UserName.ToUpper())
            {
                case "ADMIN" when model.IsDeleted:
                    _logger.ErrorLog("User Admin cannot be disabled","Edit User",string.Empty,userName);
                    return SystemSucceededTask.Failed("User Admin cannot be disabled",string.Empty,false,true);
                case "ADMIN" when model.IsLocked:
                    _logger.ErrorLog("User Admin cannot be disabled","Edit User",string.Empty,userName);
                    return SystemSucceededTask.Failed("User Admin cannot be locked",string.Empty,false,true);
                case "ADMIN" when !string.Equals(user.UserName, model.UserName, StringComparison.CurrentCultureIgnoreCase):
                    _logger.ErrorLog("Username for Admin cannot be changed","Edit User",string.Empty,userName);
                    return SystemSucceededTask.Failed("Username for Admin cannot be changed",string.Empty,false,true);
            }


            user.Email = model.Email;
            user.UserName = model.UserName;
            user.IsDeleted = model.IsDeleted;

            var res = await _userManager.UpdateAsync(user);
            if (res.Succeeded)
            {
                var rolesRemove = await  _userManager.GetRolesAsync(user);
                var resDel = await _userManager.RemoveFromRolesAsync(user, rolesRemove);
                if (resDel.Succeeded)
                {
                    var resAdd = await _userManager.AddToRolesAsync(user, rolesAdd);
                    if (resAdd.Succeeded)
                    {
                        var time = Convert.ToDateTime(model.IsLocked ? "3000-01-01" : "1970-01-01");
                        var resL = await _userManager.SetLockoutEndDateAsync(user, time);
                        if (resL.Succeeded)
                        {
                            CreateUserAddDefaultClaimForUser(user);
                            UpdateClaimValueForRoleForUser(user);
                            _logger.InformationLog("User " + model.UserName + " modified succesfully", "Create user",
                                string.Empty, userName);
                            return SystemSucceededTask.Success("User: "+user.UserName +" modified successfully");
                        }

                        _logger.ErrorLog("User " + model.UserName + " modified unsuccesfully", "Create user",
                            string.Empty, userName);
                        return SystemSucceededTask.Failed(
                            "Failed editing user " + model.UserName + ", could not set lockout for user",
                            string.Empty, false, true);
                    }

                    _logger.ErrorLog("User " + model.UserName + " modified unsuccesfully", "Create user", string.Empty,
                        userName);
                    return SystemSucceededTask.Failed(
                        "Failed editing user " + model.UserName + ", could not set lockout for user",
                        string.Empty, false, true);
                }

                _logger.ErrorLog("User " + model.UserName + " modified unsuccesfully", "Create user", string.Empty,
                    userName);
                return SystemSucceededTask.Failed(
                    "Failed editing user " + model.UserName + ", could not set lockout for user",
                    string.Empty, false, true);
            }

            _logger.ErrorLog("User " + model.UserName + " modified unsuccesfully", "Create user", string.Empty,
                userName);
            return SystemSucceededTask.Failed(
                "Failed editing user " + model.UserName + ", could not set lockout for user",
                string.Empty, false, true);


        }

        public void AddSubModulesToRole (long roleId,params string[] subModules)
        {
            var modRole = _context.Set<ModuleRole>().Where(x => x.RoleId == roleId); 
            foreach (var item in modRole)
            {
                var modrole = _context.Set<ModuleRole>()
                    .FirstOrDefault(x => x.RoleId == item.RoleId && x.SubModuleId == item.SubModuleId);
                if (modrole != null) _context.Set<ModuleRole>().Remove(modrole); 
            }
            _context.SaveChanges();

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
                    _context.Set<ModuleRole>().AddRange(modRole); 
                }
            }

            _context.SaveChanges();
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
                from mr in _context.Set<ModuleRole>()
                join sb in _subModule.Query() on mr.SubModuleId equals sb.Id
                where mr.RoleId == id
                select (sb.Name);

                return (subModRole.ToList());
            }
            catch(Exception ex)
            {
                _logger.ErrorLog(ex.Message, "GetSubmodulesByRoleId", ex.InnerException.Message);
                return null;
            }

        }

        public bool CreateUserAddDefaultClaimForUser(User user)
        {
            var claims = new List<Claim>();
            var claimsRemove = new List<Claim>();

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

        private void UpdateClaimValueForRoleForUser(User user)
        {
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