using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.YenePayCheckout
{
    public class Constants
    {
        public const string FIELDS_MERCHANT_ORDER_ID = "MerchantOrderId";
        public const string FIELDS_TOTAL = "TotalAmmount";
        public const string FIELDS_BUYER_ID = "BuyerId";
        public const string FIELDS_MERCHANT_ID = "MerchantId";
        public const string FIELDS_BUYER_NAME = "BuyerName";
        public const string FIELDS_TRANSACTION_FEE = "TransactionFee";
        public const string FIELDS_TRANSACTION_ID = "TransactionId";
        public const string FIELDS_STATUS = "Status";
        public const string FIELDS_PAYMENT_METHOD = "PaymentMethod";
        public const string FIELDS_INVOICE = "InvoiceId";
        public const string FIELDS_CURRENCY = "Currency";

        public const string BASE_YENEPAY_API_URL = "https://api.yeneapy.com";
        public const string BASE_YENEPAY_API_URL_SANDBOX = "http://localhost/api";
        public const string CHECKOUT_URL = "https://checkout.yeneapy.com" + "/Home/Process";
        public const string PDT_VERIFY_URL = BASE_YENEPAY_API_URL + "/api/api/Verify/PDT";
        public const string IPN_VERIFY_URL = BASE_YENEPAY_API_URL + "/api/api/Verify/IPN";

        public const string CHECKOUT_URL_SANDBOX = "http://localhost/checkout" + "/Home/Process";
        public const string PDT_VERIFY_URL_SANDBOX = BASE_YENEPAY_API_URL_SANDBOX + "/api/Verify/PDT";
        public const string IPN_VERIFY_URL_SANDBOX = BASE_YENEPAY_API_URL_SANDBOX + "/api/Verify/IPN";
    }
}
