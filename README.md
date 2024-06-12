## Experiments and Implementation of ESnapper

### Benchmarks

The source code for actors for Benchmarks (1) ESnapper, (2) Orleans Transactions, (3) Snapper can be found in
- (1) `Marketplace.Grains.Extended`
- (2) `Marketplace.Grains.TransactionOrleans`
- (3) `Marketplace.Grains`

### Experiments 

The source code for Experiments can be found in `Experiments`

Before starting the experiments, make sure that the configuration file found in `Experiments/Properties/experiments.json` is up to date.

The values in the configuration file are listed below:

- `resultlocation` : location relative to the repository where results are going to be stored
- `benchmarkControl` : shh support (true), no ssh support (false)
- `sshinfo` : information about ssh connection where experiment should be uploaded
- `experiments` : information about experiments
- `experiments.id` : identifier for the experiment
- `experiments.amountProducts` : amount of available products
- `experiments.nrCores` : on how many cores should this experiment be performed
- `experiments.runtime` : time of experiment execution in seconds
- `experiments.nrActiveTransactions` : Nr of transactions active at a time in a client worker 

### Running the Experiment
#### Prerequesites
- (1) Clone the repository 
- (2) This project needs the latest .NET version installed. You can find the installation file [here](https://dotnet.microsoft.com/en-us/).

#### Starting the Server for Snapper 
In a PowerShell Window, navigate to the repository. The server can be started by using the `dotnet run --project SnapperServer` command.
It is possible to state several options when starting the server, they are listed below:
- `-l,--local` Start Server Locally. Default: Not locally.
- `--extended`,`--snapper`,`orleansTrans`: Start the Server for the specified benchmark implementation.
- `--cpu` : Sets the amount of cores (not that when performing the experiments on virtual machines, the amount of cores needs to be set manually in the task manager after starting the server).
- `--numCoord`: Amount of Snapper Coordinators, default 8.
- `--batchSize` : When running the `--extended` version, controls the size of collected transaction before emitted to the system. default: 100.
- `--numScheduleCoord`: Number of Schedule Coordinators, default: 1.
- 
Example: The server can be started for the extended benchmark on 8 cores with 1 schedule coordinator and a batchSize of 50, locally like this:
```
dotnet run --project SnapperServer --cpu 8 --extended --numScheduleCoord 1 --batchSize 50 -l
```




