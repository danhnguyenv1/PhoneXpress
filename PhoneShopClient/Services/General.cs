using System.Text.Json.Serialization;
using System.Text.Json;

namespace PhoneXpressClient.Services
{
    public static class General
    {
        public static string SerializeObj(object modelOject) => JsonSerializer.Serialize(modelOject, JsonOptions());
        public static T DeserializeJsonString<T>(string jsonString) => JsonSerializer.Deserialize<T>(jsonString, JsonOptions())!;
        public static StringContent GenerateStringContent(string serializedObj) => new(serializedObj, System.Text.Encoding.UTF8, "application/json");
        public static IList<T> DeserializeJsonStringList<T>(string jsonString) => JsonSerializer.Deserialize<IList<T>>(jsonString, JsonOptions())!;
        public static JsonSerializerOptions JsonOptions()
        {
            return new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip
            };
        }

        public static string GetDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return "No description available.";
            }

            string appendDots = "...";
            int maxLength = 100;
            return description.Length > maxLength ? description.Substring(0, maxLength) + appendDots : description;
        }
    }
}
