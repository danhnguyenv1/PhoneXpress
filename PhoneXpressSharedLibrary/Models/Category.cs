using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PhoneXpressSharedLibrary.Models
{
    public class Category
    {
        public int Id { get; set; }
        [Required]
        public string? Name { get; set; }

        [JsonIgnore]
        public List<Product>? Products { get; set; }



    }
}
