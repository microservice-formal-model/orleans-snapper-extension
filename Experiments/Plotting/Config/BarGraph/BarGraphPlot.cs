using Experiments.Execution.Result;
using Experiments.ExperimentsModel;
using Experiments.Plotting.Json;

namespace Experiments.Plotting.Config.BarGraph
{
    public class BarGraphPlot : APlot
    {
        public required XAxis XAxis { get; set; }

        /// <summary>
        /// Reference for this Plot is https://swharden.com/csdv/plotting-free/oxyplot/.
        /// </summary>
        /// <param name="experiments"></param>
        public override AJsonGraph GeneratePlot(List<IResult> experimentResults)
        {
            //Each Series becomes their own y-points, we can calculate the amount of series by analyzing
            //for which experiment we have which benchmark and collect the different amounts of benchmarks
            //
            //This list also maps the series to their points in the array seriesYValues
            List<BenchmarkType> Series = new();
            //Define the properties of each Series

            foreach (IResult result in experimentResults)
            {
                if (Ids.Contains(result.ExperimentID) && !Series.Contains(result.BenchmarkType))
                {
                    Series.Add(result.BenchmarkType);
                }
            }

            //Array that has two dimension, for each series, we have the amount of x-Axis points
            //to store a y point per series
            //
            //seriesYValues[Nr benchmarktype in Series list, xAxis Point]
            double[,] seriesYValues = new double[XAxis.Points.Count, Series.Count];

            //Initialize all values with 0 so that missing series values are later filled with 0
            for (int i = 0; i < XAxis.Points.Count; i++)
            {
                for (int j = 0; j < Series.Count; j++)
                {
                    seriesYValues[i, j] = 0;
                }
            }

            //iterate over all x-Axis Points and fill all yValue points for each series
            for (int pointCounter = 0; pointCounter < XAxis.Points.Count; pointCounter++)
            {
                //Ids of experiments that are considered under the current point
                var expIds = XAxis.Points[pointCounter].Ids;
                //Find all experiment results for each id and map them to the right series
                foreach (int expId in expIds)
                {
                    //Find all experiment result that matches the yaxistype and the correct experId
                    IResult resultsWithIdAndBenchmark = experimentResults.FindLast(e => e.ExperimentID == expId &&
                      MatchTypes(YAxisType, e)) ??
                        throw new ArgumentOutOfRangeException($"Experiment with id {expId} can not be found for YAxis Type {YAxisType}.");
                    //Set the according correct series with the result we know the number by getting the
                    //index in the series list with according benchmark type
                    var indexToEnter = Series.IndexOf(resultsWithIdAndBenchmark.BenchmarkType);
                    seriesYValues[pointCounter, indexToEnter] = GetResult(resultsWithIdAndBenchmark);
                }
            }
            //[point 0 -> series A, series B, Series C]
            //[point 1 -> series A, series B, Series C]
            //We want to transform this to
            //[series A -> point 0, point 1]
            //[series B -> point 0, point 1]
            //[series C -> point 0, point 1]
            // 0 -> 0, 4.5, 1
            // 1 -> 0, 0, 9
            //afterwards
            // 0 -> 0, 0
            // 1 -> 4.5, 0
            // 2 -> 1, 9
            //That means that if before a number [i,j] needs to be mapped to [j,i]
            double[,] invertedToSeriesResults = new double[Series.Count, XAxis.Points.Count];
            //Swap the values to represent series to xValues
            for (int i = 0; i < XAxis.Points.Count; i++)
            {
                for (int j = 0; j < Series.Count; j++)
                {
                    invertedToSeriesResults[j, i] = seriesYValues[i, j];
                }
            }

            List<Series> series = new();
            Dictionary<BenchmarkType, string> GetColor = new()
                {
                    {BenchmarkType.EXTENDED, "#e4bcc1"},
                    {BenchmarkType.SNAPPER, "#cbbce4"},
                    {BenchmarkType.EVENTUAL, "#d6e5bc" },
                    {BenchmarkType.TRANSACTIONS, "#bce4df" }
                };
            for (int i = 0; i < invertedToSeriesResults.GetLength(0); i++)
            {
                var yValues = Enumerable.Range(0, invertedToSeriesResults.GetLength(1))
                        .Select(x => invertedToSeriesResults[i, x])
                        .ToList();
         
                series.Add(new Series()
                {
                    YValues = yValues,
                    Name = Series[i].ToString(),
                    Text = yValues.Select(y => y.ToString()).ToList(),
                    X = XAxis.Points.Select(x => x.Label).ToList(),
                    Color = GetColor[Series[i]]
                });
            }

            Dictionary<YAxisType, string> YAxisTitle = new()
            {
                {YAxisType.THROUGHPUT, "Tr/s" },
                {YAxisType.LATENCY, "ms" }
            };

            var esnapper = Environment.GetEnvironmentVariable("esnapper");

            var barGraph = new Json.BarGraph()
            {
                Series = series,
                XAxisTitle = XAxis.Label,
                YAxisTitle = YAxisTitle[YAxisType],
                Title = Name
            };

            return barGraph;

            //string output = JsonConvert.SerializeObject(barGraph);

            //var path = Path.Combine(esnapper, "Experiments", "res","example.json");
           // using var stream = File.Create(path);
           // using var streamWriter = new StreamWriter(stream);

            //streamWriter.WriteLine(output);
        }
    }
}
