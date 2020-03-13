using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ComponentSpace.Saml2;
using Microsoft.AspNetCore.Identity;
using ComponentSpace.Saml2.Metadata.Export;
using Microsoft.AspNetCore.Authorization;
using ComponentSpace.Saml2.Assertions;
using System.Security.Claims;

namespace FakeIdentityProvider.Controllers
{
    [Route("[controller]/[action]")]
    public class SamlController : Controller
    {
        #region Member Variables
        private readonly IConfiguration _configuration;
        private readonly IConfigurationToMetadata _configurationToMetadata;
        private readonly ISamlIdentityProvider _samlIdentityProvider;
        private readonly SignInManager<IdentityUser> _signInManager;
        #endregion

        #region Constructor
        public SamlController(
            IConfiguration configuration, 
            IConfigurationToMetadata configurationToMetadata, 
            ISamlIdentityProvider samlIdentityProvider,
            SignInManager<IdentityUser> signInManager)
        {
            _configuration = configuration;
            _configurationToMetadata = configurationToMetadata;
            _samlIdentityProvider = samlIdentityProvider;
            _signInManager = signInManager;
        }
        #endregion

        [Authorize]
        public async Task<IActionResult> InitiateSingleSignOn()
        {
            var userName = User.Identity.Name;
            var partnerName = _configuration["PartnerName"];
            var relayState = _configuration["RelayState"];
            var attributes = new List<SamlAttribute>()
            {
                new SamlAttribute(ClaimTypes.Email, User.FindFirst(ClaimTypes.Email)?.Value),
                new SamlAttribute(ClaimTypes.GivenName, User.FindFirst(ClaimTypes.GivenName)?.Value),
                new SamlAttribute(ClaimTypes.Surname, User.FindFirst(ClaimTypes.Surname)?.Value),
            };

            //sending SAML response containing a SAML assertion
            await _samlIdentityProvider.InitiateSsoAsync(partnerName, userName, attributes, relayState);
            return new EmptyResult();
        }

        
       
        public async Task<IActionResult> SingleSignOnService()
        {
            // receive the request from SP (SP-initialed SSO)
            await _samlIdentityProvider.ReceiveSsoAsync();

            if (User.Identity.IsAuthenticated)
            {
                var userName = User.Identity.Name;
                var attributes = new List<SamlAttribute>()
                {
                    new SamlAttribute(ClaimTypes.Email, User.FindFirst(ClaimTypes.Email)?.Value),
                    new SamlAttribute(ClaimTypes.GivenName, User.FindFirst(ClaimTypes.GivenName)?.Value),
                    new SamlAttribute(ClaimTypes.Surname, User.FindFirst(ClaimTypes.Surname)?.Value)

                };

                // sent to SP
                await _samlIdentityProvider.SendSsoAsync(userName, attributes);

                return new EmptyResult();
            }
            else
            {
                
                return RedirectToAction("SingleSignOnServiceCompletion");
            }
        }

        [Authorize]
        public async Task<IActionResult> SingleSignOnServiceCompletion()
        {
            var userName = User.Identity.Name;
            var attributes = new List<SamlAttribute>()
                {
                    new SamlAttribute(ClaimTypes.Email, User.FindFirst(ClaimTypes.Email)?.Value),
                    new SamlAttribute(ClaimTypes.GivenName, User.FindFirst(ClaimTypes.GivenName)?.Value),
                    new SamlAttribute(ClaimTypes.Surname, User.FindFirst(ClaimTypes.Surname)?.Value)

                };

            //await _signInManager.PasswordSignInAsync(Input.UserName, Input.Password, Input.RememberMe, false);
           
            // sent to SP
            await _samlIdentityProvider.SendSsoAsync(userName, attributes);

            return new EmptyResult();
        }

        public async Task<IActionResult> InitiateSingleLogout(string returnUrl = "/Logout")
        {
            
            // request log out to SP
            await _samlIdentityProvider.InitiateSloAsync(relayState: returnUrl);

            return new EmptyResult();
        }


        public async Task<IActionResult> SingleLogoutService()
        {
            // receive response from SP
            var sloResult = await _samlIdentityProvider.ReceiveSloAsync();

            if (sloResult.IsResponse)
            {
                // idp-initiated SLO
                if (sloResult.HasCompleted)
                {
                    await _signInManager.SignOutAsync();
                    if (!string.IsNullOrEmpty(sloResult.RelayState))
                    {
                        return LocalRedirect(sloResult.RelayState);
                    }
                    return RedirectToPage("/Index");
                }
            }
            else
            {
                // sp-initiated SLO
                await _signInManager.SignOutAsync();

                // send response back to SP
                await _samlIdentityProvider.SendSloAsync();
            }
            return new EmptyResult();
        }
    }
}
