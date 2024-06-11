using Common.Entity;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiments.Workload.PropertyMaps
{
    public class CustomerCheckoutMap : ClassMap<CustomerCheckout>
    {
        public CustomerCheckoutMap() 
        {
            Map(c => c.CustomerId);
            Map(c => c.FirstName);
            Map(c => c.LastName);
            Map(c => c.Street);
            Map(c => c.CardBrand);
            Map(c => c.CardExpiration);
            Map(c => c.CardHolderName);
            Map(c => c.CardNumber);
            Map(c => c.CardSecurityNumber);
            Map(c => c.City);
            Map(c => c.Installments);
            Map(c => c.Complement);
            Map(c => c.PaymentType);
            Map(c => c.State);
            Map(c => c.ZipCode);
        }

    }
}
