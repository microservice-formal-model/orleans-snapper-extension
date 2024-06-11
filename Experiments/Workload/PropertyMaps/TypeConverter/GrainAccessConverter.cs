using Common.Snapper.Core;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Experiments.Workload.PropertyMaps.TypeConverter
{
    public class GrainAccessConverter : DefaultTypeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if(text == null) return null;
            var splittedStringKVPs = text.Split(';').Except(new List<string>() { ""});
            Dictionary<ActorID, int> result = new();
            foreach(string kvp in splittedStringKVPs)
            {
                var kvpMembers = kvp.Split(":");
                result.Add(new ActorID(id: long.Parse(kvpMembers[1]), kvpMembers[0]), int.Parse(kvpMembers[2]));
            }
            return result;
        }

        public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        {
            string result = "";
            if (value is Dictionary<ActorID, int> dict)
            {
                foreach (KeyValuePair<ActorID, int> kvp in dict)
                {
                    result += $"{kvp.Key.className}:{kvp.Key.id}:{kvp.Value};";
                }
                return result;
            }
            return null;
        }
    }
}
