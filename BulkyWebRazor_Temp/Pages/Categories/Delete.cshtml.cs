using BulkyWebRazor_Temp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BulkyWebRazor_Temp.Pages.Categories
{
    [BindProperties]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public Category Category { get; set; }
        public DeleteModel(ApplicationDbContext db)
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
                    _db.Categories.Remove(Category);
                    _db.SaveChanges();
                    TempData["success"] = "Category Deleted Successfully";
                    return RedirectToPage("Index");
                }
                return Page();

            }
        }
    }
}

