using BulkyWebRazor_Temp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BulkyWebRazor_Temp.Pages.Categories
{
    [BindProperties]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public Category Category { get; set; }
        public EditModel(ApplicationDbContext db)
        {
            _db = db;
        }
        public void OnGet(int? categoryId)
        {
            if (categoryId != null && categoryId != 0)
            {
                Category = _db.Categories.Find(categoryId); 
            }
        }

        public IActionResult OnPost()
        {
            
            {
                if (ModelState.IsValid)
                {
                    _db.Categories.Update(Category);
                    _db.SaveChanges();
                    TempData["success"] = "Category Updated Successfully";
                    return RedirectToPage("Index");
                }
                return Page();

            }
        }
    }
}
