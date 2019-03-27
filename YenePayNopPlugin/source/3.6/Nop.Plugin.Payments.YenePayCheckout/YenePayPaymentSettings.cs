using Nop.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.YenePayCheckout
{
    public class YenePayPaymentSettings : ISettings
    {
        public bool UseSandbox { get; set; }
        public string MerchantCode { get; set; }

        //public string CheckoutUrl { get; set; }
        public string PdtUrl { get; set; }
        public string PdtToken { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }
        /// <summary>
        /// Additional fee
        /// </summary>
        public decimal AdditionalFee { get; set; }
        public bool PassProductNamesAndTotals { get; set; }
        public bool PdtValidateOrderTotal { get; set; }
        public bool EnableIpn { get; set; }
        public string IpnUrl { get; set; }
    }
}
