using PhoneXpressClient.PrivateModels;
using Stripe;
using Stripe.Checkout;

namespace PhoneXpressServer.Services
{
    public class PaymentService : IPayment
    {
        private readonly IConfiguration _configuration;
        public PaymentService(IConfiguration _configuration)
        {
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        public string CreateCheckoutSession(List<Order> cartItems)
        {

            if (cartItems is null)
                return null;
            var lineItems = new List<SessionLineItemOptions>();
            cartItems.ForEach(ci => lineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)(ci.Price * 100),
                    Currency = "usd",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = ci.Name,
                        Description = ci.Id.ToString(),

                    }
                },
                Quantity = ci.Quantity
            }));
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = ["card"],
                LineItems = lineItems,
                Mode = "payment",
                SuccessUrl = "https://localhost:7249/order-success",
                CancelUrl = "https://localhost:7249",
            };

            var service = new SessionService();
            try
            {
                var session = service.Create(options);
                return session.Url;
            }
            catch (StripeException ex)
            {
                Console.WriteLine($"Stripe Error: {ex.Message}");
                return null;
            }
        }
    }
}
