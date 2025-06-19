using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using EHS_PORTAL.Areas.CLIP.Models;
using System.Collections.Generic;
using System.Data.Entity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.IO;
using System.Net;
using EHS_PORTAL.Areas.CLIP.Services;

namespace EHS_PORTAL.Areas.CLIP.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private EHS_PORTAL.ApplicationSignInManager _signInManager;
        private EHS_PORTAL.ApplicationUserManager _userManager;
        private ApplicationDbContext _db;

        public ManageController()
        {
            _db = new ApplicationDbContext();
        }

        public ManageController(EHS_PORTAL.ApplicationUserManager userManager, EHS_PORTAL.ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
            _db = new ApplicationDbContext();
        }

        public EHS_PORTAL.ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<EHS_PORTAL.ApplicationSignInManager>();
            }
            private set 
            { 
                _signInManager = value; 
            }
        }

        public EHS_PORTAL.ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<EHS_PORTAL.ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Manage/Index
        public async Task<ActionResult> Index(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
                : message == ManageMessageId.Error ? "An error has occurred."
                : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
                : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
                : message == ManageMessageId.ProfileUpdateSuccess ? "Your profile has been updated."
                : "";

            var userId = User.Identity.GetUserId();
            var user = await UserManager.FindByIdAsync(userId);
            
            if (user == null)
            {
                return HttpNotFound();
            }
            
            var db = new ApplicationDbContext();
            
            // Get user's plants
            var userPlants = db.UserPlants
                .Where(up => up.UserId == userId)
                .Include(up => up.Plant)
                .Select(up => up.Plant)
                .ToList();
                
            // Get user's competencies
            var userCompetencies = db.UserCompetencies
                .Where(uc => uc.UserId == userId)
                .Include(uc => uc.CompetencyModule)
                .ToList();
                
            // Get user's roles
            var userRoles = await UserManager.GetRolesAsync(userId);
            
            // Get all plants for selection
            var plants = db.Plants.ToList();
            var userPlantIds = userPlants.Select(p => p.Id).ToList();
            
            // Create the view model with extended user information
            var model = new IndexViewModel
            {
                HasPassword = HasPassword(),
                PhoneNumber = await UserManager.GetPhoneNumberAsync(userId),
                TwoFactor = await UserManager.GetTwoFactorEnabledAsync(userId),
                Logins = await UserManager.GetLoginsAsync(userId),
                BrowserRemembered = await AuthenticationManager.TwoFactorBrowserRememberedAsync(userId),
                
                // Add user profile information
                UserId = userId,
                UserName = user.UserName,
                Email = user.Email,
                EmpID = user.EmpID,
                Atom_CEP = user.Atom_CEP,
                DOE_CPD = user.DOE_CPD,
                Dosh_CEP = user.Dosh_CEP,
                UserRoles = userRoles,
                UserPlants = userPlants,
                UserCompetencies = userCompetencies,
                PlantsList = plants.Select(p => new SelectListItem
                {
                    Text = p.PlantName,
                    Value = p.Id.ToString(),
                    Selected = userPlantIds.Contains(p.Id)
                }),
                SelectedPlantIds = userPlantIds
            };
            
            return View(model);
        }
        
        // POST: /Manage/Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Index(IndexViewModel model, string section)
        {
            var userId = User.Identity.GetUserId();
            var user = await UserManager.FindByIdAsync(userId);
            
            if (user == null)
            {
                return HttpNotFound();
            }
            
            // For specific sections, we don't need to validate the entire model
            if (section == "points" || section == "plants")
            {
                // Skip full model validation for these sections
            }
            else if (!ModelState.IsValid)
            {
                // For other sections (like basic), validate the model
                return View(model);
            }
            
            // Handle different form sections
            switch (section)
            {
                case "basic":
                    // Update basic profile information
                    user.UserName = model.UserName;
                    user.Email = model.Email;
                    user.EmpID = model.EmpID;
                    user.PhoneNumber = model.PhoneNumber;
                    break;
                
                case "points":
                    // CEP and CPD points are only editable by admins
                    if (User.IsInRole("Admin"))
                    {
                        user.Atom_CEP = model.Atom_CEP;
                        user.DOE_CPD = model.DOE_CPD;
                        user.Dosh_CEP = model.Dosh_CEP;
                    }
                    break;
                
                case "plants":
                    // Update user plants (only if admin)
                    if (User.IsInRole("Admin") && model.SelectedPlantIds != null)
                    {
                        var db = new ApplicationDbContext();
                        
                        // Remove existing plant assignments
                        var existingPlants = db.UserPlants.Where(up => up.UserId == userId).ToList();
                        foreach (var plant in existingPlants)
                        {
                            db.UserPlants.Remove(plant);
                        }
                        
                        // Add new plant assignments
                        foreach (var plantId in model.SelectedPlantIds)
                        {
                            db.UserPlants.Add(new UserPlant
                            {
                                UserId = userId,
                                PlantId = plantId
                            });
                        }
                        
                        await db.SaveChangesAsync();
                    }
                    break;
                
                default:
                    // If no section specified, update all fields
                    user.UserName = model.UserName;
                    user.Email = model.Email;
                    user.EmpID = model.EmpID;
                    user.PhoneNumber = model.PhoneNumber;
                    
                    if (User.IsInRole("Admin"))
                    {
                        user.Atom_CEP = model.Atom_CEP;
                        user.DOE_CPD = model.DOE_CPD;
                        user.Dosh_CEP = model.Dosh_CEP;
                    }
                    break;
            }
            
            // Update the user in the database
            var result = await UserManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }
            
            return RedirectToAction("Index", new { Message = ManageMessageId.ProfileUpdateSuccess });
        }

        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            ManageMessageId? message;
            var result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("ManageLogins", new { Message = message });
        }

        //
        // GET: /Manage/AddPhoneNumber
        public ActionResult AddPhoneNumber()
        {
            return View();
        }

        //
        // POST: /Manage/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            // Generate the token and send it
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), model.Number);
            if (UserManager.SmsService != null)
            {
                var message = new IdentityMessage
                {
                    Destination = model.Number,
                    Body = "Your security code is: " + code
                };
                await UserManager.SmsService.SendAsync(message);
            }
            return RedirectToAction("VerifyPhoneNumber", new { PhoneNumber = model.Number });
        }

        //
        // POST: /Manage/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EnableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), true);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // POST: /Manage/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DisableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), false);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // GET: /Manage/VerifyPhoneNumber
        public async Task<ActionResult> VerifyPhoneNumber(string phoneNumber)
        {
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), phoneNumber);
            // Send an SMS through the SMS provider to verify the phone number
            return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        //
        // POST: /Manage/VerifyPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePhoneNumberAsync(User.Identity.GetUserId(), model.PhoneNumber, model.Code);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.AddPhoneSuccess });
            }
            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "Failed to verify phone");
            return View(model);
        }

        //
        // POST: /Manage/RemovePhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemovePhoneNumber()
        {
            var result = await UserManager.SetPhoneNumberAsync(User.Identity.GetUserId(), null);
            if (!result.Succeeded)
            {
                return RedirectToAction("Index", new { Message = ManageMessageId.Error });
            }
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", new { Message = ManageMessageId.RemovePhoneSuccess });
        }

        //
        // GET: /Manage/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                    
                    // Log password change activity
                    var logger = new ActivityLogger(_db, HttpContext);
                    logger.LogActivity(
                        action: "UPDATE",
                        description: "User changed password",
                        entityName: "User",
                        entityId: user.Id
                    );
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
            }
            AddErrors(result);
            return View(model);
        }

        //
        // GET: /Manage/SetPassword
        public ActionResult SetPassword()
        {
            return View();
        }

        //
        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
                if (result.Succeeded)
                {
                    var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                    if (user != null)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        
                        // Log set password activity
                        var logger = new ActivityLogger(_db, HttpContext);
                        logger.LogActivity(
                            action: "UPDATE",
                            description: "User set password",
                            entityName: "User",
                            entityId: user.Id
                        );
                    }
                    return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Manage/ManageLogins
        public async Task<ActionResult> ManageLogins(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user == null)
            {
                return View("Error");
            }
            var userLogins = await UserManager.GetLoginsAsync(User.Identity.GetUserId());
            var otherLogins = AuthenticationManager.GetExternalAuthenticationTypes().Where(auth => userLogins.All(ul => auth.AuthenticationType != ul.LoginProvider)).ToList();
            ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins
            });
        }

        //
        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new AccountController.ChallengeResult(provider, Url.Action("LinkLoginCallback", "Manage"), User.Identity.GetUserId());
        }

        //
        // GET: /Manage/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
            {
                return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
            }
            var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
            return result.Succeeded ? RedirectToAction("ManageLogins") : RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
        }

        // GET: Manage/Users
        [Authorize(Roles = "Admin")]
        public ActionResult Users()
        {
            var db = new ApplicationDbContext();
            var users = db.Users.ToList();
            return View(users);
        }

        // GET: Manage/EditUser/userId
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> EditUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            var db = new ApplicationDbContext();
            var user = await UserManager.FindByIdAsync(id);

            if (user == null)
            {
                return HttpNotFound();
            }

            // Get the user's role
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));
            var userRoles = await UserManager.GetRolesAsync(user.Id);
            var currentRole = userRoles.FirstOrDefault() ?? "";

            // Get all available roles
            var roles = roleManager.Roles.ToList();
            var roleItems = roles.Select(r => new SelectListItem
            {
                Text = r.Name,
                Value = r.Name,
                Selected = r.Name == currentRole
            });

            // Get all plants
            var plants = db.Plants.ToList();

            // Get the user's plants
            var userPlantIds = db.UserPlants
                .Where(up => up.UserId == user.Id)
                .Select(up => up.PlantId)
                .ToList();

            // Create the view model
            var model = new EditUserProfileViewModel
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                EmpID = user.EmpID,
                Atom_CEP = user.Atom_CEP,
                DOE_CPD = user.DOE_CPD,
                PhoneNumber = user.PhoneNumber,
                Role = currentRole,
                RolesList = roleItems,
                SelectedPlantIds = userPlantIds,
                PlantsList = plants.Select(p => new SelectListItem
                {
                    Text = p.PlantName,
                    Value = p.Id.ToString(),
                    Selected = userPlantIds.Contains(p.Id)
                }),
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LockoutEnabled = user.LockoutEnabled,
                AccessFailedCount = user.AccessFailedCount,
                Dosh_CEP = user.Dosh_CEP
            };

            return View(model);
        }

        // POST: Manage/EditUser/userId
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> EditUser(EditUserProfileViewModel model, string section)
        {
            if (!ModelState.IsValid)
            {
                // Reload the roles and plants lists if model is invalid
                var dbContext = new ApplicationDbContext();
                var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(dbContext));
                model.RolesList = roleManager.Roles.Select(r => new SelectListItem
                {
                    Text = r.Name,
                    Value = r.Name,
                    Selected = r.Name == model.Role
                });

                model.PlantsList = dbContext.Plants.Select(p => new SelectListItem
                {
                    Text = p.PlantName,
                    Value = p.Id.ToString(),
                    Selected = model.SelectedPlantIds != null && model.SelectedPlantIds.Contains(p.Id)
                });

                return View(model);
            }

            var user = await UserManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return HttpNotFound();
            }

            // Handle different form sections
            if (section == "points")
            {
                // Update only points
                user.Atom_CEP = model.Atom_CEP;
                user.DOE_CPD = model.DOE_CPD;
                user.Dosh_CEP = model.Dosh_CEP;
                
                // Update the user
                var result = await UserManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    AddErrors(result);
                    return View(model);
                }
                
                TempData["SuccessMessage"] = "Points updated successfully.";
                return RedirectToAction("EditUser", new { id = model.Id });
            }
            else 
            {
                // Update all user properties
                user.UserName = model.UserName;
                user.Email = model.Email;
                user.EmpID = model.EmpID;
                user.Atom_CEP = model.Atom_CEP;
                user.DOE_CPD = model.DOE_CPD;
                user.PhoneNumber = model.PhoneNumber;
                user.EmailConfirmed = model.EmailConfirmed;
                user.PhoneNumberConfirmed = model.PhoneNumberConfirmed;
                user.TwoFactorEnabled = model.TwoFactorEnabled;
                user.LockoutEnabled = model.LockoutEnabled;
                user.Dosh_CEP = model.Dosh_CEP;

                // Update the user
                var result = await UserManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    AddErrors(result);
                    return View(model);
                }

                // Update user role if changed
                var userRoles = await UserManager.GetRolesAsync(user.Id);
                if (userRoles.Count > 0 && userRoles[0] != model.Role)
                {
                    // Remove from current roles
                    await UserManager.RemoveFromRolesAsync(user.Id, userRoles.ToArray());
                    
                    // Add to new role
                    await UserManager.AddToRoleAsync(user.Id, model.Role);
                }
                else if (userRoles.Count == 0 && !string.IsNullOrEmpty(model.Role))
                {
                    // User has no roles but a role was selected
                    await UserManager.AddToRoleAsync(user.Id, model.Role);
                }

                // Update user plants
                var db = new ApplicationDbContext();
                
                // Remove existing plant assignments
                var existingPlants = db.UserPlants.Where(up => up.UserId == user.Id).ToList();
                foreach (var plant in existingPlants)
                {
                    db.UserPlants.Remove(plant);
                }
                
                // Add new plant assignments
                if (model.SelectedPlantIds != null && model.SelectedPlantIds.Count > 0)
                {
                    foreach (var plantId in model.SelectedPlantIds)
                    {
                        db.UserPlants.Add(new UserPlant
                        {
                            UserId = user.Id,
                            PlantId = plantId
                        });
                    }
                }
                
                await db.SaveChangesAsync();

                TempData["SuccessMessage"] = "User profile updated successfully.";
                return RedirectToAction("Users");
            }
        }

        // GET: Manage/UserDetails/userId
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UserDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            var db = new ApplicationDbContext();
            var user = await UserManager.FindByIdAsync(id);

            if (user == null)
            {
                return HttpNotFound();
            }

            // Get user's roles
            var userRoles = await UserManager.GetRolesAsync(user.Id);
            
            // Get user's plants
            var userPlants = db.UserPlants
                .Where(up => up.UserId == user.Id)
                .Include(up => up.Plant)
                .Select(up => up.Plant)
                .ToList();
                
            // Get user's competencies
            var userCompetencies = db.UserCompetencies
                .Where(uc => uc.UserId == user.Id)
                .Include(uc => uc.CompetencyModule)
                .ToList();
                
            ViewBag.UserRoles = userRoles;
            ViewBag.UserPlants = userPlants;
            ViewBag.UserCompetencies = userCompetencies;

            return View(user);
        }

        // GET: Manage/DeleteUser/userId
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            var db = new ApplicationDbContext();
            var user = await UserManager.FindByIdAsync(id);

            if (user == null)
            {
                return HttpNotFound();
            }

            // Don't allow deletion of current user
            if (user.Id == User.Identity.GetUserId())
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction("Users");
            }

            // Get user's roles
            var userRoles = await UserManager.GetRolesAsync(user.Id);
            
            // Get user's plants
            var userPlants = db.UserPlants
                .Where(up => up.UserId == user.Id)
                .Include(up => up.Plant)
                .Select(up => up.Plant)
                .ToList();
                
            // Get user's competencies
            var userCompetencies = db.UserCompetencies
                .Where(uc => uc.UserId == user.Id)
                .Include(uc => uc.CompetencyModule)
                .ToList();
                
            ViewBag.UserRoles = userRoles;
            ViewBag.UserPlants = userPlants;
            ViewBag.UserCompetencies = userCompetencies;

            return View(user);
        }

        // POST: Manage/DeleteUser/userId
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteUser(string id, FormCollection form)
        {
            if (string.IsNullOrEmpty(id))
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "User ID is required" }, JsonRequestBehavior.AllowGet);
                }
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "User not found" }, JsonRequestBehavior.AllowGet);
                }
                return HttpNotFound();
            }

            // Don't allow deletion of current user
            if (user.Id == User.Identity.GetUserId())
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "You cannot delete your own account" }, JsonRequestBehavior.AllowGet);
                }
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction("Users");
            }

            // Delete related records first
            var db = new ApplicationDbContext();
            
            // Remove user plants
            var userPlants = db.UserPlants.Where(up => up.UserId == id).ToList();
            foreach (var plant in userPlants)
            {
                db.UserPlants.Remove(plant);
            }
            
            // Remove user competencies
            var userCompetencies = db.UserCompetencies.Where(uc => uc.UserId == id).ToList();
            foreach (var competency in userCompetencies)
            {
                db.UserCompetencies.Remove(competency);
            }
            
            await db.SaveChangesAsync();

            // Delete the user
            var result = await UserManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true, message = "User deleted successfully" }, JsonRequestBehavior.AllowGet);
                }
                TempData["SuccessMessage"] = "User deleted successfully.";
            }
            else
            {
                var errorMessage = string.Join(", ", result.Errors);
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Failed to delete user: " + errorMessage }, JsonRequestBehavior.AllowGet);
                }
                AddErrors(result);
                TempData["ErrorMessage"] = "Failed to delete user.";
            }

            return RedirectToAction("Users");
        }

        // GET: Manage/ResetUserPassword/userId
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ResetUserPassword(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
            {
                return HttpNotFound();
            }

            ViewBag.User = user;
            return View(new SetPasswordViewModel());
        }

        // POST: Manage/ResetUserPassword/userId
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ResetUserPassword(string id, SetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
            {
                return HttpNotFound();
            }

            // Remove existing password
            await UserManager.RemovePasswordAsync(user.Id);
            
            // Add new password
            var result = await UserManager.AddPasswordAsync(user.Id, model.NewPassword);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Password has been reset successfully.";
                return RedirectToAction("UserDetails", new { id = id });
            }
            
            AddErrors(result);
            return View(model);
        }

        // GET: /Manage/EditProfile
        public async Task<ActionResult> EditProfile()
        {
            var userId = User.Identity.GetUserId();
            var user = await UserManager.FindByIdAsync(userId);
            
            if (user == null)
            {
                return HttpNotFound();
            }
            
            var model = new EditProfileViewModel
            {
                UserName = user.UserName,
                Email = user.Email,
                EmpID = user.EmpID,
                PhoneNumber = user.PhoneNumber
            };
            
            return View(model);
        }
        
        // POST: /Manage/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            
            var userId = User.Identity.GetUserId();
            var user = await UserManager.FindByIdAsync(userId);
            
            if (user == null)
            {
                return HttpNotFound();
            }
            
            // Update user information
            user.UserName = model.UserName;
            user.Email = model.Email;
            user.EmpID = model.EmpID;
            user.PhoneNumber = model.PhoneNumber;
            
            var result = await UserManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }
            
            return RedirectToAction("Index", new { Message = ManageMessageId.ProfileUpdateSuccess });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }
                
                if (_db != null)
                {
                    _db.Dispose();
                    _db = null;
                }
            }

            base.Dispose(disposing);
        }

#region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        private bool HasPhoneNumber()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PhoneNumber != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            ProfileUpdateSuccess,
            Error
        }

#endregion
    }
}