using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FakeIdentityProvider.Entity
{
    public class UserDatabaseContext : IdentityDbContext
    {
        public UserDatabaseContext(DbContextOptions<UserDatabaseContext> options)
            : base(options)
        { 
        } 

    }
}
