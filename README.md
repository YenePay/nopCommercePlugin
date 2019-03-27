# nopCommercePlugin
This is YenePay's payment plugin for the .NET based nopCommerce shopping cart solution

YenePay is an electronic payment system that allows users to perform payments online or through their mobile phones. This plug in is developed to add this payment system to the nopCommerce shopping cart solution, which is an open source .NET based shopping cart.

# Getting started
In order to add the YenePay plug in for your nopCommerce powered e-commerce site, follow the following steps:

1. Download or clone the contents of this repository
2. Inside the plugin folder, find the appropriate nopCommerce version you are using. Then copy the folder Payments.YenePayCheckout and paste it inside the Plugins folder of your nopCommerce site.
3. Go to the Administrator area of your nopCommerce site and navigate to Configuration > Plugins > Local Plugins. Then scroll down to find the YenePay Checkout plugin and click on the Install button on the right end of the row.
4. When installation completes, click on Edit and check the Is enabled checkbox from the popup that opens. This will enable the plugin and makes it available for customers.
5. Go back to the Local Plugins page and click on Configure button inside the YenePay Checkout plugin. This will open the YenePayCheckout plugin configuration page. Here, check the Use Sandbox checkbox if you want to direct request to our Sandbox platform for testing out your integration. 
Also, make sure you provide your Merchant Code and PDT Identity Token which you will find after registering for a YenePay account. Make sure the PDT validate total, Pass product names and order totals to yenepay and Enable IPN checkboxes are checked.
6. Save your configuration settings and you are done.

