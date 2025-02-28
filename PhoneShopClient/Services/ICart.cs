using PhoneXpressClient.PrivateModels;
using PhoneXpressSharedLibrary.Models;
using PhoneXpressSharedLibrary.Responses;

namespace PhoneXpressClient.Services
{
    public interface ICart
    {
        public Action? CartAction { get; set; }
        public int CartCount { get; set; }
        Task GetCartCount();
        Task<ServiceResponse> AddToCart(Product model, int updateQuantity = 1);
        Task<List<Order>> MyOrders();
        Task<ServiceResponse> DeleteCart(Order cart);
        bool IsCartLoaderVisible { get; set; }
        Task<string> Checkout(List<Order> cartItems);
    }
}
