using Common.Snapper.Core;
using Common.Snapper.Order;
using Common.Snapper.Product;
using ExtendedSnapperLibrary.ActorInterface;
using ExtendedSnapperLibrary.Strategies;
using Orleans;
using Orleans.Concurrency;
using QuikGraph;
using QuikGraph.Graphviz;
using System.Collections.Concurrent;
using System.Diagnostics;
using Utilities;

namespace ExtendedSnapperLibrary.Actor
{
    //<payload information, external transaction id, access information>

    [Reentrant]
    public class ScheduleCoordinator : Grain, IScheduleCoordinator
    {
        ///Our schedules are build based on a graph. The graph needed
        ///is an undirected graph with transactions as nodes and
        ///unlabeled edges between nodes. 
        private readonly UndirectedGraph<SnapperTransaction, Edge<SnapperTransaction>> Schedule = new();

        private readonly Dictionary<long, List<SnapperTransaction>> ReverseOptimizationIndex = new();

        private readonly Helper helper;
        private readonly ConcurrentBid cbid;

        private readonly Random rand;
        //private Stopwatch EmergencyFlush = new Stopwatch();

        /// <summary>
        /// Storage of transaction id to Transaction Context
        /// </summary>
        private readonly ConcurrentDictionary<long, TaskCompletionSource<TransactionContext>> Results = new();

        private readonly ICollectIdsStrategy CollectIdsStrategy;

        /// <summary>
        /// Coordinator Grains Storage to allow external Task to call grains.
        /// It is not possible to use <c>GrainFactory</c> outside of the single threaded
        /// Orleans Grain.
        /// </summary>
        private readonly Dictionary<int, IExtendedCoordinator> CoordinatorGrains;


        public ScheduleCoordinator(Helper helper, ConcurrentBid cbid)
        {
            CollectIdsStrategy = new CollectIdsFromCheckoutAndUpdateProduct();
            CoordinatorGrains = new();
            rand = new();
            foreach (int i in Enumerable.Range(0, helper.numCoordPerGroup))
            {
                CoordinatorGrains.Add(i, GrainFactory.GetGrain<IExtendedCoordinator>(i));
            }
            this.cbid = cbid;
            this.helper = helper;
        }

        private void PushVertexToSchedule(SnapperTransaction vertex)
        {
            IEnumerable<long> ids = CollectIdsStrategy.CollectIds(vertex.FunctionCall);
            Schedule.AddVertex(vertex);
            foreach (long id in ids)
            {
                if (ReverseOptimizationIndex.TryGetValue(id, out List<SnapperTransaction>? candidates))
                {
                    foreach (SnapperTransaction st in candidates)
                    {
                        Schedule.AddEdge(new Edge<SnapperTransaction>(vertex, st));
                    }
                }

                if (!ReverseOptimizationIndex.TryAdd(id, new() { vertex }))
                {
                    ReverseOptimizationIndex[id].Add(vertex);
                }
            }
        }

        public Task Init()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Inserts a snapper transaction into a conflict schedule given
        /// their respective sets of identifiers. The strategy for
        /// extracting the ids from an event can be set using the 
        /// <c>CollectIdsStrategy</c> field.
        /// </summary>
        /// <param name="f">Information about the transaction to be executed using Snapper</param>
        /// <param name="actorAccessInformation">Information about the span of the transaction,
        /// necessary for execution with snapper</param>
        public async Task<TransactionContext> ReceiveTransactionContext(FunctionCall f, Dictionary<ActorID, int> actorAccessInformation)
        {
            //Generate globally unique id
            //Console.WriteLine($"Received a transaction on id: {this.GetPrimaryKeyLong()}");
            byte[] gb = Guid.NewGuid().ToByteArray();
            long etid = BitConverter.ToInt64(gb, 0);

            //Instantiate a non finished TaskCompletionResult
            Results.TryAdd(etid, new());
            //Insert vertex request into Queue and let background thread operate until we have a result
            //which can then be looked up in the results
            PushVertexToSchedule(new(f, actorAccessInformation, etid));
            if (Schedule.VertexCount >= helper.ScheduleBatchSize)
            {
                ExternalBatch? extb = GenerateBatch();
                var coord = CoordinatorGrains[rand.Next(helper.numCoordPerGroup)];
                await coord.RegisterSchedule(extb);
            }
            //The result is ready when enough transactions have been collected to 
            //send a batch. However, there is a small timewindow between the result being
            //published and Snapper being able to receive the transaction. The results 
            //are published a few instructions before registering a schedule with snapper
            var res = await Results[etid].Task;
            //Perform a cleanup on the result dictionary, to prevent infinite
            //growth
            Results.Remove(etid,out _);
            //Debug.Assert(debugTest);
            return res;
        }

        ExternalBatch GenerateBatch()
        {
            //Receiving a batch id from the shared batch id 
            //This bid is unique at this point
            var bid = cbid.GetNextBid();
            ExternalBatch extb = new(bid, bid - 1);
            var componentsAlg = new QuikGraph
                .Algorithms
                .ConnectedComponents
                .ConnectedComponentsAlgorithm<SnapperTransaction, Edge<SnapperTransaction>>(Schedule);
            //Execute the algorithm for calculating the connected components
            componentsAlg.Compute();
            var componentsDict = componentsAlg.Components;

            foreach (var entry in componentsDict)
            {
                //Add the transaction with the according schedule id to the Snapper Interface 
                extb.AddTransaction(entry.Value, entry.Key.Etid, entry.Key.Accesses);
                //Mark the transaction as ready to be processed in the results
                //Invaraiant here is, that a transaction that is stored in the graph must have passed the Queue
                //and in order to be in the queue the ReceiveTransactionContext method must have been used
                //this ensured that an entry for our transaction id is already present in results
                Results[entry.Key.Etid].SetResult(new TransactionContext(entry.Value, bid, entry.Key.Etid));
            }
            //(1) clear the Schedule graph
            Schedule.Clear();
            ReverseOptimizationIndex.Clear();
            return extb;
        }

        

        /*public void RenderGraphToDotFile(string path)
        {
            var dotRepresentation = Schedule.ToGraphviz(algorithm =>
            {
                algorithm.FormatVertex += (sender, args) =>
                {

                    args.VertexFormat.Label = $"etid={args.Vertex.Etid},{args.Vertex.FunctionCall.funcName}(" +
                    $"{string.Join(",",CollectIdsStrategy.CollectIds(args.Vertex.FunctionCall).Select(id => id.ToString()))})";
                };
            });
            File.WriteAllText(path, dotRepresentation);
        }*/
    }
}
