using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SpritzBuddy.Models;

namespace SpritzBuddy.Data
{
    // ATENȚIE: Moștenim IdentityDbContext<ApplicationUser>, nu simplul DbContext!
    // Asta configurează automat tabelele pentru useri.
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Aici vom adăuga tabelele noastre (DbSet-uri) mai târziu.
        // Exemplu: public DbSet<Post> Posts { get; set; }
    }
}