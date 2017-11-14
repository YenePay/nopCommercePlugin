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
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets Paypal URL
        /// </summary>
        /// <returns></returns>
        private string GetYenePayUrl()
        {
            return _yenePayPaymentSettings.CheckoutUrl;
            //return _yenePayPaymentSettings.UseSandbox ? Constants.CHECKOUT_URL :
            //    Constants.CHECKOUT_URL_SANDBOX;
        }

        private string GetYenePayPDTVerifyUrl()
        {
            return _yenePayPaymentSettings.PdtUrl;
            //return _yenePayPaymentSettings.UseSandbox ? Constants.PDT_VERIFY_URL :
            //    Constants.PDT_VERIFY_URL_SANDBOX;
        }

        private string GetYenePayIPNVerifyUrl()
        {
            return _yenePayPaymentSettings.IpnUrl;
            //return _yenePayPaymentSettings.UseSandbox ? Constants.IPN_VERIFY_URL :
            //    Constants.IPN_VERIFY_URL_SANDBOX;
        }
        /// <summary>
        /// Gets PDT details
        /// </summary>
        /// <param name="tx">TX</param>
        /// <param name="values">Values</param>
        /// <param name="response">Response</param>
        /// <returns>Result</returns>
        public bool GetPDTDetails(string tx, out Dictionary<string, string> values, out string response)
        {

            var req = (HttpWebRequest)WebRequest.Create(GetYenePayPDTVerifyUrl());
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";

            string formContent = string.Format("requestType=PDT&pdtToken={0}&transactionId={1}", _yenePayPaymentSettings.PdtToken, tx);
            req.ContentLength = formContent.Length;

            using (var sw = new StreamWriter(req.GetRequestStream(), Encoding.ASCII))
                sw.Write(formContent);

            response = null;
            bool success = false;
            //var result = req.GetResponse();
            //success = result.
            using (var sr = new StreamReader(req.GetResponse().GetResponseStream()))
                response = HttpUtility.UrlDecode(sr.ReadToEnd());

            values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bool firstLine = true;
            foreach (string l in response.Split('\n'))
            {
                string line = l.Trim();
                if (firstLine)
                {
                    success = line.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase);
                    firstLine = false;
                }
                else
                {
                    foreach (string innerLines in line.Split('&'))
                    {
                        int equalPox = innerLines.IndexOf('=');
                        if (equalPox >= 0)
                            values.Add(innerLines.Substring(0, equalPox), innerLines.Substring(equalPox + 1));
                    }
                }
            }

            return success;
        }

        /// <summary>
        /// Verifies IPN
        /// </summary>
        /// <param name="formString">Form string</param>
        /// <param name="values">Values</param>
        /// <returns>Result</returns>
        public bool VerifyIPN(string formString, out Dictionary<string, string> values)
        {
            var req = (HttpWebRequest)WebRequest.Create(GetYenePayIPNVerifyUrl());
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";

            string formContent = string.Format("{0}&=_notify-validate", formString);
            req.ContentLength = formContent.Length;

            using (var sw = new StreamWriter(req.GetRequestStream(), Encoding.ASCII))
            {
                sw.Write(formContent);
            }

            string response = null;
            using (var sr = new StreamReader(req.GetResponse().GetResponseStream()))
            {
                response = HttpUtility.UrlDecode(sr.ReadToEnd());
            }

            bool success = response.Trim().Contains("VERIFIED");

            values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string l in formString.Split('&'))
            {
                string line = l.Trim();
                int equalPox = line.IndexOf('=');
                if (equalPox >= 0)
                    values.Add(line.Substring(0, equalPox), line.Substring(equalPox + 1));
            }

            return success;
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
            var builder = new StringBuilder();
            builder.Append(GetYenePayUrl());
            string cmd = string.Empty;
            if (_yenePayPaymentSettings.PassProductNamesAndTotals)
            {
                cmd = "Cart";
            }
            else
            {
                cmd = "Express";
            }
            builder.AppendFormat("?Process={0}&MerchantId={1}", cmd, HttpUtility.UrlEncode(_yenePayPaymentSettings.MerchantCode));
            if (_yenePayPaymentSettings.PassProductNamesAndTotals)
            {
                //builder.AppendFormat("&upload=1");

                //get the items in the cart
                decimal cartTotal = decimal.Zero;
                var cartItems = postProcessPaymentRequest.Order.OrderItems;
                int x = 0;
                foreach (var item in cartItems)
                {
                    var unitPriceExclTax = GetETB(item.UnitPriceExclTax);
                    var priceExclTax = GetETB(item.PriceExclTax);
                    //round
                    var unitPriceExclTaxRounded = Math.Round(unitPriceExclTax, 2);
                    builder.AppendFormat("&Items[" + x + "].ItemName={0}", HttpUtility.UrlEncode(item.Product.Name));
                    builder.AppendFormat("&Items[" + x + "].UnitPrice={0}", unitPriceExclTaxRounded.ToString("0.00", CultureInfo.InvariantCulture));
                    builder.AppendFormat("&Items[" + x + "].Quantity={0}", item.Quantity);
                    x++;
                    cartTotal += priceExclTax;
                }

                //the checkout attributes that have a dollar value and send them to Paypal as items to be paid for
                var caValues = _checkoutAttributeParser.ParseCheckoutAttributeValues(postProcessPaymentRequest.Order.CheckoutAttributesXml);
                foreach (var val in caValues)
                {
                    var attPrice = _taxService.GetCheckoutAttributePrice(val, false, postProcessPaymentRequest.Order.Customer);
                    //round
                    var attPriceRounded = Math.Round(GetETB(attPrice), 2);
                    if (attPrice > decimal.Zero) //if it has a price
                    {
                        var ca = val.CheckoutAttribute;
                        if (ca != null)
                        {
                            var attName = ca.Name; //set the name
                            builder.AppendFormat("&Items[" + x + "].ItemName={0}", HttpUtility.UrlEncode(attName)); //name
                            builder.AppendFormat("&Items[" + x + "].UnitPrice={0}", attPriceRounded.ToString("0.00", CultureInfo.InvariantCulture)); //amount
                            builder.AppendFormat("&Items[" + x + "].Quantity={0}", 1); //quantity
                            x++;
                            cartTotal += attPrice;
                        }
                    }
                }

                //order totals

                //shipping
                var orderShippingExclTax = postProcessPaymentRequest.Order.OrderShippingExclTax;
                var orderShippingExclTaxRounded = Math.Round(GetETB(orderShippingExclTax), 2);
                if (orderShippingExclTax > decimal.Zero)
                {
                    builder.AppendFormat("&Items[" + x + "].ItemName={0}", "Shipping fee");
                    builder.AppendFormat("&Items[" + x + "].UnitPrice={0}", orderShippingExclTaxRounded.ToString("0.00", CultureInfo.InvariantCulture));
                    builder.AppendFormat("&Items[" + x + "].Quantity={0}", 1);
                    x++;
                    cartTotal += orderShippingExclTax;
                }

                //payment method additional fee
                var paymentMethodAdditionalFeeExclTax = postProcessPaymentRequest.Order.PaymentMethodAdditionalFeeExclTax;
                var paymentMethodAdditionalFeeExclTaxRounded = Math.Round(GetETB(paymentMethodAdditionalFeeExclTax), 2);
                if (paymentMethodAdditionalFeeExclTax > decimal.Zero)
                {
                    builder.AppendFormat("&Items[" + x + "].ItemName={0}}", "Payment method fee");
                    builder.AppendFormat("&Items[" + x + "].UnitPrice={0}", paymentMethodAdditionalFeeExclTaxRounded.ToString("0.00", CultureInfo.InvariantCulture));
                    builder.AppendFormat("&Items[" + x + "].Quantity={0}", 1);
                    x++;
                    cartTotal += paymentMethodAdditionalFeeExclTax;
                }

                //tax
                var orderTax = postProcessPaymentRequest.Order.OrderTax;
                var orderTaxRounded = Math.Round(GetETB(orderTax), 2);
                if (orderTax > decimal.Zero)
                {
                    //builder.AppendFormat("&tax_1={0}", orderTax.ToString("0.00", CultureInfo.InvariantCulture));

                    //add tax as item
                    builder.AppendFormat("&Items[" + x + "].ItemName={0}}", HttpUtility.UrlEncode("Sales Tax")); //name
                    builder.AppendFormat("&Items[" + x + "].UnitPrice={0}", orderTaxRounded.ToString("0.00", CultureInfo.InvariantCulture)); //amount
                    builder.AppendFormat("&Items[" + x + "].Quantity={0}", 1); //quantity

                    cartTotal += orderTax;
                    x++;
                }

                if (cartTotal > postProcessPaymentRequest.Order.OrderTotal)
                {
                    /* Take the difference between what the order total is and what it should be and use that as the "discount".
                     * The difference equals the amount of the gift card and/or reward points used. 
                     */
                    decimal discountTotal = cartTotal - postProcessPaymentRequest.Order.OrderTotal;
                    discountTotal = Math.Round(GetETB(discountTotal), 2);
                    //gift card or rewared point amount applied to cart in nopCommerce - shows in Paypal as "discount"
                    builder.AppendFormat("&Discount={0}", discountTotal.ToString("0.00", CultureInfo.InvariantCulture));
                }
            }
            else
            {
                //pass order total
                builder.AppendFormat("&ItemName=Order Number {0}", postProcessPaymentRequest.Order.Id);
                var orderTotal = Math.Round(GetETB(postProcessPaymentRequest.Order.OrderTotal), 2);
                builder.AppendFormat("&UnitPrice={0}", orderTotal.ToString("0.00", CultureInfo.InvariantCulture));
                builder.AppendFormat("&Quantity={0}", 1); //quantity
            }

            builder.AppendFormat("&MerchantOrderId={0}", postProcessPaymentRequest.Order.OrderGuid);
            //builder.AppendFormat("&charset={0}", "utf-8");
            //builder.Append(string.Format("&no_note=1&currency_code={0}", HttpUtility.UrlEncode(_currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId).CurrencyCode)));
            //builder.AppendFormat("&invoice={0}", postProcessPaymentRequest.Order.Id);
            //builder.AppendFormat("&rm=2", new object[0]);
            if (postProcessPaymentRequest.Order.ShippingStatus != ShippingStatus.ShippingNotRequired)
                builder.AppendFormat("&no_shipping=2", new object[0]);
            else
                builder.AppendFormat("&no_shipping=1", new object[0]);

            string returnUrl = _webHelper.GetStoreLocation(false) + "Plugins/PaymentYenePayChekout/PDTHandler";
            string cancelReturnUrl = _webHelper.GetStoreLocation(false) + "Plugins/PaymentYenePayChekout/CancelOrder";
            builder.AppendFormat("&SuccessUrl={0}&CancelUrl={1}", HttpUtility.UrlEncode(returnUrl), HttpUtility.UrlEncode(cancelReturnUrl));

            //Instant Payment Notification (server to server message)
            if (_yenePayPaymentSettings.EnableIpn)
            {
                string ipnUrl;
                if (String.IsNullOrWhiteSpace(_yenePayPaymentSettings.IpnUrl))
                    ipnUrl = _webHelper.GetStoreLocation(false) + "Plugins/PaymentYenePayChekout/IPNHandler";
                else
                    ipnUrl = _yenePayPaymentSettings.IpnUrl;
                builder.AppendFormat("&IPNUrl={0}", ipnUrl);
            }

            ////address
            //builder.AppendFormat("&address_override=1");
            //builder.AppendFormat("&first_name={0}", HttpUtility.UrlEncode(postProcessPaymentRequest.Order.BillingAddress.FirstName));
            //builder.AppendFormat("&last_name={0}", HttpUtility.UrlEncode(postProcessPaymentRequest.Order.BillingAddress.LastName));
            //builder.AppendFormat("&address1={0}", HttpUtility.UrlEncode(postProcessPaymentRequest.Order.BillingAddress.Address1));
            //builder.AppendFormat("&address2={0}", HttpUtility.UrlEncode(postProcessPaymentRequest.Order.BillingAddress.Address2));
            //builder.AppendFormat("&city={0}", HttpUtility.UrlEncode(postProcessPaymentRequest.Order.BillingAddress.City));
            ////if (!String.IsNullOrEmpty(postProcessPaymentRequest.Order.BillingAddress.PhoneNumber))
            ////{
            ////    //strip out all non-digit characters from phone number;
            ////    string billingPhoneNumber = System.Text.RegularExpressions.Regex.Replace(postProcessPaymentRequest.Order.BillingAddress.PhoneNumber, @"\D", string.Empty);
            ////    if (billingPhoneNumber.Length >= 10)
            ////    {
            ////        builder.AppendFormat("&night_phone_a={0}", HttpUtility.UrlEncode(billingPhoneNumber.Substring(0, 3)));
            ////        builder.AppendFormat("&night_phone_b={0}", HttpUtility.UrlEncode(billingPhoneNumber.Substring(3, 3)));
            ////        builder.AppendFormat("&night_phone_c={0}", HttpUtility.UrlEncode(billingPhoneNumber.Substring(6, 4)));
            ////    }
            ////}
            //if (postProcessPaymentRequest.Order.BillingAddress.StateProvince != null)
            //    builder.AppendFormat("&state={0}", HttpUtility.UrlEncode(postProcessPaymentRequest.Order.BillingAddress.StateProvince.Abbreviation));
            //else
            //    builder.AppendFormat("&state={0}", "");
            //if (postProcessPaymentRequest.Order.BillingAddress.Country != null)
            //    builder.AppendFormat("&country={0}", HttpUtility.UrlEncode(postProcessPaymentRequest.Order.BillingAddress.Country.TwoLetterIsoCode));
            //else
            //    builder.AppendFormat("&country={0}", "");
            //builder.AppendFormat("&zip={0}", HttpUtility.UrlEncode(postProcessPaymentRequest.Order.BillingAddress.ZipPostalCode));
            //builder.AppendFormat("&email={0}", HttpUtility.UrlEncode(postProcessPaymentRequest.Order.BillingAddress.Email));
            _httpContext.Response.Redirect(builder.ToString());
        }

        private decimal GetETB(decimal ammount)
        {
            return _currencyService.ConvertFromPrimaryStoreCurrency(ammount, _currencyService.GetCurrencyByCode("Br"));
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
                CheckoutUrl = "https://www.yenepay.com/checkout/",
                PdtUrl = "https://www.yenepay.com/api/verify/pdt",
                PdtToken = "",
                PdtValidateOrderTotal = true,
                EnableIpn = true,
                IpnUrl = "https://www.yenepay.com/api/verify/ipn",
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
        #endregion
    }
}
