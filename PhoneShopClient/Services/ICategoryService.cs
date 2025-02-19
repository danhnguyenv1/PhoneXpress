using PhoneXpressSharedLibrary.Models;
using PhoneXpressSharedLibrary.Responses;

namespace PhoneXpressClient.Services
{
    public interface ICategoryService
    {
        Action? CategoryAction { get; set; }
        Task<ServiceResponse> AddCategory(Category model);

        Task GetAllCategories();

        List<Category> AllCategories { get; set; }
    }
}
