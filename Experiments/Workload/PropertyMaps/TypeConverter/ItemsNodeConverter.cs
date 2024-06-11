using Common.Entity;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Text;

namespace Experiments.Workload.PropertyMaps.TypeConverter
{
    public class ItemsNodeConverter : DefaultTypeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            //This method consists of 2 steps. (1) decode the Base64 encoded string, (2) parse the content back to cart items

            //(1) decode the Base64 encoded string
            if(text == null) return null;
            var toParse = text;
            //var base64EncodedBytes = Convert.FromBase64String(text);

            //if(base64EncodedBytes == null) return null;

            //var toParse = Encoding.UTF8.GetString(base64EncodedBytes);

            //if(toParse == null) return null;
            // (2) parse the content back to cart items
            return ParseCartItems(toParse);
        }

        private static object? ParseCartItems(string cartItems)
        {
            //Console.WriteLine(cartItems);
            var items = new List<CartItem>();
            var splittedStringItems = cartItems.Split(';').Except(new List<string>() {""});
            foreach (string stringItem in splittedStringItems) 
            {
                var stringItemComponents = stringItem.Split(':');
                var item = new CartItem();

                item.ProductId = long.Parse(stringItemComponents[0]);
                item.ProductName = stringItemComponents[1];
                item.Quantity = int.Parse(stringItemComponents[2]);
                item.UnitPrice = decimal.Parse(stringItemComponents[3]);
                item.FreightValue = decimal.Parse(stringItemComponents[4]);
                item.SellerId = long.Parse(stringItemComponents[5]);

                items.Add(item);
            }
            if (items.Count == 0) return null;
            return items;
        }

        public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        {
            if (value is List<CartItem> castItems)
            {
                return string.Join(" ", castItems.Select(ItemToCSVString));
               // var plainTextBytes = Encoding.UTF8.GetBytes(string.Join(" ", castItems.Select(ItemToCSVString)));
               // return Convert.ToBase64String(plainTextBytes);
            }
            return null;
        }

        private string ItemToCSVString(CartItem c)
        {
            return $"{c.ProductId}:{c.ProductName}:{c.Quantity}:{c.UnitPrice}:{c.FreightValue}:{c.SellerId};";
        }
    }
}
