# Experiments and Implementation of ESnapper

## Source Code

### Benchmarks

The source code for actors for Benchmarks (1) ESnapper, (2) Orleans Transactions, (3) Snapper can be found in
- (1) `Marketplace.Grains.Extended`
- (2) `Marketplace.Grains.TransactionOrleans`
- (3) `Marketplace.Grains`

## Running the Experiment
#### Prerequesites
- (1) Clone the repository 
- (2) This project needs the latest .NET version installed. You can find the installation file [here](https://dotnet.microsoft.com/en-us/).
- (3) Setting up Environment variable `esnapper`: This project requires an environment variable to be set to the root of the repository. 
      Add to your system variables a variable named `esnapper` with location: `.../orleans-snapper-extension/`.
#### Starting the Server for Snapper 
In a PowerShell Window, navigate to the repository. The server can be started by using the `dotnet run --project SnapperServer` command.
It is possible to state several options when starting the server, they are listed below:
- `-l,--local` Start Server Locally. Default: Not locally.
- `--extended`,`--snapper`,`--orleansTrans`: Start the Server for the specified benchmark implementation.
- `--cpu` : Sets the amount of cores (not that when performing the experiments on virtual machines, the amount of cores needs to be set manually in the task manager after starting the server).
- `--numCoord`: Amount of Snapper Coordinators, default 8.
- `--batchSize` : When running the `--extended` version, controls the size of collected transaction before emitted to the system. default: 100.
- `--numScheduleCoord`: Number of Schedule Coordinators, default: 1.
- 
Example: The server can be started for the extended benchmark on 8 cores with 1 schedule coordinator and a batchSize of 50, locally like this:
```
dotnet run --project SnapperServer --cpu 8 --extended --numScheduleCoord 1 --batchSize 50 -l
```

#### Starting the Experiment
The Experiment can be started by using the command `dotnet run --project Experiment` in a PowerShell located in the root of the repository.

We included the set of pregenerated workloads used in the paper in the `/gen` directory. It is important to match the pregenerated file with the correct benchmark version and cpu amount. The folder name indicates the benchmark version and cpu amount.
For example `Experiments/gen/core8extended` is meant to be used with an experiment for 8 cores and the extended benchmark.

The configuration for the experiments is found in `gen/experiments.json`. We have included the execution for an experiment for the Extended (E-Snapper) Benchmark using 8 cores, it looks like this:
```json
{
  "resultLocation": "res/",
  "benchmarkControl": false,
  "experiments": [
    {
      "id": 1,
      "isLocal": false,
	  "useAmazonDB": false,
      "amountProducts": 1000,
      "amountScheduleCoordinators":1,
      "nrCores": 8,
      "runtime": 15,
      "generateLoad": false,
      "distributeGeneratedLoad": true,
      "generatedLocation": "gen/core8extended",
      "partitioning": {
        "nStockPartitions": 8,
        "nProductPartitions": 8,
        "nOrderPartitions": 8,
        "nPaymentPartitions": 8
      },
      "benchmarkType": "EXTENDED",
      "distribution": {
        "type": "UNIFORM",
        "zipfianConstant": 0.3
      },
      "checkoutInformation": {
        "totalAmount": 16000,
        "size": {
          "start": 1,
          "end": 10
        }
      },
      "updateProductInformation": {
        "totalAmount": 16000,
        "minimumReplenish": 10,
        "maximumReplenish": 10000
      },
      "workersInformation": {
        "amountCheckoutWorkers": 4,
        "amountUpdateProductWorkers": 4
      }
    }
  ]
}
```
The config contains relevant information about the experiment. In order to change the experiment to another pre generated transaction workload, you need to change this config in the following way:
- (1) Change `experiment.nrCores` to desired amount
- (2) Adjust the `experiment.runtime` to the desired time in seconds that the experiment should execute
- (3) Choose the desired workload folder in `experiment.generatedLocation`
- (4) Adjust the partitioning of all 4 actor types similarily as stated in the paper (4 cores -> 4 partitions, 8 cores -> 8 partitions, 16 cores -> 16 partitions) NOTE: This needs to be accurate, otherwise the experiment will not transmit transactions correctly
- (5) Change `experiment.benchmarkType` field to the desired benchmark ("SNAPPER","EXTENDED","TRANSACTIONS")
- (6) Change or leave `workersInformation.*` field expressing worker amount of client workers transmitting update product or checkout transactions (needs to be a power of 2)

When starting the experiment, the server should already be running configured to the expected experiment. 

For the example with 8 cores, EXTENDED, 8 partitions, starting the server looks like this:
```
dotnet run --project SnapperServer --cpu 8 --extended --numScheduleCoord 1 --batchSize 50 -l
```
![image](https://github.com/microservice-formal-model/orleans-snapper-extension/assets/172083713/71b2096e-a977-42a4-99d4-fe600396ed4b)
Starting the experiment will look like this:
```
dotnet run --project Experiment
```
![image](https://github.com/microservice-formal-model/orleans-snapper-extension/assets/172083713/3268c189-33aa-41f7-92b8-9de64ff90353)

The Power Shell Window running the experiment will state the result of the experiment. In our example `Average Latency: 85 ms` states the average latency for the whole runtime of the experiment, and likewise `Average Throughput: 18415 Tr/sec` the average throughput.

**!!Please note that this performs the experiments on the local machine, to get the results from the paper it is required to start the experiments on the Amazon instance as described in the paper!!**


