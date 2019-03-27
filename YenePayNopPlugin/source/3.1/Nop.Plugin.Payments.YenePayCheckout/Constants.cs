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

        public const string BASE_YENEPAY_URL = "http://www.yeneapy.com";
        public const string BASE_YENEPAY_URL_SANDBOX = "http://www.yeneapy.com";
        public const string CHECKOUT_URL = BASE_YENEPAY_URL + "/checkout/Home/Process";
        public const string PDT_VERIFY_URL = BASE_YENEPAY_URL + "/api/api/Verify/PDT";
        public const string IPN_VERIFY_URL = BASE_YENEPAY_URL + "/api/api/Verify/IPN";

        public const string CHECKOUT_URL_SANDBOX = BASE_YENEPAY_URL_SANDBOX + "/checkout/Home/Process";
        public const string PDT_VERIFY_URL_SANDBOX = BASE_YENEPAY_URL_SANDBOX + "/api/api/Verify/PDT";
        public const string IPN_VERIFY_URL_SANDBOX = BASE_YENEPAY_URL_SANDBOX + "/api/api/Verify/IPN";
    }
}
