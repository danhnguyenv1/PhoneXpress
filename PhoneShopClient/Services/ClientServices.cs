using System.Net.WebSockets;
using PhoneXpressSharedLibrary.Models;
using PhoneXpressSharedLibrary.Responses;

namespace PhoneXpressClient.Services
{
    public class ClientServices(HttpClient httpClient) : IProductService, ICategoryService
    {
        public const string ProductBaseUrl = "api/product";
        public const string CategoryBaseUrl = "api/category";

        public Action? CategoryAction { get; set; }
        public List<Category> AllCategories { get; set; }
        public Action? ProductAction { get; set; }
        public List<Product> AllProducts { get; set; }
        public List<Product> FeaturedProducts { get; set; }

        //Products
        public async Task<ServiceResponse> AddProduct(Product model)
        {
            var response = await httpClient.PostAsync(ProductBaseUrl, General.GenerateStringContent(General.SerializeObj(model)));

            var result = CheckResponse(response);
            if (!result.Flag)
                return result;

            var apiResponse = await ReadContent(response);
            await ClearAndGetAllProducts();
            return General.DeserializeJsonString<ServiceResponse>(apiResponse);
        }

        private async Task ClearAndGetAllProducts()
        {
            bool featuredProduct = true;
            bool allProduct = true;
            AllProducts = null!;
            FeaturedProducts = null!;
            await GetAllProducts(featuredProduct);
            await GetAllProducts(allProduct);
        }

        public async Task GetAllProducts(bool featuredProducts)
        {
            var products = featuredProducts ? FeaturedProducts : AllProducts;

            if (products is not null)
                return;

            var fetchedProducts = await GetProducts(featuredProducts);

            if (featuredProducts)
                FeaturedProducts = fetchedProducts;
            else
                AllProducts = fetchedProducts;

            ProductAction?.Invoke();
        }

        private async Task<List<Product>> GetProducts(bool featured)
        {
            var response = await httpClient.GetAsync($"{ProductBaseUrl}?featured={featured}");
            var (flag, _) = CheckResponse(response);
            if (!flag) return null!;

            var result = await ReadContent(response);
            return [.. General.DeserializeJsonStringList<Product>(result)];
        }

        //Categorie
        public async Task<ServiceResponse> AddCategory(Category model)
        {
            var response = await httpClient.PostAsync(CategoryBaseUrl, General.GenerateStringContent(General.SerializeObj(model)));

            var result = CheckResponse(response);
            if (!result.Flag)
                return result;

            var apiResponse = await ReadContent(response);
            await ClearAndGetAllCategories();
            return General.DeserializeJsonString<ServiceResponse>(apiResponse);
        }

        public async Task GetAllCategories()
        {
            if (AllCategories is null)
            {
                var response = await httpClient.GetAsync($"{CategoryBaseUrl}");
                var (flag, _) = CheckResponse(response);
                if (!flag) return;

                var result = await ReadContent(response);
                AllCategories = (List<Category>?)General.DeserializeJsonStringList<Category>(result)!;
                CategoryAction?.Invoke();
            }
        }

        private async Task ClearAndGetAllCategories()
        {
            AllCategories = null!;
            await GetAllCategories();
        }

        private ServiceResponse CheckResponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                return new ServiceResponse(false, "Error occured. Try again later...");
            else
                return new ServiceResponse(true, null!);
        }

        private async Task<string> ReadContent(HttpResponseMessage response) => await response.Content.ReadAsStringAsync();
    }
}
