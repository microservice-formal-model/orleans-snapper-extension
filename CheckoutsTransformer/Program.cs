using CheckoutsTransformer.CLI;
using CheckoutsTransformer.Model;
using CommandLine;
using Common.Snapper.Core;
using CsvHelper;
using CsvHelper.Configuration;
using Experiments.Workload.PropertyMaps;
using Experiments.Workload.TableEntries;
using Newtonsoft.Json;
using System.Globalization;


var options = Parser.Default.ParseArguments<Options>(args).Value;

var esnapper = Environment.GetEnvironmentVariable("esnapper") ?? throw new Exception("Environment variable esnapper not set.");

var configPath = Path.Combine(esnapper, "CheckoutsTransformer", "Properties", "config.json");

var sourcePath = Path.Combine(esnapper, options.Path);
var targetPath = Path.Combine(esnapper, options.ResultPath);

StreamReader r = new(configPath);
string json = r.ReadToEnd();

var checkedJson = json ?? throw new Exception($"Pleases provide config file in directory: {configPath}");

var entries = JsonConvert.DeserializeObject<List<Entries>>(checkedJson);

var checkedEntries = entries ?? throw new Exception($"config is maleformed in path: {configPath}");

CsvConfiguration config = new(CultureInfo.InvariantCulture)
{
    HasHeaderRecord = true
};


using (var sourceFs = new FileStream(sourcePath,FileMode.Open))
using (var targetFs = new FileStream(targetPath,FileMode.Create))
using (var sourceStreamReader = new StreamReader(sourceFs))
using (var targetStreamWrtier = new StreamWriter(targetFs))
using (var csvWriter = new CsvWriter(targetStreamWrtier,config))
using (var csvReader = new CsvReader(sourceStreamReader,config))
{
    csvWriter.Context.RegisterClassMap<CheckoutParameterAccessMap>();
    csvWriter.WriteHeader<CheckoutParameterAccess>();
    csvWriter.NextRecord();

    csvReader.Context.RegisterClassMap<CheckoutParameterAccessMap>();
    var records = csvReader.GetRecords<CheckoutParameterAccess>();

    foreach ( var record in records)
    {
        var updatedGrainAccess = record
                .GrainAccesses
                .Select(kvp => new Tuple<ActorID, int>(chooseFromEntry(kvp.Key), kvp.Value))
                .ToDictionary(kvp => kvp.Item1, kvp => kvp.Item2);

        record.GrainAccesses = updatedGrainAccess;
        csvWriter.WriteRecord(record);
        csvWriter.NextRecord();
    }

    ActorID chooseFromEntry(ActorID original)
    {
        var newActorId = entries.Select(e =>
        {
            if (e.Entry1 == original.className)
            {
                return new ActorID(original.id, e.Entry2);
            }

            if (e.Entry2 == original.className)
            {
                return new ActorID(original.id, e.Entry1);
            }

            return null;
        })
        .First(aid => aid != null) ?? throw new Exception($"There was a grain access without a specified replacement: {original}");
        return newActorId;
    }

}