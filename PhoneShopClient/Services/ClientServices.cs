using Blazored.LocalStorage;
using PhoneXpressClient.Authentication;
using PhoneXpressClient.PrivateModels;
using PhoneXpressSharedLibrary.Dtos;
using PhoneXpressSharedLibrary.Models;
using PhoneXpressSharedLibrary.Responses;

namespace PhoneXpressClient.Services
{
    public class ClientServices(HttpClient httpClient,
        AuthenticationService authenticationService,
        ILocalStorageService localStorageService) : IProductService, ICategoryService, IUserAccountService, ICart
    {
        public const string ProductBaseUrl = "api/product";
        public const string CategoryBaseUrl = "api/category";
        public const string AuthenticationBaseUrl = "api/account";

        public Action? CategoryAction { get; set; }
        public List<Category> AllCategories { get; set; }
        public Action? ProductAction { get; set; }
        public List<Product> AllProducts { get; set; }
        public List<Product> FeaturedProducts { get; set; }
        public List<Product> ProductsByCategory { get; set; }
        public Action? CartAction { get; set; }
        public int CartCount { get; set; }
        public bool IsCartLoaderVisible { get; set; }

        //Products
        public async Task<ServiceResponse> AddProduct(Product model)
        {
            await authenticationService.GetUserDetails();
            var privateHttpClient = await authenticationService.AddHeaderToHttpClient();
            var response = await privateHttpClient.PostAsync(ProductBaseUrl, General.GenerateStringContent(General.SerializeObj(model)));

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
            Random random = new();
            return FeaturedProducts.ElementAt(random.Next(0, FeaturedProducts.Count));
        }

        //Categorie
        public async Task<ServiceResponse> AddCategory(Category model)
        {
            await authenticationService.GetUserDetails();
            var privateHttpClient = await authenticationService.AddHeaderToHttpClient();
            var response = await privateHttpClient.PostAsync(CategoryBaseUrl, General.GenerateStringContent(General.SerializeObj(model)));

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

        private async Task<string> ReadContent(HttpResponseMessage response) =>
            await response.Content.ReadAsStringAsync();


        //Account/Authentication Service
        public async Task<ServiceResponse> Register(UserDTO model)
        {
            var response = await httpClient.PostAsync($"{AuthenticationBaseUrl}/register",
                General.GenerateStringContent(General.SerializeObj(model)));
            var result = CheckResponse(response);
            if (!result.Flag)
                return result;

            var apiResponse = await ReadContent(response);
            return General.DeserializeJsonString<ServiceResponse>(apiResponse);
        }

        public async Task<LoginResponse> Login(LoginDTO model)
        {

            var response = await httpClient.PostAsync($"{AuthenticationBaseUrl}/login", General.GenerateStringContent(General.SerializeObj(model)));
            if (!response.IsSuccessStatusCode)
                return new LoginResponse(false, "Error occured", null!, null!);

            var apiResponse = await ReadContent(response);
            return General.DeserializeJsonString<LoginResponse>(apiResponse);
        }

        //Cart Service

        public async Task GetCartCount()
        {
            string cartString = await GetCartFromLocalStorage();
            if (string.IsNullOrEmpty(cartString))
                CartCount = 0;
            else
                CartCount = General.DeserializeJsonStringList<StorageCart>(cartString).Count;
            CartAction?.Invoke();
        }

        public async Task<ServiceResponse> AddToCart(Product model, int updateQuantity = 1)
        {

            string message = string.Empty;
            var MyCart = new List<StorageCart>();
            var getCartFromStrorage = await GetCartFromLocalStorage();
            if (!string.IsNullOrEmpty(getCartFromStrorage))
            {
                MyCart = (List<StorageCart>)General.DeserializeJsonStringList<StorageCart>(getCartFromStrorage);
                var checkIfAddedAlready = MyCart.FirstOrDefault(_ => _.ProductId == model.Id);
                if (checkIfAddedAlready is null)
                {
                    MyCart.Add(new StorageCart() { ProductId = model.Id, Quantity = 1 });
                    message = "Product Added to Cart";
                }
                else
                {
                    var updatedProduct = new StorageCart() { Quantity = updateQuantity, ProductId = model.Id };
                    MyCart.Remove(checkIfAddedAlready!);
                    MyCart.Add(updatedProduct);
                    message = "Product Updated";
                }
            }
            else
            {
                MyCart.Add(new StorageCart() { ProductId = model.Id, Quantity = 1 });
                message = "Product Added to Cart";
            }
            await RemoveCartFromLocalStorage();
            await SetCartToLocalStorage(General.SerializeObj(MyCart));
            await GetCartCount();
            return new ServiceResponse(true, message);
        }

        public async Task<List<Order>> MyOrders()
        {
            IsCartLoaderVisible = true;
            var cartList = new List<Order>();
            string myCartString = await GetCartFromLocalStorage();
            if (string.IsNullOrEmpty(myCartString)) return null!;

            var myCartList = General.DeserializeJsonStringList<StorageCart>(myCartString);
            await GetAllProducts(false);
            foreach (var cartItem in myCartList)
            {
                var product = AllProducts.FirstOrDefault(_ => _.Id == cartItem.ProductId);
                cartList.Add(new Order()
                {
                    Id = product!.Id,
                    Name = product.Name,
                    Quantity = cartItem.Quantity,
                    Price = product.Price,
                    Image = product.Base64Img,
                });
            }
            IsCartLoaderVisible = false;
            await GetCartCount();
            return cartList;
        }

        public async Task<ServiceResponse> DeleteCart(Order cart)
        {
            var myCartList = General.DeserializeJsonStringList<StorageCart>(await GetCartFromLocalStorage()); if (myCartList is null)
                return new ServiceResponse(false, "Product not found");

            myCartList.Remove(myCartList.FirstOrDefault(_ => _.ProductId == cart.Id)!);
            await RemoveCartFromLocalStorage();
            await SetCartToLocalStorage(General.SerializeObj(myCartList));
            await GetCartCount();
            return new ServiceResponse(true, "Product removed successfully");
        }

        private async Task<string> GetCartFromLocalStorage() => await localStorageService.GetItemAsStringAsync("cart");

        private async Task SetCartToLocalStorage(string cart) => await localStorageService.SetItemAsStringAsync("cart", cart);

        private async Task RemoveCartFromLocalStorage() => await localStorageService.RemoveItemAsync("cart");


        public async Task<string> Checkout(List<Order> cartItems)
        {
            var response = await httpClient.PostAsync("api/payment/checkout",
                General.GenerateStringContent(General.SerializeObj(cartItems)));

            var url = await response.Content.ReadAsStringAsync();
            return url;
        }
    }
}
