using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using FakeIdentityProvider.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;

namespace FakeIdentityProvider.Pages
{
    public class LoginModel : PageModel
    {

        private readonly ILogger<LoginModel> _logger;
        private readonly UserDatabaseContext _userDatabaseContext;
        private readonly SignInManager<IdentityUser> _signInManager;

        #region Constructor
        public LoginModel(ILogger<LoginModel> logger, UserDatabaseContext userDatabaseContext, 
            SignInManager<IdentityUser> signInManager)
        {
            _logger = logger;
            _userDatabaseContext = userDatabaseContext;
            _signInManager = signInManager;
        }
        #endregion

        #region Member Variables
        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }
        #endregion


        public class InputModel
        {
            [Required, Display(Name = "User Name"), EmailAddress]
            public string UserName { get; set; }

            [Required, DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember Me")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl = returnUrl ?? Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            if (ModelState.IsValid)
            {
                
                var result = await _signInManager.PasswordSignInAsync(Input.UserName, Input.Password, Input.RememberMe, false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in");
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attemp");
                return Page();
            }

            return Page();
        }
    
    }
}
