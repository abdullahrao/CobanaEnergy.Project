using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Signup;
using Logic.ResponseModel.Helper;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace CobanaEnergy.Project.Controllers
{
    /// <summary>
    /// Handles User Creation, Change Password and Login functinality
    /// </summary>
    public class AccountController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController()
        {
            var context = new ApplicationDBContext();
            var store = new UserStore<ApplicationUser>(context);
            _userManager = new UserManager<ApplicationUser>(store);

            var roleStore = new RoleStore<IdentityRole>(context);
            _roleManager = new RoleManager<IdentityRole>(roleStore);

            _userManager.UserTokenProvider = new DataProtectorTokenProvider<ApplicationUser>(
             new Microsoft.Owin.Security.DataProtection.DpapiDataProtectionProvider("CobanaEnergy").Create("ASP.NET Identity"));
        }

        #region user_creation

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public async Task<ActionResult> Create()
        {
            try
            {
                ViewBag.Roles =await _roleManager.Roles.ToListAsync();
                return View();
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Failed to load roles: {ex.Message}";
                TempData["ToastType"] = "error";
                Logic.Logger.Log($"Sign up Page rendering failed!! {ex.Message}");
                return RedirectToAction("Index", "Login");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Controls")]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Create(RegisterUserDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.Username) || string.IsNullOrEmpty(dto.Password) || string.IsNullOrEmpty(dto.ConfirmPassword)
                    || dto.Roles.Count < 1)
                {
                    // return Json(new { success = false, message = "All Fields are Mandatory!" });
                    return JsonResponse.Fail("All Fields are Mandatory!");
                }
                if (dto.Password != dto.ConfirmPassword)
                {
                    return JsonResponse.Fail("Passwords & Confirm Password do not match.");
                    //return Json(new { success = false, message = "Passwords & Confirm Password do not match." });
                }

                foreach (var role in dto.Roles)
                {
                    var roleExists = await _roleManager.RoleExistsAsync(role);
                    if (!roleExists)
                    {
                        return JsonResponse.Fail($"Role '{role}' does not exist.");
                        // return Json(new { success = false, message = $"Role '{role}' does not exist." });
                    }
                }

                var usernamePattern = @"^[A-Za-z]{3,}$";
                if (!Regex.IsMatch(dto.Username, usernamePattern))
                {
                    return JsonResponse.Fail("Username must be at least 3 letters and contain only English alphabets.");
                }

                var user = new ApplicationUser { UserName = dto.Username, Email = $"{dto.Username}_{Guid.NewGuid()}@noemail.cobana" };

                var result = await _userManager.CreateAsync(user, dto.Password);

                if (result.Succeeded)
                {
                    foreach (var role in dto.Roles)
                    {
                        await _userManager.AddToRoleAsync(user.Id, role);
                    }
                    //  return Json(new { success = true });
                    //return JsonResponse.Ok(new { UserId = user.Id }, "User created successfully");
                    return JsonResponse.Ok(new { success = true }, "");
                }

                var errorMessages = string.Join(", ", result.Errors);
                //return Json(new { success = false, message = errorMessages });
                return JsonResponse.Fail(errorMessages);
            }
            catch (Exception ex)
            {
                Logic.Logger.Log($"User Creation Failed!! {ex.Message}");
                //return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
                return JsonResponse.Fail($"An error occurred: {ex.Message}");
            }
        }

        #endregion

        #region change_password

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public async Task<ActionResult> ChangePassword()
        {
            try
            {
                ViewBag.Users = await _userManager.Users.ToListAsync(); 
                return View();
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Failed to load users: {ex.Message}";
                TempData["ToastType"] = "error";
                return RedirectToAction("Create");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Controls")]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ChangePassword(string userId, string newPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(newPassword))
                {
                    return JsonResponse.Fail("All Fields are Mandatory!");
                }
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return JsonResponse.Fail("User not found.");

                var token = await _userManager.GeneratePasswordResetTokenAsync(user.Id);
                var result = await _userManager.ResetPasswordAsync(user.Id, token, newPassword);

                if (result.Succeeded)
                    return JsonResponse.Ok(null, "Password updated successfully!");

                var error = string.Join(", ", result.Errors);
                return JsonResponse.Fail(error);
            }
            catch (Exception ex)
            {
                Logic.Logger.Log($"Password change failed: {ex.Message}");
                return JsonResponse.Fail("Something went wrong while changing the password.");
            }
        }

        #endregion

        #region Login

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login()
        {
            if (User.Identity.IsAuthenticated)//// Update the View Method as well; FOR ANY NEW ROLES REDIRECTION
            {
                if (User.IsInRole("Pre-sales") || User.IsInRole("Controls"))
                    return RedirectToAction("Dashboard", "PreSales");
                else
                    return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Login(string username, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                    return JsonResponse.Fail("All fields are required.");

                var usernamePattern = @"^[A-Za-z]{3,}$";
                if (!Regex.IsMatch(username, usernamePattern))
                {
                    return JsonResponse.Fail("Username must be at least 3 letters and contain only English alphabets.");
                }

                var user = await _userManager.FindAsync(username, password);
                if (user == null || user.UserName != username)
                    return JsonResponse.Fail("Invalid username or password.");

                var identity = await _userManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
                var authManager = HttpContext.GetOwinContext().Authentication;
                authManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                authManager.SignIn(new Microsoft.Owin.Security.AuthenticationProperties { IsPersistent = false }, identity);

                // Fetch user roles
                var roles = await _userManager.GetRolesAsync(user.Id);

                // Default redirect URL //// Update the View Method as well;
                string redirectUrl = Url.Action("Login", "Account");

                if (roles.Contains("Pre-sales"))
                {
                    redirectUrl = Url.Action("Dashboard", "PreSales");
                }
                else if (roles.Contains("Controls"))
                {
                    redirectUrl = Url.Action("Dashboard", "PreSales");
                }
                else
                {
                    redirectUrl = Url.Action("Index", "Home");
                }


                //return JsonResponse.Ok(null, "Login successful!");
                return JsonResponse.Ok(new { redirectUrl });
            }
            catch (Exception ex)
            {
                Logic.Logger.Log($"Login failed for {username}: {ex.Message}");
                return JsonResponse.Fail("An error occurred while processing login.");
            }
        }

        #endregion

        #region logout

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Request.GetOwinContext().Authentication.SignOut();
            Session.Clear();

            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            Response.Cache.SetNoStore();

            return RedirectToAction("Login", "Account");
        }

        #endregion
    }
}