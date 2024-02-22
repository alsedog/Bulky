using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using System.Web.WebPages.Html;
using Microsoft.AspNetCore.Mvc;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Bulky.DataAccess.Migrations;
using System.Collections.Generic;
using Product = Bulky.Models.Product;



[Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]

    public class ProductController : Controller
    {
        private readonly IUnitOfWork _UnitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _UnitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            List<Product> objProductList = _UnitOfWork.Product.GetAll(includeProperties: "Category").ToList();

            return View(objProductList);
        }
        public IActionResult Upsert(int? productId)
        {

            ProductVM productVM = new()
            {
                CategoryList = (IEnumerable<SelectListItem>)_UnitOfWork.Category.GetAll().ToList().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString(),
                }),

                Product = new Product()
            };
            if (productId == null || productId == 0)
            {
                //create
                return View(productVM);
            }
            else
            {
                //update
                productVM.Product = _UnitOfWork.Product.Get(u => u.Id == productId, includeProperties: "ProductImages");
                return View(productVM);
            }
        }
        [HttpPost]
        public IActionResult Upsert(ProductVM productVM, List<IFormFile> files)
        {

            if (ModelState.IsValid)
            {
                if (productVM.Product.Id == 0)
                {
                    _UnitOfWork.Product.Add(productVM.Product);
                }
                else
                {
                    _UnitOfWork.Product.Update(productVM.Product);
                }
                _UnitOfWork.Save();

                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (files != null)
                {
                    foreach (IFormFile file in files)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string productPath = @"images\products\product-" + productVM.Product.Id;
                        string finalPath = Path.Combine(wwwRootPath, productPath);

                        if (!Directory.Exists(finalPath))
                            Directory.CreateDirectory(finalPath);

                        using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                        {
                            file.CopyTo(fileStream);
                        }

                        
                        ProductImage productImage = new()
                        {
                            ImageUrl = @"\" + productPath + @"\" + fileName,
                            ProductId = productVM.Product.Id,
                        };

                        if (productVM.Product.ProductImages == null)
                            productVM.Product.ProductImages = new List<ProductImage>();

                        productVM.Product.ProductImages.Add(productImage);

                    }

                    _UnitOfWork.Product.Update(productVM.Product);
                    _UnitOfWork.Save();
                }

                TempData["success"] = "Product Created/updated Successfully";
                return RedirectToAction("Index");
            }

            else
            {
                productVM.CategoryList = _UnitOfWork.Category.GetAll().Select(u => new SelectListItem
                    {
                    Text = u.Name,
                    Value = u.Id.ToString()
                        
                    });
                return View(productVM);
            }
            
           
        }
    public IActionResult DeleteImage (int imageId)
    {
        var imageToBeDeleted = _UnitOfWork.ProductImage.Get(u => u.Id == imageId);
        int productId = imageToBeDeleted.ProductId;
        if (imageToBeDeleted != null)
        {
            if(!string.IsNullOrEmpty(imageToBeDeleted.ImageUrl))
            {
                var oldImagePath =
                Path.Combine(_webHostEnvironment.WebRootPath,
                imageToBeDeleted.ImageUrl.TrimStart('\\'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }
            _UnitOfWork.ProductImage.Remove(imageToBeDeleted);
            _UnitOfWork.Save();

            TempData["success"] = "Deleted Successfully";
        }
        return RedirectToAction(nameof(Upsert), new
        {
            productId = productId
        });
    }

    #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> objProductList = _UnitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = objProductList });
        }

        //Post
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var ProductToBeDeleted = _UnitOfWork.Product.Get(u => u.Id == id);
            if (ProductToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error While Deleting" });
            }
        

        
        string productPath = @"images\products\product-" +id;
        string finalPath = Path.Combine(_webHostEnvironment.WebRootPath, productPath);

        if (Directory.Exists(finalPath))
        {
            string[] filepaths = Directory.GetFiles(finalPath);
            foreach (string filepath in filepaths)
            {
                System.IO.File.Delete(filepath);
            }


            Directory.Delete(finalPath);
        }

        _UnitOfWork.Product.Remove(ProductToBeDeleted);
            _UnitOfWork.Save();

            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
    }


