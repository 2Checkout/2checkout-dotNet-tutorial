using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Site.Models;
using TwoCheckout;

namespace Site.Controllers
{ 
    public class OrdersController : Controller
    {
        private OrderDBContext db = new OrderDBContext();

        //
        // GET: /Orders/

        public ViewResult Index()
        {
            return View(db.Orders.ToList());
        }

        //
        // GET: /Orders/Details/5

        public ViewResult Details(int id)
        {
            Order order = db.Orders.Find(id);
            return View(order);
        }

        //
        // GET: /Orders/Create

        public ActionResult Create()
        {
            return View();
        } 

        //
        // POST: /Orders/Create

        [HttpPost]
        public ActionResult Create(Order order)
        {
            if (ModelState.IsValid)
            {
                db.Orders.Add(order);
                db.SaveChanges();
                return RedirectToAction("Index");  
            }

            return View(order);
        }
        
        //
        // GET: /Orders/Edit/5
 
        public ActionResult Edit(int id)
        {
            Order order = db.Orders.Find(id);
            return View(order);
        }

        //
        // POST: /Orders/Edit/5

        [HttpPost]
        public ActionResult Edit(Order order)
        {
            if (ModelState.IsValid)
            {
                db.Entry(order).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(order);
        }

        //
        // GET: /Orders/Delete/5
 
        public ActionResult Delete(int id)
        {
            Order order = db.Orders.Find(id);
            return View(order);
        }

        //
        // POST: /Orders/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {            
            Order order = db.Orders.Find(id);
            db.Orders.Remove(order);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        //Pass Order and Buyer to 2Checkout
        public ActionResult Checkout()
        {
            //Get Timestamp
            DateTime date = DateTime.Now;
            String time = date.ToString("yyyyMMdd-HHmmss");

            //Create Pending Order
            Order order = new Order();
            order.OrderNumber = "";
            order.CustomerName = "";
            order.Total = "";
            order.DatePlaced = time;
            db.Orders.Add(order);
            db.SaveChanges();

            //Pass to 2Checkout
            var dictionary = new Dictionary<string, string>();
            dictionary.Add("sid", "1817037");
            dictionary.Add("mode", "2CO");
            dictionary.Add("li_0_type", "Product");
            dictionary.Add("li_0_name", "Example Product");
            dictionary.Add("li_0_price", "1.00");
            dictionary.Add("merchant_order_id", order.ID.ToString());
            String PaymentLink = TwocheckoutCharge.Link(dictionary);
            Response.Redirect(PaymentLink);
            return View();
        }

        //Passback from 2Checkout
        public ActionResult Return()
        {
            //Check MD5 Hash Returned
            var dictionary = new Dictionary<string, string>();
            dictionary.Add("sid", Request.Params["sid"]);
            dictionary.Add("order_number", Request.Params["order_number"]);
            dictionary.Add("total", Request.Params["total"]);
            dictionary.Add("key", Request.Params["key"]);
            TwocheckoutResponse result = TwocheckoutReturn.Check(dictionary, "tango");

            if (result.response_code == "Success")
            {
                //Get Timestamp
                DateTime date = DateTime.Now;
                String time = date.ToString("yyyyMMdd-HHmmss");

                //Update Order as Paid
                int ID = Convert.ToInt32(Request.Params["merchant_order_id"]);
                Order order = db.Orders.Find(ID);
                order.OrderNumber = Request.Params["order_number"];
                order.DatePlaced = time;
                order.CustomerName = Request.Params["card_holder_name"];
                order.Total = Request.Params["total"];
                order.Refunded = "";

                db.Entry(order).State = EntityState.Modified;
                db.SaveChanges();

                ViewBag.Message = "Thank you for your Order!";
            }
            else
            {
                ViewBag.Message = "There was a problem with your order. Please contact the site owner to troubleshoot!";
            }
            return View();
        }

        //Handle Fraud Status Changed INS Message
        public ActionResult Notification()
        {
            //Check MD5 Hash
            var dictionary = new Dictionary<string, string>();
            dictionary.Add("vendor_id", Request.Params["vendor_id"]);
            dictionary.Add("sale_id", Request.Params["sale_id"]);
            dictionary.Add("invoice_id", Request.Params["invoice_id"]);
            dictionary.Add("md5_hash", Request.Params["md5_hash"]);
            TwocheckoutResponse result = TwocheckoutNotification.Check(dictionary, "tango");

            //Check to insure MD5 Matches
            if (result.response_code == "Success")
            {
                //Get Order ID
                int ID = Convert.ToInt32(Request.Params["vendor_order_id"]);

                //Check Message Type and Fraud Status
                if (Request.Params["message_type"] == "FRAUD_STATUS_CHANGED" && Request.Params["fraud_status"] == "pass")
                {
                    Order order = db.Orders.Find(ID);
                    order.Refunded = "";
                    db.Entry(order).State = EntityState.Modified;
                    db.SaveChanges();
                }
                else if (Request.Params["message_type"] == "FRAUD_STATUS_CHANGED" && Request.Params["fraud_status"] == "fail") 
                {
                    Order order = db.Orders.Find(ID);
                    order.Refunded = "Yes";
                    db.Entry(order).State = EntityState.Modified;
                    db.SaveChanges();
                }

                ViewBag.Message = "MD5 Hash Matched";
            }
            else
            {
                ViewBag.Message = "MD5 Hash Mismatch";
            }
            return View();
        }

        //Use 2Checkout API to Refund
        public ActionResult Refund(int id)
        {
            Order order = db.Orders.Find(id);
            return View(order);
        }

        //Use 2Checkout API to Refund
        [HttpPost, ActionName("Refund")]
        public ActionResult RefundConfirmed(int id)
        {
            //Find Order
            Order order = db.Orders.Find(id);

            //Set API Credentials
            TwocheckoutConfig.ApiUsername = "APIuser1817037";
            TwocheckoutConfig.ApiPassword = "APIpass1817037";

            //Attempt Refund
            var dictionary = new Dictionary<string, string>();
            dictionary.Add("sale_id", order.OrderNumber);
            dictionary.Add("comment", "Refunded");
            dictionary.Add("category", "5");
            TwocheckoutResponse result = TwocheckoutSale.Refund(dictionary);

            //If Successful, update order.
            if (result.response_code == "OK")
            {
                order.Refunded = "Yes";
                db.Entry(order).State = EntityState.Modified;
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}