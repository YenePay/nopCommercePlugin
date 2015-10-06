using Nop.Core.Domain.Payments;

namespace Nop.Plugin.Payments.YenePayCheckout
{
    /// <summary>
    /// Represents paypal helper
    /// </summary>
    public class YenePayHelper
    {
        /// <summary>
        /// Gets a payment status
        /// </summary>
        /// <param name="paymentStatus">PayPal payment status</param>
        /// <param name="paymentMethod">PayPal pending reason</param>
        /// <returns>Payment status</returns>
        public static PaymentStatus GetPaymentStatus(string paymentStatus, string paymentMethod)
        {
            var result = PaymentStatus.Pending;

            if (paymentStatus == null)
                paymentStatus = string.Empty;

            if (paymentMethod == null)
                paymentMethod = string.Empty;

            switch (paymentStatus.ToLowerInvariant())
            {
                case "processing":
                case "waiting":
                    switch (paymentMethod.ToLowerInvariant())
                    {
                        case "mobilebanking":
                            result = PaymentStatus.Pending;
                            break;
                        default:
                            result = PaymentStatus.Pending;
                            break;
                    }
                    break;
                case "processed":
                case "completed":
                case "delivered":
                case "paid":
                    result = PaymentStatus.Paid;
                    break;
                case "canceled":
                case "erroroccured":
                case "new":
                case "voided":
                    result = PaymentStatus.Voided;
                    break;
                case "refunded":
                case "reversed":
                    result = PaymentStatus.Refunded;
                    break;
                default:
                    break;
            }
            return result;
        }
    }
}

