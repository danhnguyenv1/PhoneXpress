using PhoneXpressSharedLibrary.Models;
using PhoneXpressSharedLibrary.Responses;

namespace PhoneXpressClient.Services
{
    public interface IProductService
    {
        Task<ServiceResponse> AddProduct(Product model);
        Task<List<Product>> GetAllProducts(bool featuredProducts);
    }
}
