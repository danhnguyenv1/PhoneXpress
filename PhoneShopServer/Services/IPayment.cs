using PhoneXpressClient.PrivateModels;
using Stripe.Checkout;

namespace PhoneXpressServer.Services
{
    public interface IPayment
    {
        string CreateCheckoutSession(List<Order> cartItems);
    }
}
