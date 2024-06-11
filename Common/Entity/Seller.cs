using Orleans;
using System;
namespace Common.Entity
{
    /**
     * Seller information is assembled based on two sources:
     * (i) Olist dev public API: https://dev.olist.com/docs/retrieving-seller-informations
     * (ii) Olist public data set: https://www.kaggle.com/datasets/olistbr/brazilian-ecommerce?select=olist_sellers_dataset.csv
     * The additional attributes added as part of this benchmark are:
     * street, complement, order_count
     */
    [GenerateSerializer]
    public class Seller
    {
        [Id(0)]
        public long id { get; set; }

        [Id(1)]
        public string name { get; set; }

        [Id(2)]
        public string company_name { get; set; }

        [Id(3)]
        public string email { get; set; }

        [Id(4)]
        public string phone { get; set; }

        [Id(5)]
        public string mobile_phone { get; set; }

        [Id(6)]
        public string cpf { get; set; }

        [Id(7)]
        public string cnpj { get; set; }

        [Id(8)]
        public string address { get; set; }

        [Id(9)]
        public string complement { get; set; }

        [Id(10)]
        public string city { get; set; }

        [Id(11)]
        public string state { get; set; }

        [Id(12)]
        public string zip_code_prefix { get; set; }

        // statistics. these vakues can be deduced (by aggregate operation) from order items and packages, respectively. not sure we should keep them
        [Id(13)]
        public int order_count { get; set; }
        // public int delivery_count { get; set; }

    }
}

