using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace Bulky_Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()

        {
           
            IEnumerable<Product> productList = _unitOfWork.Product.GetAll(includeProperties: "Category,ProductImages");
            return View(productList);
        }

        public IActionResult Details(int ProductId)
        {
            var product = _unitOfWork.Product.Get(u => u.Id == ProductId, includeProperties: "Category,ProductImages");

            if (product == null)
            {
                return NotFound(); // Handle the case where the product is not found
            }

            ShoppingCart cart = new ShoppingCart
            {
                Product = product,
                Count = 1,
                ProductId = ProductId
            };
            return View(cart);
        }

       [HttpPost]
       [Authorize]
         public IActionResult Details(ShoppingCart shoppingCart)
         {
         var claimsIdentity = User.Identity as ClaimsIdentity;

          if (claimsIdentity != null)
          {
        var userIdClaim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim != null)
        {
            shoppingCart.ApplicationUserId = userIdClaim.Value;
            ShoppingCart cartFromdb = _unitOfWork.ShoppingCart.Get(u => u.ApplicationUserId == userIdClaim.Value
                && u.ProductId == shoppingCart.ProductId);

            if (cartFromdb != null)
            {
                // Shopping cart exists
                cartFromdb.Count += shoppingCart.Count;
                _unitOfWork.ShoppingCart.Update(cartFromdb);
                        _unitOfWork.Save();
                    }
            else
            {
                // Add cart record
                       _unitOfWork.ShoppingCart.Add(shoppingCart);
                        _unitOfWork.Save();
                        HttpContext.Session.SetInt32(SD.SessionCart,
                       _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userIdClaim.Value).Count());
            }
            TempData["Success"] = "cart Updated Successfully";
            
            return RedirectToAction("Index"); // Redirect to the index page or another appropriate action
        }
    }

    // Handle the case where User.Identity is not a ClaimsIdentity or the user ID claim is not found
     return RedirectToAction("Error"); // Redirect to an error page or another appropriate action
}


        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
