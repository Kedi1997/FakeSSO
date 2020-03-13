using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace FakeServiceProvider.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly SignInManager<IdentityUser> _signInManager;

        public IndexModel(ILogger<IndexModel> logger, SignInManager<IdentityUser> signInManager)
        {
            _logger = logger;
            _signInManager = signInManager;
        }

        public void OnGet()
        {
            bool flag = _signInManager.IsSignedIn(User);
           
        }
    }
}
