## 2Checkout .NET Integration Tutorial
----------------------------------------

In this tutorial we will walk through integrating the 2Checkout payment method into an existing site built on the .NET MVC3 framework using C#. The source for the example application used in this tutorial can be accessed in this Github repository.

Setting up the Example Application
----------------------------------

We need an existing example application to demonstrate the integration so lets download or clone the 2checkout-dotNet-tutorial application.

```shell
$ git clone https://github.com/2checkout/2checkout-dotNet-tutorial.git
```

This repository contains both an example before and after application so that we can follow along with the tutorial using the site\_before app and compare the result with the site\_after app. We can loading up the site\_before application in Visual Studio.

We will need to add a reference.

* [Json.NET 4.5](http://json.codeplex.com/releases/view/92198)

The project also requires that you have MVC3, VS 2010 SP1 and SQL CE.


Let's go ahead and startup the example application by running it in Visual Studio.

![](http://github.com/2checkout/2checkout-dotNet-tutorial/raw/master/img/site-1.png)

You can see that this site provides the user with an order button. It also provides us with a way to manage orders that are placed.

_/Orders/_

![](http://github.com/2checkout/2checkout-dotNet-tutorial/raw/master/img/site-2.png)

In this tutorial, we will integrate the 2Checkout payment method so that the user can purchase and we can validate the MD5 hash returned by 2Checkout. We will also setup a listener that can be used to update the order on the Fraud Status Changed notification send by 2Checkout. To provide an example API usage, we will add the ability to refund the order.


Adding the 2Checkout dotNet Library
------------------------------------
The 2Checkout Java library provides us with a simple bindings to the API, INS and Checkout process so that we can integrate each feature with only a few lines of code. We can download or clone the library at [https://github.com/2checkout/2checkout-dotnet](https://github.com/2checkout/2checkout-dotnet).

Including the library is as easy as downloading the dll and adding it as a reference.

Setup Checkout
-----------
To pass the buyer and the sale to 2Checkout we will need to link our button to the 2Checkout checkout page. Lets go ahead and setup a new method in our OrdersController that will utilize the TwocheckoutCharge method to build our payment link.

_Controllers/OrdersController.cs_
```csharp
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
```

Lets take a look at what we did here. First we grab the current timestamp and create our pending order.

```csharp
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
```

Then we use the `TwocheckoutCharge.Link()` method to create our payment link so we can redirect the buyer to 2Checkout.

```csharp
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
```

Passback
--------

When the order is completed, 2Checkout will return the buyer and the sale parameters to the URL that we specify as the approved URL in our account. This URL can also be passed in dynamically for each sale using the `x_receipt_link_url` parameter.

Lets create the return URL by adding a return method. We will also need to setup our return method to handle the passback and display the confirmation page.

_Controllers/OrdersController.cs_
```csharp
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
```

First we need to validate the MD5 Hash returned by 2Checkout, so we grab the `sid`, `total`, `order_number` and `key` from the parameters returned by 2Checkout and assign them to the `dictionary` Dictionary.

```csharp
    var dictionary = new Dictionary<string, string>();
    dictionary.Add("sid", Request.Params["sid"]);
    dictionary.Add("order_number", Request.Params["order_number"]);
    dictionary.Add("total", Request.Params["total"]);
    dictionary.Add("key", Request.Params["key"]);
```

Then we pass the Dictionary to the `TwocheckoutReturn.Check()` binding as the first argument and our secret word as the second argument. This method will return the string "Success" if the hash matches.

```csharp
    var result = TwocheckoutReturn.Check(dictionary, "tango");
```

If the hash matches, we update the order and display the return page. If the hash does not match, we display an error message.

```csharp
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
```

We also need to add the corresponding view.

_Views/Orders/Return.cshtml
```csharp
@{
    ViewBag.Title = "Return";
}

<h2>@ViewBag.Message</h2>
```

Now we can setup our return method, enter our secret word and provide the approved URL path under the Site Management page in our 2Checkout admin.

**Site Management Page**

![](http://github.com/2checkout/2checkout-dotNet-tutorial/raw/master/img/site-3.png)

**Lets try it out with a live sale.**

![](http://github.com/2checkout/2checkout-dotNet-tutorial/raw/master/img/site-1.png)

**Enter in our billing information and submit the payment.**

![](http://github.com/2checkout/2checkout-dotNet-tutorial/raw/master/img/site-4.png)

**Return Page.**

![](http://github.com/2checkout/2checkout-dotNet-tutorial/raw/master/img/site-5.png)

Notifications
-------------

2Checkout will send notifications to our application under the following circumstances.

* Order Created
* Fraud Status Changed
* Shipping Status Changed
* Invoice Status Changed
* Refund Issued
* Recurring Installment Success
* Recurring Installment Failed
* Recurring Stopped
* Recurring Complete
* Recurring Restarted

For our application, we are interested in the Fraud Status Changed message so that we can mark the order as refunded is it fails fraud review. To handle this message, we will create a new notification route.

_Controllers/OrdersController.cs_
```csharp
    //Handle Fraud Status Changed INS Message
    public ActionResult Notification()
    {
        //Check MD5 Hash
        var dictionary = new Dictionary<string, string>();
        dictionary.Add("vendor_id", Request.Params["vendor_id"]);
        dictionary.Add("sale_id", Request.Params["sale_id"]);
        dictionary.Add("invoice_id", Request.Params["invoice_id"]);
        dictionary.Add("md5_hash", Request.Params["md5_hash"]);
        var result = TwocheckoutNotification.Check(dictionary, "tango");

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
```

We grab the message parameters and assign the `vendor_id`, `invoice_id`, `sale_id` and `md5_hash` values to a new Dictionary.

```csharp
    var dictionary = new Dictionary<string, string>();
    dictionary.Add("vendor_id", Request.Params["vendor_id"]);
    dictionary.Add("sale_id", Request.Params["sale_id"]);
    dictionary.Add("invoice_id", Request.Params["invoice_id"]);
    dictionary.Add("md5_hash", Request.Params["md5_hash"]);
```

Then we pass the Dictionary to the `TwocheckoutNotification.Check()` binding as the first argument and our secret word as the second argument. This method will return a string "Success" if the hash matches.

```csharp
	var result = TwocheckoutNotification.Check(dictionary, "tango");
```

If the hash matches, we can preform actions based on the `message_type` parameter value.

For the Fraud Status Changed message, we will also check the value of the `fraud_status` parameter and only send the product if it equals 'pass'.

```csharp
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
```

We also need to add the corresponding view.

_Views/Orders/Notification.cshtml
```csharp
@{
    ViewBag.Title = "Notification";
}

<h2>@ViewBag.Message</h2>
```


Now we can setup our Notification URL path and enable the Fraud Status Changed message under the Notifications page in our 2Checkout admin.

![](http://github.com/2checkout/2checkout-dotNet-tutorial/raw/master/img/site-6.png)

Lets test our notification function. Now there are a couple ways to go about this. You can wait for the notifications to come on a live sale, or just head over to the [INS testing tool](http://developers.2checkout.com/inss) and test the messages right now. Remember the MD5 hash must match so for easy testing, you must compute the hash based on like below:

`UPPERCASE(MD5_ENCRYPTED(sale\_id + vendor\_id + invoice\_id + Secret Word))`

You can just use an [online MD5 Hash generator](https://www.google.com/webhp?q=md5+generator) and convert it to uppercase.

API
-------------
To provide an example on using the API, we will add a refund method to the _/Orders_ index page. To accomplish this we will need to add a Refund method to our Orders Controller and create the corresponding view.

_Controllers/OrdersController.cs_
```csharp
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
```

We grab the order ID so that we can find the order in our Refund method.

```csharp
	Order order = db.Orders.Find(id);
```

We set our API username and password.

```csharp
	TwocheckoutConfig.ApiUsername = "APIuser1817037";
	TwocheckoutConfig.ApiPassword = "APIpass1817037";
```

We create a new Dictionary and add the orders sale_id, a refund comment and a category as required by 2Checkout to complete the refund action.

```csharp
	var dictionary = new Dictionary<string, string>();
	dictionary.Add("sale_id", order.OrderNumber);
	dictionary.Add("comment", "Refunded");
	dictionary.Add("category", "5");
```

Now we use the `TwocheckoutSale.Refund` method to attempt the refund.

```csharp
	var result = TwocheckoutSale.Refund(dictionary);
```

If the refund is successful, we will update the order status, then we return the user to the Order index.

```csharp
    if (result.response_code == "OK")
    {
        order.Refunded = "Yes";
        db.Entry(order).State = EntityState.Modified;
        db.SaveChanges();
    }
```

Conclusion
----------

Now our application is fully integrated! Our buyers can pay for the product, we update the sale based on the passback and INS Fraud Status Changed message, and we can refund the sales through the API.
