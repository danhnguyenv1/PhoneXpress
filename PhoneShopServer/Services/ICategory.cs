using PhoneXpressSharedLibrary.Models;
using PhoneXpressSharedLibrary.Responses;

namespace PhoneXpressServer.Repositories
{
    public interface ICategory
    {
        Task<ServiceResponse> AddCategory(Category model);
        Task<List<Category>> GetAllCategories();
    }
}
