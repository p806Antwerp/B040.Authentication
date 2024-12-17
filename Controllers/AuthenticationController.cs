using B040.Authentication.Models;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.UI;
using Mg.Services;
using B040.Services;
using B040.Services.Models;
using b040;
using System.Web.Security;
using System.Security.Policy;
using System.Runtime.InteropServices.ComTypes;
using System.Web.Helpers;
using System.Security.Principal;
using Microsoft.VisualBasic.Logging;
using Serilog;
using System.Web;
using System.Data;
using Microsoft.VisualBasic.ApplicationServices;
using MySql.Data.MySqlClient;
using System.Data.SqlClient;
using static b040.Productielijst;

namespace B040.Authentication.Controllers
{

    [Authorize]
    [RoutePrefix("api/Authentication")]
    public class AuthenticationController : ApiController
    {
        //[AllowAnonymous]
        //[HttpPost]
        //[Route("Admin/CreateAdmin")]
        //public bool CreateAdmin(ApplicationUser applicationUser)
        //{
        //    var u = _context.Users.FirstOrDefault(x => x.UserName == applicationUser.UserName);
        //    addRole("Admin");
        //    addRole("Client");
        //    return true;
        //    void addRole(string roleName)
        //    {
        //        var roleId = _context.Roles.FirstOrDefault(x => x.Name == roleName).Id;
        //        var ur = new IdentityUserRole()
        //        {
        //            UserId = u.Id,
        //            RoleId = roleId
        //        };
        //        if (u.Roles.Any(x => x.RoleId == roleId) == false)
        //        {
        //            u.Roles.Add(ur);
        //            _context.SaveChanges();
        //        }
        //    }
        //}
        //[AllowAnonymous]
        //[HttpPost]
        //[Route("Admin/CreateClients")]
        //public async Task<bool> CreateClients()
        //{
        //    var b040 = DataAccessB040.GetInstance();
        //    ApiHelper apiHelper = new ApiHelper();
        //    AccountController accountController = new AccountController();
        //    List<ClientWithEmailModel> clientsWithEmail = b040.GetClientsWithEmail();
        //    foreach (ClientWithEmailModel c in clientsWithEmail)
        //    {
        //        var au = new UserNamePasswordPairModel()
        //        {
        //            UserName = c.Kl_Email
        //        };
        //        var exists = ExistsUser(au);
        //        if (exists.Exists == false)
        //        {
        //            string zerofilledAccount = ("00000" + c.Kl_Nummer.Trim()).Right(5);
        //            string pwd = $"Pwd{zerofilledAccount}.";
        //            try
        //            {
        //                await apiHelper.CreateUserAsync(c.Kl_Email, pwd);
        //            }
        //            catch (Exception ex)
        //            {
        //                Serilog.Log.Warning($"{c.Kl_Email} was not created. {ex.Message}");
        //                continue;
        //            }
        //        }
        //        var u = _context.Users.FirstOrDefault(x => x.UserName == c.Kl_Email);
        //        addRole("Client");
        //        void addRole(string roleName)
        //        {
        //            var roleId = _context.Roles.FirstOrDefault(x => x.Name == roleName).Id;
        //            var ur = new IdentityUserRole()
        //            {
        //                UserId = u.Id,
        //                RoleId = roleId
        //            };
        //            if (u.Roles.Any(x => x.RoleId == roleId) == false)
        //            {
        //                u.Roles.Add(ur);
        //                _context.SaveChanges();
        //            }
        //        }
        //    }
        //    return true;
        //}
        [AllowAnonymous]
        [HttpPost]
        [Route("Admin/CreateClient")]
        public async Task<OpResult> CreateClient(CreateClientModel cc)
        {
            string name = cc.Name;
            string pwd = cc.Password;
            OpResult or = new OpResult();
            ApiHelper apiHelper = new ApiHelper();
            try
            {
                var exists = ExistsUser(new UserNamePasswordPairModel { UserName = name, Password = pwd });
                if (exists.Exists == false)
                {
                    try
                    {
                        Task<IdentityResult> result = new PasswordValidator
                        {
                            RequiredLength = 8,        // Minimum password length
                            RequireNonLetterOrDigit = true,  // Require at least one non-alphanumeric character
                            RequireDigit = true,      // Require at least one numeric character
                            RequireLowercase = true,  // Require at least one lowercase letter
                            RequireUppercase = true   // Require at least one uppercase letter
                        }
                        .ValidateAsync(cc.Password);
                        if (result.Result.Succeeded == false)
                        {
                            throw new Exception("Invalid Password");
                        }
                        string passwordHash = new PasswordHasher().HashPassword(cc.Password);
                        string id = Guid.NewGuid().ToString();
                        string insertAspNetUser = @"
                            INSERT INTO AspNetUsers (Id, UserName, PasswordHash, SecurityStamp, Email, EmailConfirmed, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
                            VALUES (@iD, @UserName, @PasswordHash, '', UserName , 1, '', 0, 0, 0, 0)";
                        MySqlParameter[] parameters = {
                            new MySqlParameter("@Id", id),
                            new MySqlParameter("@UserName", cc.Name),
                            new MySqlParameter("@PasswordHash", passwordHash)
                        };
                        MariaDBHelper.ExecuteNonQuery(insertAspNetUser, parameters);
                    }
                    catch (Exception ex)
                    {
                        or.Message = $"{name} was not created. {ex.Message}";
                        or.Success = false;
                        Serilog.Log.Warning(or.Message);
                    }
                    if (or.Success == false) { return or; }
                }
                ApplicationUser u = new ApplicationUser();
                string selectUserQuery = "SELECT * FROM AspNetUsers WHERE UserName = @UserName";
                MySqlParameter[] selectUserparameters = {
                    new MySqlParameter("@UserName", cc.Name)};
                u = MariaDBHelper.ExecuteQuery<ApplicationUser>(selectUserQuery, reader =>
                {
                    return new ApplicationUser
                    {
                        Id = reader["Id"].ToString(),
                    };
                }, selectUserparameters).FirstOrDefault();
                or.Message = u.Id;
                addRole("Client");
                void addRole(string roleName)
                {
                    string insertRole = @"
                            INSERT INTO AspNetUserRoles (UserId, RoleId)
                            VALUES (@id, (SELECT Id FROM AspNetRoles WHERE Name = @RoleName));                            
                            ";
                    MySqlParameter[] insertRoleParameters = {
                            new MySqlParameter("@Id", u.Id),
                            new MySqlParameter("@RoleName", roleName)
                        };
                    MariaDBHelper.ExecuteNonQuery(insertRole, insertRoleParameters);
                }
            }
            catch (Exception ex)
            {
                or.Message = $"Could not create client {name}, {ex.Message}";
                or.Success = false;
                return or;
            }
            return or;
        }
        // POST api/Authentication/UpdateUser
 
        //[AllowAnonymous]
        //[HttpPost]
        //[Route("Admin/CreateRoles")]
        // public void CreateRoles()
        //{
        //    process("Admin");
        //    process("Client");
        //    void process(string roleName)
        //    {
        //        if (_context.Roles.Any(x => x.Name == roleName)) { return; }
        //        var r = new IdentityRole() { Name = roleName };
        //        _context.Roles.Add(r);
        //        _context.SaveChanges();
        //    }
        //}

        [AllowAnonymous]
        [HttpPost]
        [Route("Admin/ExistsUser")]
        public ExistsModel ExistsUser(UserNamePasswordPairModel model)
        {
            ApplicationUser u = null;
            string getUser = @"
                SELECT Id,PasswordHash, UserName FROM AspNetUsers
                WHERE UserName = @UserName";
            MySqlParameter[] parameters = {
                new MySqlParameter("@UserName", model.UserName) };
            u = MariaDBHelper.ExecuteQuery(getUser, reader =>
            {
                return new ApplicationUser
                {
                    Id = reader["Id"].ToString(),
                    PasswordHash = reader["PasswordHash"].ToString(),
                    UserName = reader["UserName"].ToString()
                };
            }, parameters).FirstOrDefault();
            var result = new ExistsModel() { Exists = u != null };
            return result;
        }
        [AllowAnonymous]
        [HttpGet]
        [Route("Admin/GetAllRoles")]
        public List<IdentityRole> GetAllRoles()
        {
            var roles = new List<IdentityRole>();
            try
            {
                string selectGetRoles = "select * from AspNetRoles";
                roles = MariaDBHelper.ExecuteQuery(selectGetRoles, reader =>
                {
                    return new IdentityRole
                    {
                        Id = reader["Id"].ToString(),
                        Name = reader["Name"].ToString(),
                    };
                });
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning($"Error: {ex.Message}");
                Serilog.Log.Warning(ex.StackTrace);
                throw;
            }
            return roles;
        }
        [AllowAnonymous]
        [HttpPost]
        [Route("Admin/GetRoles")]
        public UserDTO GetRoles(UserNamePasswordPairModel model)
        {
            var rv = new UserDTO();
            rv.Roles = new List<string>();
            if (model.UserName == null) { return rv; }
            if (model.Password == null) { return rv; }
            ApplicationUser u;
            Serilog.Log.Warning($"Current User: {WindowsIdentity.GetCurrent().Name}");
            try
            {
                string selectUserQuery = "SELECT * FROM AspNetUsers WHERE UserName = @UserName";
                MySqlParameter[] selectUsserParameters = {
                    new MySqlParameter("@UserName", model.UserName)};
                u = MariaDBHelper.ExecuteQuery<ApplicationUser>(selectUserQuery, reader =>
                {
                    return new ApplicationUser
                    {
                        Id = reader["Id"].ToString(),
                        UserName = reader["UserName"].ToString(),
                        PasswordHash = reader["PasswordHash"].ToString(),
                        // Add more properties if needed
                    };
                }, selectUsserParameters).FirstOrDefault();
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning($"GetRoles exception: {ex.Message}");

                throw ex;
            }
            if (u == null) { return rv; }
            // Verify the passowrd
            var hasher = new PasswordHasher();
            var result = hasher.VerifyHashedPassword(u.PasswordHash, model.Password);
            if (result != PasswordVerificationResult.Success) { return rv; }
            // return the role name(s)
            rv.WebAccountId = u.Id;
            rv.WebAccountName = model.UserName;
            rv.Roles = new List<string>();
            string selectRoles = @"
                    SELECT NAME FROM AspNetUserRoles,AspNetRoles              
	                    WHERE ROLEID = ID
                        AND USERID = @UserId";
            MySqlParameter[] parameters = {
                    new MySqlParameter("@UserId", u.Id)};
            rv.Roles = MariaDBHelper.ExecuteQuery<string>(selectRoles, reader =>
            {
                return reader["NAME"].ToString();
            }, parameters);
            return rv;
        }
        //[AllowAnonymous]
        //[HttpGet]
        //[Route("Admin/GetAllUsers")]
        //public List<UserWithRolesModel> GetAllUsers()
        //{
        //    var roles = _context.Roles.ToList();
        //    List<UserWithRolesModel> results = _context
        //        .Users.ToList()
        //        .Select(x => new UserWithRolesModel
        //        {
        //            Id = x.Id,
        //            UserName = x.UserName,
        //            Roles = x.Roles.Join(roles, u => u.RoleId, r => r.Id, (u, r) => r)
        //                .Select(y => new RoleModel() { Id = y.Id, Name = y.Name })
        //                .ToList()
        //        })
        //        .ToList();
        //    return results;
        //}
        //    [AllowAnonymous]
        //    [HttpPost]
        //    [Route("Admin/UpdateUser")]
        //    // Implemented in Account(Controller)
        //    // For some reason UserManager does not get initialized correctly here 
        //    // Some of the Password requirement get lost.
        //    public async Task<OpResult> UpdateUser(UpdateUserDTO updateUser)
        //    {
        //        OpResult or = new OpResult();
        //        if (updateUser == null)
        //        {
        //            or.Message = "Null Parameter in UpdateUser Endpoint";
        //            or.Success = false;
        //            return or;
        //        }
        //        _context = new ApplicationDbContext();
        //        UserManager<ApplicationUser> _userManager =
        //            new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(_context));

        //        var user = await _userManager.FindByIdAsync(updateUser.WebAccountId);
        //        if (user == null)
        //        {
        //            or.Message = "Invalid User Id in Update User Endpoint";
        //            or.Success = false;
        //            return or;
        //        }
        //        if (user.UserName.ToUpper() != updateUser.WebAccountName.ToUpper())
        //        {
        //            user.UserName = updateUser.WebAccountName;
        //        }
        //        var result = await _userManager
        //            .PasswordValidator.ValidateAsync(updateUser.Password);
        //        if ( result.Succeeded == false)
        //        {
        //            or.Message = result.Errors.FirstOrDefault();
        //            or.Success = false;
        //            return or;
        //        }
        //        var newPasswordHash = _userManager.PasswordHasher.HashPassword(updateUser.Password);

        //        user.PasswordHash= newPasswordHash;
        //        var updateResult = await _userManager.UpdateAsync(user);
        //        if (updateResult.Succeeded==false)
        //        {
        //            or.Message = updateResult.Errors.FirstOrDefault();
        //            or.Success = false;
        //            return or;
        //        }
        //        return or;
        //    }
    }
}