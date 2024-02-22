using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using System.Linq;
using System;
using Bulky.DataAccess.Data;

namespace Bulky.DataAccess.Repository
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
    {
        private readonly ApplicationDbContext _db;

        public OrderHeaderRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(OrderHeader obj)
        {
            _db.OrderHeaders.Update(obj);
        }

        public void UpdateStatus(int id, string orderStatus, string? paymentStatus = null)
        {
            var orderFromDb = _db.OrderHeaders.FirstOrDefault(u => u.Id == id);
            if (orderFromDb != null)
            {
                orderFromDb.OrderStatus = orderStatus;
                if (!string.IsNullOrEmpty(paymentStatus))
                {
                    orderFromDb.PaymentStatus = paymentStatus;
                }
            }
        }

        public void UpdateStripePaymentID(int id, string sessionId, string stripepaymentIntentId)
        {
            var orderFromDb = _db.OrderHeaders.FirstOrDefault(u => u.Id == id);
           if (!string.IsNullOrEmpty(sessionId))
           {
                orderFromDb.SessionId = sessionId;
            }
            if (!string.IsNullOrEmpty(stripepaymentIntentId))
            {
                orderFromDb.paymentIntentId = stripepaymentIntentId;
                orderFromDb .PaymentDate = DateTime.Now;
            }
        }
    }
}
