using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.ViewModels;
using System.Security.Claims;
using Bulky.Models;
using Bulky.Utility;
using Stripe.Checkout; // Add this using statement
using SessionService = Stripe.Checkout.SessionService;
using SessionCreateOptions = Stripe.Checkout.SessionCreateOptions;
using Microsoft.Azure.ActiveDirectory.GraphClient.Internal;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.DirectoryServices.ActiveDirectory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
namespace Bulky_Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
        }

        public IActionResult Index()
        {
            string userId = null; // Define userId and initialize it to null

            var claimsIdentity = User.Identity as ClaimsIdentity;

            if (claimsIdentity != null)
            {
                var userIdClaim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim != null)
                {
                    userId = userIdClaim.Value; // Assign the value inside the if statement
                }
            }

            if (userId != null) // Check if userId has a valid value
            {
                ShoppingCartVM = new ShoppingCartVM
                {
                    ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                    includeProperties: "Product"),
                    OrderHeader = new()

                };

                IEnumerable<ProductImage> productImages = _unitOfWork.ProductImage.GetAll();

                foreach (var cart in ShoppingCartVM.ShoppingCartList)
                {
                    cart.Product.ProductImages = productImages.Where(u => u.ProductId == cart.Product.Id).ToList();
                    cart.Price = GetPriceBasedQuantity(cart);
                    ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
                }
            }
            else
            {
                // Handle the case where userId is not available (e.g., user is not authenticated)
            }

            // Your existing logic for the Index action
            return View(ShoppingCartVM);
        }

        public IActionResult plus(int cartId)
        {
            var cartFromdb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            cartFromdb.Count += 1;
            _unitOfWork.ShoppingCart.Update(cartFromdb);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }
        public IActionResult Summary()
        {
            string userId = null; // Define userId and initialize it to null

            var claimsIdentity = User.Identity as ClaimsIdentity;

            if (claimsIdentity != null)
            {
                var userIdClaim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim != null)
                {
                    userId = userIdClaim.Value; // Assign the value inside the if statement
                }
            }

            if (userId != null) // Check if userId has a valid value
            {
                ShoppingCartVM = new ShoppingCartVM
                {
                    ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                    includeProperties: "Product"),
                    OrderHeader = new()
                };

                // Fetch the ApplicationUser from the database
                ApplicationUser user = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

                if (user != null) // Check if the user is found
                {
                    ShoppingCartVM.OrderHeader.ApplicationUserId = userId;
                    ShoppingCartVM.OrderHeader.ApplicationUser = user;
                    ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
                    ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
                    ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
                    ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
                    ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
                    ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;
                }
                else
                {
                    // Handle the case where the user is not found
                    // You can return an error view or perform other appropriate actions
                }

                foreach (var cart in ShoppingCartVM.ShoppingCartList)
                {
                    cart.Price = GetPriceBasedQuantity(cart);
                    ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
                }
            }
            else
            {
                // Handle the case where userId is not available (e.g., user is not authenticated)
            }

            // Your existing logic for the Index action
            return View(ShoppingCartVM);
        }

        [HttpPost]
        [ActionName("Summary")]
        public IActionResult SummaryPOST()
        {
            string userId = null; // Define userId and initialize it to null
            ApplicationUser applicationuser = null; // Declare applicationuser at this level

            var claimsIdentity = User.Identity as ClaimsIdentity;

            if (claimsIdentity != null)
            {
                var userIdClaim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim != null)
                {
                    userId = userIdClaim.Value; // Assign the value inside the if statement
                }
            }

            if (userId != null) // Check if userId has a valid value
            {
                ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                    includeProperties: "Product");

                ShoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now;
                ShoppingCartVM.OrderHeader.ApplicationUserId = userId;

                applicationuser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

                foreach (var cart in ShoppingCartVM.ShoppingCartList)
                {
                    cart.Price = GetPriceBasedQuantity(cart);
                    ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
                }
            }

            if (applicationuser.CompanyId.GetValueOrDefault() == 0)
            {
                // it is a regular customer 
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            }
            else
            {
                // it is a company user
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }

            _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitOfWork.Save();
            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count,
                };
                _unitOfWork.OrderDetail.Add(orderDetail);
                _unitOfWork.Save();
            }

            if (applicationuser.CompanyId.GetValueOrDefault() == 0)
            {
                // It is a regular customer account, and you need to capture payment
                // stripe logic
               var domain = "https://localhost:7119/";
                var options = new SessionCreateOptions
                {
                    SuccessUrl = domain+ $"customer/cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
                    CancelUrl = domain+"customer/cart/index",
                    LineItems = new List<SessionLineItemOptions>(),                               
                    Mode = "payment",
                };

                foreach(var item in ShoppingCartVM.ShoppingCartList)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions()
                        {
                            UnitAmount = (long)(item.Price * 100), // $20.50 => 2050
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Title
                            }
                        },
                        Quantity = item.Count
                    };
                    options.LineItems.Add(sessionLineItem);
                }
                var service = new SessionService();
                Session session = service.Create(options);
                _unitOfWork.OrderHeader.UpdateStripePaymentID(ShoppingCartVM.OrderHeader.Id,session.Id, session.PaymentIntentId);
                _unitOfWork.Save();
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303); 

            }
            return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.Id });
        }
        public ActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");
            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                // this is an order by the customer
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
                HttpContext.Session.Clear();
            }
            _emailSender.SendEmailAsync(orderHeader.ApplicationUser.Email, "New Order - Bulky Store",
                $"<p>New Order Created - {orderHeader.Id}</p>");
        
            List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart
            .GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
            _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
            _unitOfWork.Save();
            return View(id);
        }

        public IActionResult minus(int cartId)
        {
            var cartFromdb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId, tracked: true);
            if(cartFromdb.Count < 1)
            {
                //remove that from cart
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart
                .GetAll(u => u.ApplicationUserId == cartFromdb.ApplicationUserId).Count() - 1);
                _unitOfWork.ShoppingCart.Remove(cartFromdb);
            }
            else
            {
                cartFromdb.Count -= 1;
                _unitOfWork.ShoppingCart.Update(cartFromdb);
            }
            
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }
        public IActionResult Remove(int cartId)
        {
            var cartFromdb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId,tracked:true);
            HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart
           .GetAll(u => u.ApplicationUserId == cartFromdb.ApplicationUserId).Count() - 1);
            if (cartFromdb != null)
            {
                _unitOfWork.ShoppingCart.Remove(cartFromdb);               
                _unitOfWork.Save();              
            }
            return RedirectToAction(nameof(Index));
        }
        

        private double GetPriceBasedQuantity(ShoppingCart shoppingCart)
        {
            if(shoppingCart.Count<= 50 )
            {
                return shoppingCart.Product.Price;
            }
            else
            {
                if(shoppingCart.Count<= 100)
                {
                    return shoppingCart.Product.Price50;
                }
                else
                {
                    return shoppingCart.Product.Price100;
                }
            }
        }
    }
}
