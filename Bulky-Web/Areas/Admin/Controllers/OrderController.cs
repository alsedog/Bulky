using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Models;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace Bulky_Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitofWork;
        [BindProperty]
        public OrderVM OrderVM { get; set; }

        public OrderController(IUnitOfWork unitOfWork)
        {
                _unitofWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int orderId)
        {
            OrderVM = new()
            {
                OrderHeader = _unitofWork.OrderHeader.Get(u => u.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetails = _unitofWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderId, includeProperties: "Product")
            };
            return View(OrderVM);
        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult UpdateOrderDetail(OrderVM orderVM)
        {
            var orderHeaderFromDb = _unitofWork.OrderHeader.Get(u => u.Id == orderVM.OrderHeader.Id);

            if (orderHeaderFromDb != null)
            {
                orderHeaderFromDb.Name = orderVM.OrderHeader.Name;
                orderHeaderFromDb.PhoneNumber = orderVM.OrderHeader.PhoneNumber;
                orderHeaderFromDb.StreetAddress = orderVM.OrderHeader.StreetAddress;
                orderHeaderFromDb.City = orderVM.OrderHeader.City;
                orderHeaderFromDb.State = orderVM.OrderHeader.State;
                orderHeaderFromDb.PostalCode = orderVM.OrderHeader.PostalCode;

                if (!string.IsNullOrEmpty(orderVM.OrderHeader.TrackingNumber))
                {
                    orderHeaderFromDb.Carrier = orderVM.OrderHeader.TrackingNumber;
                }

                _unitofWork.OrderHeader.Update(orderHeaderFromDb);
                _unitofWork.Save();
                TempData["Success"] = "Order Details Updated Successfully.";
            }

            return RedirectToAction(nameof (Details), new {orderId= orderHeaderFromDb.Id});
        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _unitofWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);   
            _unitofWork.Save();
            TempData["Success"] = "Order Details Updated Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {
            var orderHeader = _unitofWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
            orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            orderHeader.TrackingNumber = OrderVM.OrderHeader.Carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.paymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));

            }
            _unitofWork.OrderHeader.Update(orderHeader);
            _unitofWork.Save();

            _unitofWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusShipped);
            _unitofWork.Save();
            TempData["Success"] = "Order Shipped Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder()
        { 
        
            var orderHeader = _unitofWork.OrderHeader.Get(u=> u.Id == OrderVM.OrderHeader.Id);

            if(orderHeader.PaymentStatus == SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.paymentIntentId

                };

                var service = new RefundService();
                Refund refund = service.Create(options);

                _unitofWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefund);

            }
            else
            {
                _unitofWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
            }
            
            _unitofWork.Save();
            TempData["Success"] = "Order cancelled Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });

        }
        [ActionName("Details")]
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult Details_PAY_NOW()
            
        {
           
            OrderVM.OrderHeader = _unitofWork.OrderHeader
           .Get(u => u.Id == OrderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
            OrderVM.OrderDetails = _unitofWork.OrderDetail
           .GetAll(u => u.OrderHeaderId == OrderVM.OrderHeader.Id , includeProperties: "Product");

            // stripe logic
            var domain = "https://localhost:7119/";
            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.Id}",
                CancelUrl = domain + $"admin/order/details?orderId={OrderVM.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            foreach (var item in OrderVM.OrderDetails)
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
            _unitofWork.OrderHeader.UpdateStripePaymentID(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitofWork.Save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);

            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });

        }
        public ActionResult PaymentConfirmation(int orderHeaderId)
        {
            OrderHeader orderHeader = _unitofWork.OrderHeader.Get(u => u.Id == orderHeaderId);
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                // this is an order by the company
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitofWork.OrderHeader.UpdateStripePaymentID(orderHeaderId, session.Id, session.PaymentIntentId);
                    _unitofWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus ,SD.PaymentStatusApproved);
                    _unitofWork.Save();
                }
            }

            
            return View(orderHeaderId);
        }



        #region API CALLS
        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> objOrderHeaders;

            if(User.IsInRole(SD.Role_Admin)|| User.IsInRole(SD.Role_Employee))
            {
                objOrderHeaders = _unitofWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                objOrderHeaders = _unitofWork.OrderHeader
               .GetAll(u => u.ApplicationUserId == userId, includeProperties: "ApplicationUser");
            }

            switch (status)
            {
                case "pending":
                    objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
                default:
                    break;
            }

            return Json(new { data = objOrderHeaders });
        }


        #endregion

    }
}
