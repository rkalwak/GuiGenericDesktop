using Xunit;

// Enable parallel test execution for this assembly.
// MaxParallelThreads = 0 lets xUnit choose a sensible default (number of processors).
[assembly: CollectionBehavior(DisableTestParallelization = false, MaxParallelThreads = 4)]
