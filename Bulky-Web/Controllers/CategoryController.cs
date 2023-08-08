using Bulky_Web.Data;
using Microsoft.AspNetCore.Mvc;

namespace Bulky_Web.Controllers
{
    public class CategoryController : Controller
    {
        public CategoryController(ApplicationDbContext)
        {
            
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
