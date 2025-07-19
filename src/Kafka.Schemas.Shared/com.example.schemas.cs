namespace com.example.schemas
{
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class StandardProduct : Product
    {
        public string StandardProductFeatures { get; set; }
    }
    public class PremiumProduct : Product
    {
        public string PremiumProductFeatures { get; set; }
    }

    public class Contact
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    // Base OrderMessage class
    public class OrderMessage
    {
        public Customer CustomerInfo { get; set; }
        public Contact ContactInfo { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }

    // Specific message types for the union, inheriting from OrderMessage
    public class StandardOrderMessage : OrderMessage
    {
        public StandardProduct ProductInfo { get; set; }
        public string StandardFeatures { get; set; }
    }

    public class PremiumOrderMessage : OrderMessage
    {
        public PremiumProduct ProductInfo { get; set; }
        public int PremiumDiscountPercentage { get; set; }
        public string DedicatedSupportContact { get; set; }
    }
}
