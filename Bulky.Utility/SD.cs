using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Utility
{
    public static class SD
    {
        public const String Role_Customer = "Customer";
        public const String Role_Company = "Company";
        public const String Role_Admin = "Admin";
        public const String Role_Employee = "Employee";

        public const string StatusPending = "Pending";
        public const string StatusApproved = "Approved";
        public const string StatusInProcess = "Processing";
        public const string StatusShipped = "Shipped";
        public const string StatusCancelled = "Cancelled";
        public const string StatusRefund = "Refund";

        public const string PaymentStatusPending = "Pending";
        public const string PaymentStatusApproved = "Approved";
        public const string PaymentStatusDelayedPayment = "ApprovedForDelayedPayment";
        public const string PaymentStatusRejected = "Rejected";


        public const string SessionCart = "SessionShoppingCart";

    }
}
