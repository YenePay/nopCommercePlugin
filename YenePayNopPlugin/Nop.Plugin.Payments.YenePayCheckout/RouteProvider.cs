using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.YenePayChekout
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            //PDT
            routes.MapRoute("Plugin.Payments.YenePayCheckout.PDTHandler",
                 "Plugins/PaymentYenePayChekout/PDTHandler",
                 new { controller = "PaymentYenePayCheckout", action = "PDTHandler" },
                 new[] { "Nop.Plugin.Payments.YenePayCheckout.Controllers" }
            );
            //IPN
            routes.MapRoute("Plugin.Payments.YenePayCheckout.IPNHandler",
                 "Plugins/PaymentYenePayChekout/IPNHandler",
                 new { controller = "PaymentYenePayCheckout", action = "IPNHandler" },
                 new[] { "Nop.Plugin.Payments.YenePayCheckout.Controllers" }
            );
            //Cancel
            routes.MapRoute("Plugin.Payments.YenePayCheckout.CancelOrder",
                 "Plugins/PaymentYenePayChekout/CancelOrder",
                 new { controller = "PaymentYenePayCheckout", action = "CancelOrder" },
                 new[] { "Nop.Plugin.Payments.YenePayCheckout.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
