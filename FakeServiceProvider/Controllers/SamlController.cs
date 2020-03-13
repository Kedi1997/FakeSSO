using ComponentSpace.Saml2;
using ComponentSpace.Saml2.Metadata.Export;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FakeServiceProvider.Controllers
{
    [Route("[controller]/[action]")]
    public class SamlController : Controller
    {

        #region Member Variables
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IConfigurationToMetadata _configurationToMetadata;
        private readonly ISamlServiceProvider _samlServiceProvider;
        private readonly SignInManager<IdentityUser> _signInManager;
        #endregion

        #region Constructor
        public SamlController(
            IConfiguration configuration,
            IConfigurationToMetadata configurationToMetadata,
            ISamlServiceProvider samlServiceProvider,
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager)
        {
            _configuration = configuration;
            _configurationToMetadata = configurationToMetadata;
            _samlServiceProvider = samlServiceProvider;
            _signInManager = signInManager;
            _userManager = userManager;

        }
        #endregion

        public async Task<IActionResult> AssertionConsumerService()
        {
            var ssoResult = await _samlServiceProvider.ReceiveSsoAsync();

            var user = await _userManager.FindByNameAsync(ssoResult.UserID);

            if (user == null)
            {
                user = new IdentityUser { UserName = ssoResult.UserID, Email = ssoResult.UserID };
                var result = await _userManager.CreateAsync(user);

                if (!result.Succeeded)
                {
                    throw new Exception($"The user {ssoResult.UserID} couldn't be created - {result}");
                }

            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            bool flag = _signInManager.IsSignedIn(User);

            if (!string.IsNullOrEmpty(ssoResult.RelayState))
            {
                return LocalRedirect(ssoResult.RelayState);

            }

            return LocalRedirect("/index");
        }

        public async Task<IActionResult> InitiateSingleSignOn(string returnUrl = null)
        {
            var partnerName = _configuration["PartnerName"];

            // request sent to IdP's SSO Service
            await _samlServiceProvider.InitiateSsoAsync(partnerName, returnUrl);

            return new EmptyResult();
        }

        public async Task<IActionResult> InitiatedSingleLogout(string returnUrl = "/Logout")
        {
            var partnerName = _configuration["PartnerName"];

            // send logout request to Idp's SLO service
            await _samlServiceProvider.InitiateSloAsync(relayState: returnUrl);
            return new EmptyResult();
        }

        public async Task<IActionResult> SingleLogoutService()
        {
            // SP receive logout request from IdP
            var sloResult = await _samlServiceProvider.ReceiveSloAsync();

            if (sloResult.IsResponse)
            {
                // SP-initated SLO
                //if (sloResult.HasCompleted)
                //{
                    await _signInManager.SignOutAsync();
                    if (!string.IsNullOrEmpty(sloResult.RelayState))
                    {
                        return LocalRedirect(sloResult.RelayState);
                    }
                    return RedirectToPage("/Index");
                //}
            }
            else
            {
                // idp-initiated SLO
                await _signInManager.SignOutAsync();

                // send back response to IdP's SLO service
                await _samlServiceProvider.SendSloAsync();
            }

            return new EmptyResult();

        }


    }
}
