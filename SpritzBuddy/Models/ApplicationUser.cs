using Microsoft.AspNetCore.Identity;

namespace SpritzBuddy.Models
{
    // Moștenim IdentityUser pentru a avea deja funcționalități de login, parolă, email etc.
    public class ApplicationUser : IdentityUser
    {
        // Aici vom adăuga mai târziu: NumeComplet, Descriere, Poza, etc.
        // Momentan o lăsăm goală, dar moștenește tot ce trebuie.
    }
}