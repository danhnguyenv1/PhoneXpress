using PhoneXpressSharedLibrary.Models;
using PhoneXpressSharedLibrary.Responses;

namespace PhoneXpressServer.Repositories
{
    public interface IProduct
    {
        Task<ServiceResponse> AddProduct(Product model);
        Task<List<Product>> GetAllProducts(bool featuredProducts);
    }
}
