## 2Checkout .NET Integration Tutorial
----------------------------------------

In this tutorial we will walk through integrating the 2Checkout payment method into an existing site built on the .NET MVC3 framework using C#. The source for the example application used in this tutorial can be accessed in this Github repository.

Setting up the Example Application
----------------------------------

We need an existing example application to demonstrate the integration so lets download or clone the 2checkout-dotNet-tutorial application.

```shell
$ git clone https://github.com/2Checkout/2checkout-dotNet-tutorial.git
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

Pass
