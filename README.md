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


