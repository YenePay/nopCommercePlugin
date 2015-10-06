using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.YenePayCheckout.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.YenePayCheckout.Fields.UseSandbox")]
        public bool UseSandbox { get; set; }
        public bool UseSandbox_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.YenePayCheckout.Fields.MerchantCode")]
        public string MerchantCode { get; set; }

        [NopResourceDisplayName("Plugins.Payments.YenePayCheckout.Fields.CheckoutUrl")]
        public string CheckoutUrl { get; set; }
        public bool MerchantId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.YenePayCheckout.Fields.PDTToken")]
        public string PdtToken { get; set; }

        [NopResourceDisplayName("Plugins.Payments.YenePayCheckout.Fields.PDTUrl")]
        public string PdtUrl { get; set; }
        public bool PdtToken_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.YenePayCheckout.Fields.PDTValidateOrderTotal")]
        public bool PdtValidateOrderTotal { get; set; }
        public bool PdtValidateOrderTotal_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.YenePayCheckout.Fields.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFee_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.YenePayCheckout.Fields.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
        public bool AdditionalFeePercentage_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.YenePayCheckout.Fields.PassProductNamesAndTotals")]
        public bool PassProductNamesAndTotals { get; set; }
        public bool PassProductNamesAndTotals_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.YenePayCheckout.Fields.EnableIpn")]
        public bool EnableIpn { get; set; }
        public bool EnableIpn_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.YenePayCheckout.Fields.IpnUrl")]
        public string IpnUrl { get; set; }
        public bool IpnUrl_OverrideForStore { get; set; }
    }
}