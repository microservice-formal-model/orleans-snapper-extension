# Experiments and Implementation of ESnapper

## Source Code

### Benchmarks

The source code for actors for Benchmarks (1) ESnapper, (2) Orleans Transactions, (3) Snapper can be found in
- (1) `Marketplace.Grains.Extended`
- (2) `Marketplace.Grains.TransactionOrleans`
- (3) `Marketplace.Grains`

### Schedule Coordinator

Source Code for scalable schedule coordinator actor can be found in `ExtendedSnapperLibrary.Actor.ScheduleCoordinator`.

## Running the Experiment
### Configuration
The configuration for executing an experiment can be found in `Properties/experiment.json`. We summarize here the values that needs to be adjusted.
An exemplary configuration can be found in section *Running the Experiment - On Local Machine - Starting the Experiment*. To alter between experiments, use this exemplary configuration file as a base and only adjust the values stated below.
|Experiment|Configuration|
|--|--|
|Scalability - 4 cores| `"amountProducts":1000`,`"nrCores":4`,`"generatedLocation":"gen/cloudExperimentLoad/core4(extended/snapper)"`,`"amountScheduleCoordinators":1`,`"partitioning":{"nStockPartitions": 16,"nProductPartitions": 16,"nOrderPartitions": 16,"nPaymentPartitions": 16}`,`"benchmarkType": "EXTENDED"/"SNAPPER"`,`"workersInformation": {"amountCheckoutWorkers": 2,"amountUpdateProductWorkers": 2}`|
|Scalability - 8 cores|`"amountProducts":1000`,`"nrCores":4`,`"generatedLocation":"gen/cloudExperimentLoad/core8(extended/snapper)"`,`"amountScheduleCoordinators":2`,`"partitioning":{"nStockPartitions": 32,"nProductPartitions": 32,"nOrderPartitions": 32,"nPaymentPartitions": 32}`,`"benchmarkType": "EXTENDED"/"SNAPPER"`,`"workersInformation": {"amountCheckoutWorkers": 4,"amountUpdateProductWorkers": 4}`|
|Scalability - 12 cores|`"amountProducts":1000`,`"nrCores":4`,`"generatedLocation":"gen/cloudExperimentLoad/core16(extended/snapper)"`,`"amountScheduleCoordinators":2`,`"partitioning":{"nStockPartitions": 64,"nProductPartitions": 64,"nOrderPartitions": 64,"nPaymentPartitions": 64}`,`"benchmarkType": "EXTENDED"/"SNAPPER"`,`"workersInformation": {"amountCheckoutWorkers": 4,"amountUpdateProductWorkers": 4}`|
|Scalability - 16 cores|`"amountProducts":1000`,`"nrCores":4`,`"generatedLocation":"gen/cloudExperimentLoad/core16(extended/snapper)"`,`"amountScheduleCoordinators":4`,`"partitioning":{"nStockPartitions": 64,"nProductPartitions": 64,"nOrderPartitions": 64,"nPaymentPartitions": 64}`,`"benchmarkType": "EXTENDED"/"SNAPPER"`,`"workersInformation": {"amountCheckoutWorkers": 8,"amountUpdateProductWorkers": 8}`|
|Skewness - Uniform|`"amountProducts":1000`,`"nrCores":16`,`"generatedLocation":"gen/checkout5/P1000/hot0/(esnapper/snapper)"`,`"amountScheduleCoordinators":4`,`"partitioning":{"nStockPartitions": 64,"nProductPartitions": 64,"nOrderPartitions": 64,"nPaymentPartitions": 64}`,`"benchmarkType": "EXTENDED"/"SNAPPER"`,`"workersInformation": {"amountCheckoutWorkers": 8,"amountUpdateProductWorkers": 0}`|
|Skewness - 0.1|`"amountProducts":1000`,`"nrCores":16`,`"generatedLocation":"gen/checkout5/P1000/hot01/(esnapper/snapper)"`,`"amountScheduleCoordinators":4`,`"partitioning":{"nStockPartitions": 64,"nProductPartitions": 64,"nOrderPartitions": 64,"nPaymentPartitions": 64}`,`"benchmarkType": "EXTENDED"/"SNAPPER"`,`"workersInformation": {"amountCheckoutWorkers": 8,"amountUpdateProductWorkers": 0}`|
|Skewness - 0.3|`"amountProducts":1000`,`"nrCores":16`,`"generatedLocation":"gen/checkout5/P1000/hot03/(esnapper/snapper)"`,`"amountScheduleCoordinators":4`,`"partitioning":{"nStockPartitions": 64,"nProductPartitions": 64,"nOrderPartitions": 64,"nPaymentPartitions": 64}`,`"benchmarkType": "EXTENDED"/"SNAPPER"`,`"workersInformation": {"amountCheckoutWorkers": 8,"amountUpdateProductWorkers": 0}`|
|Skewness - 0.5|`"amountProducts":1000`,`"nrCores":16`,`"generatedLocation":"gen/checkout5/P1000/hot05/(esnapper/snapper)"`,`"amountScheduleCoordinators":4`,`"partitioning":{"nStockPartitions": 64,"nProductPartitions": 64,"nOrderPartitions": 64,"nPaymentPartitions": 64}`,`"benchmarkType": "EXTENDED"/"SNAPPER"`,`"workersInformation": {"amountCheckoutWorkers": 8,"amountUpdateProductWorkers": 0}`|
|Skewness - 0.7|`"amountProducts":1000`,`"nrCores":16`,`"generatedLocation":"gen/checkout5/P1000/hot07/(esnapper/snapper)"`,`"amountScheduleCoordinators":4`,`"partitioning":{"nStockPartitions": 64,"nProductPartitions": 64,"nOrderPartitions": 64,"nPaymentPartitions": 64}`,`"benchmarkType": "EXTENDED"/"SNAPPER"`,`"workersInformation": {"amountCheckoutWorkers": 8,"amountUpdateProductWorkers": 0}`|
|Skewness - 0.9|`"amountProducts":1000`,`"nrCores":16`,`"generatedLocation":"gen/checkout5/P1000/hot09/(esnapper/snapper)"`,`"amountScheduleCoordinators":4`,`"partitioning":{"nStockPartitions": 64,"nProductPartitions": 64,"nOrderPartitions": 64,"nPaymentPartitions": 64}`,`"benchmarkType": "EXTENDED"/"SNAPPER"`,`"workersInformation": {"amountCheckoutWorkers": 8,"amountUpdateProductWorkers": 0}`|
|Checkout Size 1|`"amountProducts":10000`,`"nrCores":16`,`"generatedLocation":"gen/checkoutSize/P10000/1/(esnapper/snapper)"`,`"amountScheduleCoordinators":4`,`"partitioning":{"nStockPartitions": 64,"nProductPartitions": 64,"nOrderPartitions": 64,"nPaymentPartitions": 64}`,`"benchmarkType": "EXTENDED"/"SNAPPER"`,`"workersInformation": {"amountCheckoutWorkers": 8,"amountUpdateProductWorkers": 0}`|
|Checkout Size 5|`"amountProducts":10000`,`"nrCores":16`,`"generatedLocation":"gen/checkoutSize/P10000/5/(esnapper/snapper)"`,`"amountScheduleCoordinators":4`,`"partitioning":{"nStockPartitions": 64,"nProductPartitions": 64,"nOrderPartitions": 64,"nPaymentPartitions": 64}`,`"benchmarkType": "EXTENDED"/"SNAPPER"`,`"workersInformation": {"amountCheckoutWorkers": 8,"amountUpdateProductWorkers": 0}`|
|Checkout Size 10|`"amountProducts":10000`,`"nrCores":16`,`"generatedLocation":"gen/checkoutSize/P10000/10/(esnapper/snapper)"`,`"amountScheduleCoordinators":4`,`"partitioning":{"nStockPartitions": 64,"nProductPartitions": 64,"nOrderPartitions": 64,"nPaymentPartitions": 64}`,`"benchmarkType": "EXTENDED"/"SNAPPER"`,`"workersInformation": {"amountCheckoutWorkers": 8,"amountUpdateProductWorkers": 0}`|
|Checkout Size 20|`"amountProducts":10000`,`"nrCores":16`,`"generatedLocation":"gen/checkoutSize/P10000/20/(esnapper/snapper)"`,`"amountScheduleCoordinators":4`,`"partitioning":{"nStockPartitions": 64,"nProductPartitions": 64,"nOrderPartitions": 64,"nPaymentPartitions": 64}`,`"benchmarkType": "EXTENDED"/"SNAPPER"`,`"workersInformation": {"amountCheckoutWorkers": 8,"amountUpdateProductWorkers": 0}`|
|Checkout Size 50|`"amountProducts":10000`,`"nrCores":16`,`"generatedLocation":"gen/checkoutSize/P10000/50/(esnapper/snapper)"`,`"amountScheduleCoordinators":4`,`"partitioning":{"nStockPartitions": 64,"nProductPartitions": 64,"nOrderPartitions": 64,"nPaymentPartitions": 64}`,`"benchmarkType": "EXTENDED"/"SNAPPER"`,`"workersInformation": {"amountCheckoutWorkers": 8,"amountUpdateProductWorkers": 0}`|
### On Local Machine
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

We included the set of pregenerated workloads used in the paper in the `/gen` directory. A table explaining the settings for each experiments and which folder to use can be found in this README in section *Running the Experiment - Configuration* It is important to match the pregenerated file with the correct benchmark version and cpu amount. 

The configuration for the experiments is found in `Experiments/Properties/experiments.json`. We have included the execution for a hotset experiment for the Extended (E-Snapper) Benchmark using 8 cores, it looks like this:
```json
{
  "resultLocation": "res/",
  "benchmarkControl": false,
  "experiments": [
    {
      "id": 1,
      "isLocal": true,
      "useAmazonDB": false,
      "amountProducts": 1000,
      "nrCores": 16,
      "runtime": 30,
      "nrActiveTransactions" :  200,
      "generateLoad": false,
      "distributeGeneratedLoad": true,
      "generatedLocation": "gen/checkout5/P1000/hot07/esnapper",
      "amountScheduleCoordinators": 8,
      "partitioning": {
        "nStockPartitions": 64,
        "nProductPartitions": 64,
        "nOrderPartitions": 64,
        "nPaymentPartitions": 64
      },
      "benchmarkType": "EXTENDED",
      "distribution": {
        "type": "UNIFORM",
        "zipfianConstant": 0.5,
        "itemSets": [
          {
            "id": 0,
            "pick": "INDIVIDUAL",
            "items": [ 0, 1, 2, 3, 4 ]
          },
          {
            "id": 1,
            "pick": "RANGE",
            "items": [ 5, 999 ]
          }
        ],
        "probabilities": [
          {
            "id": 0,
            "probability": 0.3
          },
          {
            "id": 1,
            "probability": 0.7
          }
        ]
      },
      "checkoutInformation": {
        "totalAmount": 16000,
        "size": {
          "start": 5,
          "end": 5
        }
      },
      "updateProductInformation": {
        "totalAmount": 16000,
        "minimumReplenish": 10,
        "maximumReplenish": 10000
      },
      "workersInformation": {
        "amountCheckoutWorkers": 8,
        "amountUpdateProductWorkers": 0
      }
    }
  ]
}
```
**Note that information in attribute `experiment.distribution` is only used when generating a new workload. For recreating the experiments we included all the workloads already. This is indicated by the `generateLoad:false` property** 

Section *Running the Experiment - Configuration* contains information about configuration for each experiment. 

When starting the experiment, the server should already be running configured to the expected experiment. 

For our example with 16 cores, EXTENDED, 64 partitions, starting the server looks like this:
```
dotnet run --project SnapperServer --cpu 16 --extended --numScheduleCoord 8 --batchSize 20 -l
```
![image](https://github.com/microservice-formal-model/orleans-snapper-extension/assets/172083713/5bee9e70-a036-4e64-a595-2750c1d6d921)

Starting the experiment will look like this:
```
dotnet run --project Experiment
```
![image](https://github.com/microservice-formal-model/orleans-snapper-extension/assets/172083713/84d99f4d-7251-4c2b-84b2-67120bf86ace)


The Power Shell Window running the experiment will state the result of the experiment. In our example `Average Latency: 85 ms` states the average latency for the whole runtime of the experiment, and likewise `Average Throughput: 18415 Tr/sec` the average throughput.

**!!Please note that this performs the experiments on the local machine, to get the results from the paper it is required to start the experiments on the Amazon instance as described in the next section!!**

### On Amazon Cloud
#### Prerequesites
- (1) Two Amazon instances (for the experiment in the paper, we used 2x c5.9xlarge) that are in the same *Placement Group*. 
- (2) Access to a DynamoDB Table is required. This will maintain the Orleans Cluster.
- (3) Clone the project on both instances 
- (4) Both instances need to have an environment variable named `esnapper` that points to the root directory of the repository.
- (5)  On the experiment machine, go to the .cs-file located in `Experiments/OrleansClientManager.cs`. Alter the Source Code by adding your secret information to the `DynamoDBGatewayOptions`.
  ```csharp
  Action<DynamoDBGatewayOptions> dynamoOptions = options =>
  {
  	options.Service = yourRegion;
  	options.AccessKey = yourAccessKey;
  	options.SecretKey = yourSecretKey;
  }; 
  ```   		 
- (6) On the server machine, go to the .cs-file located in `SnapperServer/Program.cs`. Alter the Source Code by adding your secret information to the `DynamoDBGatewayOptions`.
  ```
  Action<DynamoDBClusteringOptions> dynamoOptions = options =>
  {
      options.AccessKey = yourAccessKey;
      options.SecretKey = youSecretKey;
      options.Service = yourRegion;
  };
  ```
- On your server machine, from the root repository create the directory `bin/Release/net7.0/Properties/`. In this directory create a file `server-config.json` containing the public ip address of your server.
  ```json
  {
    "pubIP": yourIp
  }
  ```
#### Starting the Server
Start the server according to the experiment demands as described in the section about starting the experiments locally. 
The only thing you need to alter is removing the `-l` flag from the dotnet start command.

In the Amazon Cloud the source code for altering the number of CPUs does not work. This is due to the settings of the virtual machine.
In order to change the number of cpus, it is required to go into the TaskManager, find the process of the dotnet execution and change the amount of cores there manually.
A guide on how to do this can be found [here](https://superuser.com/questions/309617/how-to-limit-a-process-to-a-single-cpu-core). We summarize the steps:   
  - Press Ctrl + Shift + Esc to get open Task Manager.
  - Click on the Processes tab.
  - Find the process that needs its processor affinity changed.
  - Right-click on the process.
  - Click on "Set Affinity".
#### Starting the Experiments
The experiments can be started as explained in the local section, however the `experiments.json` configuration file needs to set the values `"useAmazonDB":true`, and `"isLocal":false`.
