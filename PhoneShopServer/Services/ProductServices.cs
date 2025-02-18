using Microsoft.EntityFrameworkCore;
using PhoneXpressServer.Data;
using PhoneXpressSharedLibrary.Models;
using PhoneXpressSharedLibrary.Responses;

namespace PhoneXpressServer.Repositories
{
    public class ProductServices : IProduct
    {
        private readonly AppDbContext appDbContext;
        public ProductServices(AppDbContext appDbContext)
        {
            this.appDbContext = appDbContext;
        }

        public async Task<ServiceResponse> AddProduct(Product model)
        {
            if (model is null) return new ServiceResponse(false, "Model is null");
            var (flag, message) = await CheckName(model.Name!);
            if (flag)
            {
                appDbContext.Products.Add(model);
                await Commit();
                return new ServiceResponse(true, "Saved");

            }
            return new ServiceResponse(flag, message);

        }

        public async Task<List<Product>> GetAllProducts(bool onlyFeatured)
        {
            return onlyFeatured
                ? await appDbContext.Products.Where(p => p.Featured).ToListAsync()
                : await appDbContext.Products.ToListAsync();
        }

        private async Task<ServiceResponse> CheckName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new ServiceResponse(false, "Product name cannot be empty.");

            string normalizedName = name.Trim().ToLower();
            bool exists = await appDbContext.Products
                    .AnyAsync(x => x.Name != null && x.Name.Trim().ToLower() == normalizedName);

            return exists
                ? new ServiceResponse(false, "Product already exists.")
                : new ServiceResponse(true, "Product name is available.");
        }


        private async Task Commit() => await appDbContext.SaveChangesAsync();
    }
}
