using Microsoft.AspNetCore.Mvc;
using SpritzBuddy.Data; // <--- Asigură-te că ai acest using
using SpritzBuddy.Models;
using System.Diagnostics;

namespace SpritzBuddy.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context; // <--- 1. Adăugăm contextul bazei de date

        // 2. Injectăm contextul în constructor
        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // --- TEST PENTRU BAZA DE DATE ---
        [HttpGet]
        public IActionResult TestDatabase()
        {
            try
            {
                // Încercăm să vedem dacă ne putem conecta

                // Încercăm să numărăm utilizatorii (chiar dacă e 0, înseamnă că tabelul există)
                int userCount = _context.Users.Count();

                string dbName = _context.Database.ProviderName;

                return Content($"SUCCES! ✅\n" +
                               $"M-am conectat la baza de date!\n" +
                               $"Provider: {dbName}\n" +
                               $"Număr utilizatori găsiți în tabelă: {userCount}");
            }
            catch (Exception ex)
            {
                // Dacă crapă ceva, afișăm eroarea pe ecran
                return Content($"EROARE CRITICĂ ❌\n\nMesaj: {ex.Message}\n\nDetalii: {ex.InnerException?.Message}");
            }
        }
        // -------------------------------

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}