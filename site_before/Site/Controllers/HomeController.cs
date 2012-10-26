using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Site.Models;
using TwoCheckout;

namespace Site.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = "Click the button below to order!";

            return View();
        }

        public ActionResult Checkout()
        {
            var dictionary = new Dictionary<string, string>();
            dictionary.Add("sid", "1817037");
            dictionary.Add("cart_order_id", "Test Cart");
            dictionary.Add("total", "1.00");
            String PaymentLink = TwocheckoutCharge.Link(dictionary);
            Response.Redirect(PaymentLink);
            return View();
        }
    }
}
