namespace Api.Models
{
    public class CustomerAPI
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CustomerType { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }
}
