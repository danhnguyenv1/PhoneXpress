using System.Text.Json;
using System.Text.Json.Serialization;
using PhoneXpressSharedLibrary.Models;
using PhoneXpressSharedLibrary.Responses;

namespace PhoneXpressClient.Services
{
    public class ClientServices(HttpClient httpClient) : IProductService
    {
        private const string BaseUrl = "api/product";
        private static string SerializeObj(object modelOject) => JsonSerializer.Serialize(modelOject, JsonOptions());
        private static T DeserializeJsonString<T>(string jsonString) => JsonSerializer.Deserialize<T>(jsonString, JsonOptions())!;
        private static StringContent GenerateStringContent(string serializedObj) => new(serializedObj, System.Text.Encoding.UTF8, "application/json");
        private static IList<T> DeserializeJsonStringList<T>(string jsonString) => JsonSerializer.Deserialize<IList<T>>(jsonString, JsonOptions())!;
        private static JsonSerializerOptions JsonOptions()
        {
            return new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip
            };
        }

        public async Task<ServiceResponse> AddProduct(Product model)
        {
            var response = await httpClient.PostAsync(BaseUrl, GenerateStringContent(SerializeObj(model)));

            //Read response
            if (!response.IsSuccessStatusCode)
                return new ServiceResponse(false, "Error occured. Try again later...");

            var apiResponse = await response.Content.ReadAsStringAsync();
            return DeserializeJsonString<ServiceResponse>(apiResponse);
        }

        public async Task<List<Product>> GetAllProducts(bool featuredProducts)
        {
            var results = await httpClient.GetAsync($"{BaseUrl}?featured={featuredProducts}");
            if (!results.IsSuccessStatusCode) return null!;

            var result = await results.Content.ReadAsStringAsync();
            return [.. DeserializeJsonStringList<Product>(result)];

        }
    }
}
