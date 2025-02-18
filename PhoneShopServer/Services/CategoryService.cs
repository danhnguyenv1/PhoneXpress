using Microsoft.EntityFrameworkCore;
using PhoneXpressServer.Data;
using PhoneXpressSharedLibrary.Models;
using PhoneXpressSharedLibrary.Responses;

namespace PhoneXpressServer.Repositories
{
    public class CategoryService(AppDbContext appDbContext) : ICategory
    {
        private readonly AppDbContext appDbContext = appDbContext;

        public async Task<ServiceResponse> AddCategory(Category model)
        {
            if (model is null) return new ServiceResponse(false, "Model is null");
            var (flag, message) = await CheckName(model.Name!);
            if (flag)
            {
                appDbContext.Categories.Add(model);
                await Commit();
                return new ServiceResponse(true, "Category Saved");

            }
            return new ServiceResponse(flag, message);
        }

        public async Task<List<Category>> GetAllCategories() => await appDbContext.Categories.ToListAsync();


        private async Task<ServiceResponse> CheckName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new ServiceResponse(false, "Category name cannot be empty.");

            string normalizedName = name.Trim().ToLower();
            bool exists = await appDbContext.Categories
                    .AnyAsync(x => x.Name != null && x.Name.Trim().ToLower() == normalizedName);

            return exists
                ? new ServiceResponse(false, "Category already exists.")
                : new ServiceResponse(true, "Category name is available.");
        }

        private async Task Commit() => await appDbContext.SaveChangesAsync();
    }
}
