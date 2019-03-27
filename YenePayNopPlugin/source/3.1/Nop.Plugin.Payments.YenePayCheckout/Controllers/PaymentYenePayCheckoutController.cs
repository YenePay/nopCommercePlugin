using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.YenePayCheckout.Models;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.YenePayCheckout.Controllers
{
    public class PaymentYenePayCheckoutController : BaseNopPaymentController
    {
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ILogger _logger;
        private readonly IWebHelper _webHelper;
        private readonly PaymentSettings _paymentSettings;

        public PaymentYenePayCheckoutController(IWorkContext workContext,
            IStoreService storeService, 
            ISettingService settingService, 
            IPaymentService paymentService, 
            IOrderService orderService, 
            IOrderProcessingService orderProcessingService, 
            ILogger logger, IWebHelper webHelper,
            PaymentSettings paymentSettings)
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._logger = logger;
            this._webHelper = webHelper;
            this._paymentSettings = paymentSettings;
        }
        
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var yenePayCheckoutPaymentSettings = _settingService.LoadSetting<YenePayPaymentSettings>(storeScope);

            var model = new ConfigurationModel();
            model.UseSandbox = yenePayCheckoutPaymentSettings.UseSandbox;
            model.MerchantCode = yenePayCheckoutPaymentSettings.MerchantCode;
            model.PdtToken = yenePayCheckoutPaymentSettings.PdtToken;
            model.PdtValidateOrderTotal = yenePayCheckoutPaymentSettings.PdtValidateOrderTotal;
            model.AdditionalFee = yenePayCheckoutPaymentSettings.AdditionalFee;
            model.AdditionalFeePercentage = yenePayCheckoutPaymentSettings.AdditionalFeePercentage;
            model.PassProductNamesAndTotals = yenePayCheckoutPaymentSettings.PassProductNamesAndTotals;
            model.EnableIpn = yenePayCheckoutPaymentSettings.EnableIpn;
            model.IpnUrl = yenePayCheckoutPaymentSettings.IpnUrl;

            model.ActiveStoreScopeConfiguration = storeScope;
            if (storeScope > 0)
            {
                model.UseSandbox_OverrideForStore = _settingService.SettingExists(yenePayCheckoutPaymentSettings, x => x.UseSandbox, storeScope);
                model.MerchantId_OverrideForStore = _settingService.SettingExists(yenePayCheckoutPaymentSettings, x => x.MerchantCode, storeScope);
                model.PdtToken_OverrideForStore = _settingService.SettingExists(yenePayCheckoutPaymentSettings, x => x.PdtToken, storeScope);
                model.PdtValidateOrderTotal_OverrideForStore = _settingService.SettingExists(yenePayCheckoutPaymentSettings, x => x.PdtValidateOrderTotal, storeScope);
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(yenePayCheckoutPaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(yenePayCheckoutPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
                model.PassProductNamesAndTotals_OverrideForStore = _settingService.SettingExists(yenePayCheckoutPaymentSettings, x => x.PassProductNamesAndTotals, storeScope);
                model.EnableIpn_OverrideForStore = _settingService.SettingExists(yenePayCheckoutPaymentSettings, x => x.EnableIpn, storeScope);
                model.IpnUrl_OverrideForStore = _settingService.SettingExists(yenePayCheckoutPaymentSettings, x => x.IpnUrl, storeScope);
            }

            return View("Nop.Plugin.Payments.YenePayCheckout.Views.PaymentYenePayCheckout.Configure", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var yenePayStandardPaymentSettings = _settingService.LoadSetting<YenePayPaymentSettings>(storeScope);

            //save settings
            yenePayStandardPaymentSettings.UseSandbox = model.UseSandbox;
            yenePayStandardPaymentSettings.MerchantCode = model.MerchantCode;
            yenePayStandardPaymentSettings.PdtToken = model.PdtToken;
            yenePayStandardPaymentSettings.PdtValidateOrderTotal = model.PdtValidateOrderTotal;
            yenePayStandardPaymentSettings.AdditionalFee = model.AdditionalFee;
            yenePayStandardPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
            yenePayStandardPaymentSettings.PassProductNamesAndTotals = model.PassProductNamesAndTotals;
            yenePayStandardPaymentSettings.EnableIpn = model.EnableIpn;
            yenePayStandardPaymentSettings.IpnUrl = model.IpnUrl;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            if (model.UseSandbox_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(yenePayStandardPaymentSettings, x => x.UseSandbox, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(yenePayStandardPaymentSettings, x => x.UseSandbox, storeScope);

            if (model.MerchantId_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(yenePayStandardPaymentSettings, x => x.MerchantCode, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(yenePayStandardPaymentSettings, x => x.MerchantCode, storeScope);

            if (model.PdtToken_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(yenePayStandardPaymentSettings, x => x.PdtToken, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(yenePayStandardPaymentSettings, x => x.PdtToken, storeScope);

            if (model.PdtValidateOrderTotal_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(yenePayStandardPaymentSettings, x => x.PdtValidateOrderTotal, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(yenePayStandardPaymentSettings, x => x.PdtValidateOrderTotal, storeScope);

            if (model.AdditionalFee_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(yenePayStandardPaymentSettings, x => x.AdditionalFee, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(yenePayStandardPaymentSettings, x => x.AdditionalFee, storeScope);

            if (model.AdditionalFeePercentage_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(yenePayStandardPaymentSettings, x => x.AdditionalFeePercentage, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(yenePayStandardPaymentSettings, x => x.AdditionalFeePercentage, storeScope);

            if (model.PassProductNamesAndTotals_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(yenePayStandardPaymentSettings, x => x.PassProductNamesAndTotals, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(yenePayStandardPaymentSettings, x => x.PassProductNamesAndTotals, storeScope);

            if (model.EnableIpn_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(yenePayStandardPaymentSettings, x => x.EnableIpn, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(yenePayStandardPaymentSettings, x => x.EnableIpn, storeScope);

            if (model.IpnUrl_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(yenePayStandardPaymentSettings, x => x.IpnUrl, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(yenePayStandardPaymentSettings, x => x.IpnUrl, storeScope);

            //now clear settings cache
            _settingService.ClearCache();

            return Configure();
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            return View("Nop.Plugin.Payments.YenePayCheckout.Views.PaymentYenePayCheckout.PaymentInfo");
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();
            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            return paymentInfo;
        }

        [ValidateInput(false)]
        public ActionResult PDTHandler(FormCollection form)
        {
            string tx = _webHelper.QueryString<string>(Constants.FIELDS_TRANSACTION_ID);
            Dictionary<string, string> values;
            string response;

            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.YenePayCheckout") as YenePayPaymentProcessor;
            if (processor == null ||
                !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("YenePay Standard module cannot be loaded");

            if (processor.GetPDTDetails(tx, out values, out response))
            {
                string orderNumber = string.Empty;
                values.TryGetValue(Constants.FIELDS_MERCHANT_ORDER_ID, out orderNumber);
                Guid orderNumberGuid = Guid.Empty;
                try
                {
                    orderNumberGuid = new Guid(orderNumber);
                }
                catch { }
                Order order = _orderService.GetOrderByGuid(orderNumberGuid);
                if (order != null)
                {
                    decimal total = decimal.Zero;
                    try
                    {
                        total = decimal.Parse(values[Constants.FIELDS_TOTAL], new CultureInfo("en-US"));
                    }
                    catch (Exception exc)
                    {
                        _logger.Error("YenePay PDT. Error getting mc_gross", exc);
                    }

                    string payer_status = string.Empty;
                    values.TryGetValue("payer_status", out payer_status);
                    string payment_status = string.Empty;
                    values.TryGetValue(Constants.FIELDS_STATUS, out payment_status);
                    string pending_reason = string.Empty;
                    values.TryGetValue(Constants.FIELDS_STATUS, out pending_reason);
                    string mc_currency = string.Empty;
                    values.TryGetValue(Constants.FIELDS_CURRENCY, out mc_currency);
                    string txn_id = string.Empty;
                    values.TryGetValue(Constants.FIELDS_TRANSACTION_ID, out txn_id);
                    string payment_type = string.Empty;
                    values.TryGetValue(Constants.FIELDS_PAYMENT_METHOD, out payment_type);
                    string payer_id = string.Empty;
                    values.TryGetValue(Constants.FIELDS_BUYER_ID, out payer_id);
                    string receiver_id = string.Empty;
                    values.TryGetValue(Constants.FIELDS_MERCHANT_ID, out receiver_id);
                    string invoice = string.Empty;
                    values.TryGetValue(Constants.FIELDS_INVOICE, out invoice);
                    string payment_fee = string.Empty;
                    values.TryGetValue(Constants.FIELDS_TRANSACTION_FEE, out payment_fee);

                    var sb = new StringBuilder();
                    sb.AppendLine("YenePay PDT:");
                    sb.AppendLine("total: " + total);
                    sb.AppendLine("Payer status: " + payer_status);
                    sb.AppendLine("Payment status: " + payment_status);
                    sb.AppendLine("Pending reason: " + pending_reason);
                    sb.AppendLine("mc_currency: " + mc_currency);
                    sb.AppendLine("txn_id: " + txn_id);
                    sb.AppendLine("payment_type: " + payment_type);
                    sb.AppendLine("payer_id: " + payer_id);
                    sb.AppendLine("receiver_id: " + receiver_id);
                    sb.AppendLine("invoice: " + invoice);
                    sb.AppendLine("payment_fee: " + payment_fee);


                    //order note
                    order.OrderNotes.Add(new OrderNote()
                    {
                        Note = sb.ToString(),
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow
                    });
                    _orderService.UpdateOrder(order);

                    //load settings for a chosen store scope
                    var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
                    var yenePayStandardPaymentSettings = _settingService.LoadSetting<YenePayPaymentSettings>(storeScope);

                    //validate order total
                    if (yenePayStandardPaymentSettings.PdtValidateOrderTotal && !Math.Round(total, 2).Equals(Math.Round(order.OrderTotal, 2)))
                    {
                        string errorStr = string.Format("YenePay PDT. Returned order total {0} doesn't equal order total {1}", total, order.OrderTotal);
                        _logger.Error(errorStr);

                        return RedirectToAction("Index", "Home", new { area = "" });
                    }

                    //mark order as paid
                    if (_orderProcessingService.CanMarkOrderAsPaid(order))
                    {
                        order.AuthorizationTransactionId = txn_id;
                        _orderService.UpdateOrder(order);

                        _orderProcessingService.MarkOrderAsPaid(order);
                    }
                }

                return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id});
            }
            else
            {
                string orderNumber = string.Empty;
                values.TryGetValue(Constants.FIELDS_MERCHANT_ORDER_ID, out orderNumber);
                Guid orderNumberGuid = Guid.Empty;
                try
                {
                    orderNumberGuid = new Guid(orderNumber);
                }
                catch { }
                Order order = _orderService.GetOrderByGuid(orderNumberGuid);
                if (order != null)
                {
                    //order note
                    order.OrderNotes.Add(new OrderNote()
                    {
                        Note = "YenePay PDT failed. " + response,
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow
                    });
                    _orderService.UpdateOrder(order);
                }
                return RedirectToAction("Index", "Home", new { area = "" });
            }
        }

        [ValidateInput(false)]
        public ActionResult IPNHandler()
        {
            byte[] param = Request.BinaryRead(Request.ContentLength);
            string strRequest = Encoding.ASCII.GetString(param);
            Dictionary<string, string> values;

            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.YenePayCheckout") as YenePayPaymentProcessor;
            if (processor == null ||
                !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("YenePay Standard module cannot be loaded");

            if (processor.VerifyIPN(strRequest, out values))
            {
                #region values
                decimal total = decimal.Zero;
                try
                {
                    total = decimal.Parse(values["mc_gross"], new CultureInfo("en-US"));
                }
                catch { }

                string payer_status = string.Empty;
                values.TryGetValue("payer_status", out payer_status);
                string payment_status = string.Empty;
                values.TryGetValue(Constants.FIELDS_STATUS, out payment_status);
                string pending_reason = string.Empty;
                values.TryGetValue(Constants.FIELDS_STATUS, out pending_reason);
                string mc_currency = string.Empty;
                values.TryGetValue(Constants.FIELDS_CURRENCY, out mc_currency);
                string txn_id = string.Empty;
                values.TryGetValue(Constants.FIELDS_TRANSACTION_ID, out txn_id);
                string payment_type = string.Empty;
                values.TryGetValue(Constants.FIELDS_PAYMENT_METHOD, out payment_type);
                string payer_id = string.Empty;
                values.TryGetValue(Constants.FIELDS_BUYER_ID, out payer_id);
                string receiver_id = string.Empty;
                values.TryGetValue(Constants.FIELDS_MERCHANT_ID, out receiver_id);
                string invoice = string.Empty;
                values.TryGetValue(Constants.FIELDS_INVOICE, out invoice);
                string payment_fee = string.Empty;
                values.TryGetValue(Constants.FIELDS_TRANSACTION_FEE, out payment_fee);

                #endregion

                var sb = new StringBuilder();
                sb.AppendLine("YenePay IPN:");
                foreach (KeyValuePair<string, string> kvp in values)
                {
                    sb.AppendLine(kvp.Key + ": " + kvp.Value);
                }

                var newPaymentStatus = YenePayHelper.GetPaymentStatus(payment_status, pending_reason);
                sb.AppendLine("New payment status: " + newPaymentStatus);
                string txn_type = string.Empty, rp_invoice_id = string.Empty;
                switch (txn_type)
                {
                    case "recurring_payment_profile_created":
                        //do nothing here
                        break;
                    case "recurring_payment":
                        #region Recurring payment
                        {
                            Guid orderNumberGuid = Guid.Empty;
                            try
                            {
                                orderNumberGuid = new Guid(rp_invoice_id);
                            }
                            catch
                            {
                            }

                            var initialOrder = _orderService.GetOrderByGuid(orderNumberGuid);
                            if (initialOrder != null)
                            {
                                var recurringPayments = _orderService.SearchRecurringPayments(0, 0, initialOrder.Id, null, 0, int.MaxValue);
                                foreach (var rp in recurringPayments)
                                {
                                    switch (newPaymentStatus)
                                    {
                                        case PaymentStatus.Authorized:
                                        case PaymentStatus.Paid:
                                            {
                                                var recurringPaymentHistory = rp.RecurringPaymentHistory;
                                                if (recurringPaymentHistory.Count == 0)
                                                {
                                                    //first payment
                                                    var rph = new RecurringPaymentHistory()
                                                    {
                                                        RecurringPaymentId = rp.Id,
                                                        OrderId = initialOrder.Id,
                                                        CreatedOnUtc = DateTime.UtcNow
                                                    };
                                                    rp.RecurringPaymentHistory.Add(rph);
                                                    _orderService.UpdateRecurringPayment(rp);
                                                }
                                                else
                                                {
                                                    //next payments
                                                    _orderProcessingService.ProcessNextRecurringPayment(rp);
                                                }
                                            }
                                            break;
                                    }
                                }

                                //this.OrderService.InsertOrderNote(newOrder.OrderId, sb.ToString(), DateTime.UtcNow);
                                _logger.Information("YenePay IPN. Recurring info", new NopException(sb.ToString()));
                            }
                            else
                            {
                                _logger.Error("YenePay IPN. Order is not found", new NopException(sb.ToString()));
                            }
                        }
                        #endregion
                        break;
                    default:
                        #region Standard payment
                        {
                            string orderNumber = string.Empty;
                            values.TryGetValue(Constants.FIELDS_MERCHANT_ORDER_ID, out orderNumber);
                            Guid orderNumberGuid = Guid.Empty;
                            try
                            {
                                orderNumberGuid = new Guid(orderNumber);
                            }
                            catch
                            {
                            }

                            var order = _orderService.GetOrderByGuid(orderNumberGuid);
                            if (order != null)
                            {

                                //order note
                                order.OrderNotes.Add(new OrderNote()
                                {
                                    Note = sb.ToString(),
                                    DisplayToCustomer = false,
                                    CreatedOnUtc = DateTime.UtcNow
                                });
                                _orderService.UpdateOrder(order);

                                switch (newPaymentStatus)
                                {
                                    case PaymentStatus.Pending:
                                        {
                                        }
                                        break;
                                    case PaymentStatus.Authorized:
                                        {
                                            if (_orderProcessingService.CanMarkOrderAsAuthorized(order))
                                            {
                                                _orderProcessingService.MarkAsAuthorized(order);
                                            }
                                        }
                                        break;
                                    case PaymentStatus.Paid:
                                        {
                                            if (_orderProcessingService.CanMarkOrderAsPaid(order))
                                            {

                                                order.AuthorizationTransactionId = txn_id;
                                                _orderService.UpdateOrder(order);

                                                _orderProcessingService.MarkOrderAsPaid(order);
                                            }
                                        }
                                        break;
                                    case PaymentStatus.Refunded:
                                        {
                                            if (_orderProcessingService.CanRefundOffline(order))
                                            {
                                                _orderProcessingService.RefundOffline(order);
                                            }
                                        }
                                        break;
                                    case PaymentStatus.Voided:
                                        {
                                            if (_orderProcessingService.CanVoidOffline(order))
                                            {
                                                _orderProcessingService.VoidOffline(order);
                                            }
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }
                            else
                            {
                                _logger.Error("YenePay IPN. Order is not found", new NopException(sb.ToString()));
                            }
                        }
                        #endregion
                        break;
                }
            }
            else
            {
                _logger.Error("YenePay IPN failed.", new NopException(strRequest));
            }

            //nothing should be rendered to visitor
            return Content("");
        }

        public ActionResult CancelOrder(FormCollection form)
        {
            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}