using Nop.Core.Plugins;
using Nop.Services.Payments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Services.Localization;
using System.Web;
using Nop.Services.Orders;
using Nop.Services.Tax;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Services.Directory;
using Nop.Services.Configuration;
using System.Net;
using System.IO;
using Nop.Plugin.Payments.YenePayCheckout.Controllers;
using Nop.Core.Domain.Payments;
using System.Globalization;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Orders;
using System.Web.Routing;
using System.Net.Http;
using YenePaySdk;

namespace Nop.Plugin.Payments.YenePayCheckout
{
    public class YenePayPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields
        private readonly YenePayPaymentSettings _yenePayPaymentSettings;
        private readonly ISettingService _settingService;
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly IWebHelper _webHelper;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ITaxService _taxService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly HttpContextBase _httpContext;
        private CheckoutHelper _checkoutHelper;
        #endregion

        #region Ctor

        public YenePayPaymentProcessor(YenePayPaymentSettings yenePayPaymentSettings,
            ISettingService settingService, ICurrencyService currencyService,
            CurrencySettings currencySettings, IWebHelper webHelper,
            ICheckoutAttributeParser checkoutAttributeParser, ITaxService taxService,
            IOrderTotalCalculationService orderTotalCalculationService, HttpContextBase httpContext)
        {
            this._yenePayPaymentSettings = yenePayPaymentSettings;
            this._settingService = settingService;
            this._currencyService = currencyService;
            this._currencySettings = currencySettings;
            this._webHelper = webHelper;
            this._checkoutAttributeParser = checkoutAttributeParser;
            this._taxService = taxService;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._httpContext = httpContext;
            this._checkoutHelper = new CheckoutHelper();
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets PDT details
        /// </summary>
        /// <param name="tx">TX</param>
        /// <param name="values">Values</param>
        /// <param name="response">Response</param>
        /// <returns>Result</returns>
        //public bool GetPDTDetails(string tx, string orderId, out Dictionary<string, string> values, out string response)
        public async Task<Dictionary<string, string>> GetPDTDetails(string tx, string orderId)
        {
            PDTRequestModel model = new PDTRequestModel(_yenePayPaymentSettings.PdtToken, new Guid(tx), orderId);
            model.UseSandbox = _yenePayPaymentSettings.UseSandbox;
            var pdtResponse = await CheckoutHelper.RequestPDT(model);

            return pdtResponse;
        }

        /// <summary>
        /// Verifies IPN
        /// </summary>
        /// <param name="formString">Form string</param>
        /// <param name="values">Values</param>
        /// <returns>Result</returns>
        //public bool VerifyIPN(string formString, out Dictionary<string, string> values)
        public async Task<bool> VerifyIPN(IPNModel ipnModel)
        {
            //var result = string.Empty;
            ipnModel.UseSandbox = _yenePayPaymentSettings.UseSandbox;
            if (ipnModel != null)
            {
                var isIPNValid = await CheckoutHelper.IsIPNAuthentic(ipnModel);
                return isIPNValid;
            }
            return false;
        }
        #endregion

        #region Methods
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.NewPaymentStatus = PaymentStatus.Pending;
            return result;
        }

        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            CheckoutOptions checkoutOptions = new CheckoutOptions();
            checkoutOptions.UseSandbox = _yenePayPaymentSettings.UseSandbox;

            if (_yenePayPaymentSettings.PassProductNamesAndTotals)
            {
                checkoutOptions.Process = CheckoutType.Cart;
            }
            else
            {
                checkoutOptions.Process = CheckoutType.Express;
            }
            checkoutOptions.SellerCode = HttpUtility.UrlEncode(_yenePayPaymentSettings.MerchantCode);
            List<CheckoutItem> checkoutItems = new List<CheckoutItem>();
            if (_yenePayPaymentSettings.PassProductNamesAndTotals)
            {
                //get the items in the cart
                decimal cartTotal = decimal.Zero;
                var cartItems = postProcessPaymentRequest.Order.OrderItems;
                //int x = 0;
                
                foreach (var item in cartItems)
                {
                    var unitPriceExclTax = GetETB(item.UnitPriceExclTax);
                    var priceExclTax = GetETB(item.PriceExclTax);
                    //round
                    var unitPriceExclTaxRounded = Math.Round(unitPriceExclTax, 2);
                    CheckoutItem checkoutItem = new CheckoutItem();
                    checkoutItem.ItemName = HttpUtility.UrlEncode(item.Product.Name);
                    checkoutItem.UnitPrice = unitPriceExclTaxRounded;
                    checkoutItem.Quantity = item.Quantity;
                    checkoutItems.Add(checkoutItem);
                    cartTotal += priceExclTax;
                }

                //the checkout attributes that have a dollar value and send them to Paypal as items to be paid for
                var caValues = _checkoutAttributeParser.ParseCheckoutAttributeValues(postProcessPaymentRequest.Order.CheckoutAttributesXml);
                foreach (var val in caValues)
                {
                    var attPrice = GetETB(_taxService.GetCheckoutAttributePrice(val, false, postProcessPaymentRequest.Order.Customer));
                    //round
                    var attPriceRounded = Math.Round(attPrice, 2);
                    if (attPrice > decimal.Zero) //if it has a price
                    {
                        var ca = val.CheckoutAttribute;
                        if (ca != null)
                        {
                            var attName = ca.Name; //set the name
                            CheckoutItem checkoutItem = new CheckoutItem();
                            checkoutItem.ItemName = HttpUtility.UrlEncode(attName);
                            checkoutItem.UnitPrice = attPriceRounded;
                            checkoutItem.Quantity = 1;
                            checkoutItems.Add(checkoutItem);

                            cartTotal += attPrice;
                        }
                    }
                }

                //order totals

                //shipping
                var orderShippingExclTax = GetETB(postProcessPaymentRequest.Order.OrderShippingExclTax);
                var orderShippingExclTaxRounded = Math.Round(orderShippingExclTax, 2);
                if (orderShippingExclTax > decimal.Zero)
                {
                    checkoutOptions.TotalItemsDeliveryFee = orderShippingExclTaxRounded;

                    cartTotal += orderShippingExclTax;
                }

                //payment method additional fee
                var paymentMethodAdditionalFeeExclTax = GetETB(postProcessPaymentRequest.Order.PaymentMethodAdditionalFeeExclTax);
                var paymentMethodAdditionalFeeExclTaxRounded = Math.Round(paymentMethodAdditionalFeeExclTax, 2);
                if (paymentMethodAdditionalFeeExclTax > decimal.Zero)
                {
                    CheckoutItem checkoutItem = new CheckoutItem();
                    checkoutItem.ItemName = "Payment method fee";
                    checkoutItem.UnitPrice = paymentMethodAdditionalFeeExclTaxRounded;
                    checkoutItem.Quantity = 1;
                    checkoutItems.Add(checkoutItem);

                    cartTotal += paymentMethodAdditionalFeeExclTax;
                }

                //tax
                var orderTax = GetETB(postProcessPaymentRequest.Order.OrderTax);
                var orderTaxRounded = Math.Round(orderTax, 2);
                if (orderTax > decimal.Zero)
                {
                    checkoutOptions.TotalItemsTax1 = orderTaxRounded;

                    cartTotal += orderTax;
                }

                // discount
                if (Math.Round(cartTotal,2) > Math.Round(GetETB(postProcessPaymentRequest.Order.OrderTotal),2))
                {
                    /* Take the difference between what the order total is and what it should be and use that as the "discount".
                     * The difference equals the amount of the gift card and/or reward points used. 
                     */
                    decimal discountTotal = cartTotal - postProcessPaymentRequest.Order.OrderTotal;
                    discountTotal = Math.Round(GetETB(discountTotal), 2);
                    //gift card or rewared point amount applied to cart in nopCommerce - shows in YenePay as "discount"
                    checkoutOptions.TotalItemsDiscount = discountTotal;
                }
            }
            else
            {
                //pass order total                
                var orderTotal = Math.Round(GetETB(postProcessPaymentRequest.Order.OrderTotal), 2);
                CheckoutItem checkoutItem = new CheckoutItem();
                checkoutItem.ItemName = string.Format("Order Number {0}", postProcessPaymentRequest.Order.Id);
                checkoutItem.UnitPrice = orderTotal;
                checkoutItem.Quantity = 1;
            }

            checkoutOptions.OrderId = postProcessPaymentRequest.Order.OrderGuid.ToString();

            //set successreturnurl and cancelreturnurl
            string returnUrl = _webHelper.GetStoreLocation(false) + "Plugins/PaymentYenePayChekout/PDTHandler";
            string cancelReturnUrl = _webHelper.GetStoreLocation(false) + "Plugins/PaymentYenePayChekout/CancelOrder";
            checkoutOptions.SuccessReturn = HttpUtility.UrlEncode(returnUrl);
            checkoutOptions.CancelReturn = HttpUtility.UrlEncode(cancelReturnUrl);

            //Instant Payment Notification (server to server message)
            if (_yenePayPaymentSettings.EnableIpn)
            {
                string ipnUrl;
                if (String.IsNullOrWhiteSpace(_yenePayPaymentSettings.IpnUrl))
                    ipnUrl = _webHelper.GetStoreLocation(false) + "Plugins/PaymentYenePayChekout/IPNHandler";
                else
                    ipnUrl = _yenePayPaymentSettings.IpnUrl;

                checkoutOptions.IpnUrlReturn = ipnUrl;
            }
            
            string checkoutUrl = CheckoutHelper.GetCheckoutUrl(checkoutOptions, checkoutItems);

            //redirect customer to YenePay checkout to complete payment
            _httpContext.Response.Redirect(checkoutUrl);
        }

        public decimal GetETB(decimal ammount)
        {
            return _currencyService.ConvertFromPrimaryExchangeRateCurrency(ammount, _currencyService.GetCurrencyByCode("ETB"));
        }

        public decimal GetAdditionalHandlingFee(IList<Core.Domain.Orders.ShoppingCartItem> cart)
        {
            var result = this.CalculateAdditionalFee(_orderTotalCalculationService, cart,
               _yenePayPaymentSettings.AdditionalFee, _yenePayPaymentSettings.AdditionalFeePercentage);
            return result;
        }

        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            //let's ensure that at least 1 minute passed after order is placed
            if ((DateTime.UtcNow - order.CreatedOnUtc).TotalMinutes < 1)
                return false;

            return true;
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "PaymentYenePayCheckout";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.YenePayChekout.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Gets a route for payment info
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentYenePayCheckout";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.YenePayChekout.Controllers" }, { "area", null } };
        }

        public Type GetControllerType()
        {
            return typeof(PaymentYenePayCheckoutController);
        }

        public override void Install()
        {
            //settings
            var settings = new YenePayPaymentSettings()
            {
                UseSandbox = false,
                MerchantCode = "",
                //CheckoutUrl = "https://www.yenepay.com/checkout/",
                PdtUrl = "https://endpoints.yenepay.com/api/verify/pdt/",
                PdtToken = "",
                PdtValidateOrderTotal = true,
                EnableIpn = true,
                IpnUrl = "https://endpoints.yenepay.com/api/verify/ipn/",
                PassProductNamesAndTotals = true
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.RedirectionTip", "You will be redirected to YenePay site to complete the order.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.UseSandbox", "Use Sandbox");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.UseSandbox.Hint", "Check to enable Sandbox (testing environment).");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.MerchantCode", "Merchant Code");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.CheckoutUrl", "Checkout endpoint url");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.MerchantCode.Hint", "Specify your YenePay merchant code.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.PDTUrl", "PDT endpoint url");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.PDTToken", "PDT Identity Token");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.PDTToken.Hint", "Specify PDT identity token");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.PDTValidateOrderTotal", "PDT. Validate order total");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.PDTValidateOrderTotal.Hint", "Check if PDT handler should validate order totals.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.AdditionalFee", "Additional fee");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.AdditionalFee.Hint", "Enter additional fee to charge your customers.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.AdditionalFeePercentage", "Additional fee. Use percentage");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.AdditionalFeePercentage.Hint", "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.PassProductNamesAndTotals", "Pass product names and order totals to YenePay");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.PassProductNamesAndTotals.Hint", "Check if product names and order totals should be passed to YenePay.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.EnableIpn", "Enable IPN (Instant Payment Notification)");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.EnableIpn.Hint", "Check if IPN is enabled.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.EnableIpn.Hint2", "Leave blank to use the default IPN handler URL.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.IpnUrl", "IPN Handler");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.IpnUrl.Hint", "Specify IPN Handler.");

            base.Install();
        }
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<YenePayPaymentSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.RedirectionTip");
            this.DeletePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.UseSandbox");
            this.DeletePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.UseSandbox.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.MerchantCode");
            this.DeletePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.MerchantCode.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.CheckoutUrl");
            this.DeletePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.PDTToken");
            this.DeletePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.PDTUrl");
            this.DeletePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.PDTToken.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.PDTValidateOrderTotal");
            this.DeletePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.PDTValidateOrderTotal.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.AdditionalFee");
            this.DeletePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.AdditionalFee.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.AdditionalFeePercentage");
            this.DeletePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.AdditionalFeePercentage.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.PassProductNamesAndTotals");
            this.DeletePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.PassProductNamesAndTotals.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.EnableIpn");
            this.DeletePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.EnableIpn.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.EnableIpn.Hint2");
            this.DeletePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.IpnUrl");
            this.DeletePluginLocaleResource("Plugins.Payments.YenePayCheckout.Fields.IpnUrl.Hint");

            base.Uninstall();
        }

        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return false;
        }

        #endregion

        #region Properies

        public bool SupportCapture
        {
            get { return true; }
        }

        public bool SupportPartiallyRefund
        {
            get { return false; }
        }

        public bool SupportRefund
        {
            get { return false; }
        }

        public bool SupportVoid
        {
            get { return false; }
        }

        public RecurringPaymentType RecurringPaymentType
        {
            get { return Services.Payments.RecurringPaymentType.NotSupported; }
        }

        public PaymentMethodType PaymentMethodType
        {
            get { return Services.Payments.PaymentMethodType.Redirection; }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get
            {
                return false;
            }
        }
        #endregion
    }
}
