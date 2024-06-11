using Experiments.Execution.Result;
using Experiments.ExperimentsModel;
using Experiments.Plotting.Json;

namespace Experiments.Plotting.Config.LineGraph
{
    public class LineGraphPlot : APlot
    {
        public required XAxis XAxis { get; set; }
        public override AJsonGraph GeneratePlot(List<IResult> experimentResults)
        {
            List<BenchmarkType> lines = new();

            foreach (IResult result in experimentResults)
            {
                if (Ids.Contains(result.ExperimentID) && !lines.Contains(result.BenchmarkType))
                {
                    lines.Add(result.BenchmarkType);
                }
            }

            Dictionary<BenchmarkType, List<Tuple<int, int>>> lineToXYPoints = new();

            foreach(BenchmarkType tpe in lines){
                lineToXYPoints.TryAdd(tpe, new());
            }

            foreach (Point point in XAxis.Points)
            {
                foreach (int experimentId in point.Ids)
                {
                    var experimentResult = experimentResults.FindLast(e => e.ExperimentID == experimentId &&
                      MatchTypes(YAxisType, e)) ??
                        throw new ArgumentOutOfRangeException($"Experiment with id {experimentId} can not be found for YAxis Type {YAxisType}.");

                    int res = Convert.ToInt32(GetResult(experimentResult));

                    lineToXYPoints[experimentResult.BenchmarkType].Add(Tuple.Create(point.Label, res));
                }
            }

            Dictionary<YAxisType, string> YAxisTitle = new()
            {
                {YAxisType.THROUGHPUT, "Tr/s" },
                {YAxisType.LATENCY, "ms" }
            };

            List<Line> jsonLines = new();
            foreach (var l in lineToXYPoints)
            {
                jsonLines.Add(new Line()
                {
                    Name = l.Key.ToString(),
                    Y = l.Value.Select(xy => xy.Item2).ToList(),
                    X = l.Value.Select(xy => xy.Item1).ToList()
                });
            }

            return new Json.LineGraph()
            {
                XAxisTitle = XAxis.Label,
                YAxisTitle = YAxisTitle[YAxisType],
                Title = Name,
                Type = "LinePlot",
                Lines = jsonLines
            };
        }
    }
}
