using CsvHelper.Configuration;
using Experiments.Workload.PropertyMaps.TypeConverter;
using Experiments.Workload.TableEntries;

namespace Experiments.Workload.PropertyMaps
{
    public class CheckoutParameterAccessMap : ClassMap<CheckoutParameterAccess>
    {
        public CheckoutParameterAccessMap() 
        {
            References<CheckoutParameterMap>(cpa => cpa.ChkParam);
            Map(cpa => cpa.GrainAccesses).TypeConverter<GrainAccessConverter>();
        }

    }
}
