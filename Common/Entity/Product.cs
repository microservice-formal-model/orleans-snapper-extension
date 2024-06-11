using Orleans;
using System;
namespace Common.Entity
{
    /**
     * Product is based on info found in:
     * (i) https://dev.olist.com/docs/creating-a-product
     * (ii) Olist data set, order_items file
     * It is worthy to note that the attributes gtin, stock, package mesasures, photo, and tags are not considered
     * Besides, only one category is chosen as found in olist public data set
     */
    [GenerateSerializer]
    public class Product : IEquatable<Product>
	{
        // PK
        [Id(0)]
        public long product_id { get; set; }

        // FK
        [Id(1)]
        public long seller_id { get; set; }

        [Id(2)]
        public string name { get; set; }

        [Id(3)]
        public string sku { get; set; }

        [Id(4)]
        public string category_name { get; set; }

        [Id(5)]
        public string description { get; set; }

        [Id(6)]
        public decimal price { get; set; }

        [Id(11)]
        public decimal freight_value { get; set; }

        // "2017-10-06T01:40:58.172415Z"
        [Id(7)]
        public string created_at { get; set; }
        [Id(8)]
        public string updated_at { get; set; }

        [Id(9)]
        public bool active { get; set; }

        // https://dev.olist.com/docs/products
        // approved by default
        [Id(10)]
        public string status { get; set; }

        public bool Equals(Product other)
        {
            return product_id == other.product_id &&
                seller_id == other.seller_id &&
                name == other.name &&
                sku == other.sku &&
                category_name == other.category_name &&
                description == other.description &&
                price == other.price &&
                freight_value == other.freight_value &&
                created_at == other.created_at &&
                updated_at == other.updated_at &&
                active == other.active &&
                status == other.status;
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(product_id);
            hash.Add(seller_id);
            hash.Add(name);
            hash.Add(sku);
            hash.Add(category_name);
            hash.Add(description);
            hash.Add(price);
            hash.Add(freight_value);
            hash.Add(created_at);
            hash.Add(updated_at);
            hash.Add(active);
            hash.Add(status);
            return hash.ToHashCode();
        }
    }
}

