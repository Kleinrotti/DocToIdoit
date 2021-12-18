using System;

namespace DocToIdoit
{
    /// <summary>
    /// Represents a product which defines properties of a delivery note.
    /// </summary>
    internal class Product
    {
        public string DeliveryNote { get; set; }
        public string ProductName { get; set; }
        public DateTime OrderDate { get; set; }
        public string SerialNumer { get; set; }
        public string Type { get; set; }
        public int Template { get; set; }
        public string IdoitPrefix { get; set; }
    }
}