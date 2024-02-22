using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Microsoft.AspNetCore.Mvc;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace Bulky_Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            List<Company> objCompanyList = _unitOfWork.Company.GetAll().ToList();
            return View(objCompanyList);
        }

        public IActionResult Upsert(int? productId)
        {
            if (productId == null || productId == 0)
            {
                // Create
                return View(new Company());
            }
            else
            {
                // Update
                Company companyObj = _unitOfWork.Company.Get(u => u.Id == productId);
                if (companyObj == null)
                {
                    return NotFound();
                }
                return View(companyObj);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Company companyObj)
        {
            if (ModelState.IsValid)
            {
                if (companyObj.Id == 0)
                {
                    _unitOfWork.Company.Add(companyObj);
                }
                else
                {
                    _unitOfWork.Company.Update(companyObj);
                }

                _unitOfWork.Save();
                TempData["success"] = "Company Created/Updated Successfully";
                return RedirectToAction("Index");
            }
            else
            {
                return View(companyObj);
            }
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Company> objCompanyList = _unitOfWork.Company.GetAll().ToList();
            return Json(new { data = objCompanyList });
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var companyToBeDeleted = _unitOfWork.Company.Get(u=> u.Id==id);
            if (companyToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error While Deleting" });
            }

            _unitOfWork.Company.Remove(companyToBeDeleted);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Delete Successful" });
        }

        #endregion
    }
}
