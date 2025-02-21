using System.Net.WebSockets;
using PhoneXpressSharedLibrary.Models;
using PhoneXpressSharedLibrary.Responses;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        public List<Product> ProductsByCategory { get; set; }

        //Products
        public async Task<ServiceResponse> AddProduct(Product model)
        {
            var response = await httpClient.PostAsync(ProductBaseUrl, General.GenerateStringContent(General.SerializeObj(model)));

            var result = CheckResponse(response);
            if (!result.Flag)
                return result;

            var apiResponse = await ReadContent(response);
            var data = General.DeserializeJsonString<ServiceResponse>(apiResponse);
            if (!data.Flag) return data;
            await ClearAndGetAllProducts();
            return data;
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

        public async Task GetProductsByCategory(int categoryId)
        {
            bool featued = false;
            await GetAllProducts(featued);
            ProductsByCategory = AllProducts.Where(_ => _.CategoryId == categoryId).ToList();
            ProductAction?.Invoke();
        }

        //Get Random Product fo Banner
        public Product GetRandomProduct()
        {
            if (FeaturedProducts is null) return null!;
            Random RandomNumbers = new();
            int minimumNumber = FeaturedProducts.Min(_ => _.Id);
            int maximumNumber = FeaturedProducts.Max(_ => _.Id) + 1;
            int result = RandomNumbers.Next(minimumNumber, maximumNumber);
            return FeaturedProducts.FirstOrDefault(_ => _.Id == result)!;
        }

        //Categorie
        public async Task<ServiceResponse> AddCategory(Category model)
        {
            var response = await httpClient.PostAsync(CategoryBaseUrl, General.GenerateStringContent(General.SerializeObj(model)));

            var result = CheckResponse(response);
            if (!result.Flag)
                return result;

            var apiResponse = await ReadContent(response);

            var data = General.DeserializeJsonString<ServiceResponse>(apiResponse);
            if (!data.Flag) return data;
            await ClearAndGetAllCategories();
            return data;

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
