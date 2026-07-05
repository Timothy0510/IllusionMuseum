using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IllusionMuseum.Models;

namespace IllusionMuseum.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var museum = await _context.museuminfo.FirstOrDefaultAsync();
            return View(museum);
        }

        public IActionResult About()
        {
            return View();
        }
    }
}