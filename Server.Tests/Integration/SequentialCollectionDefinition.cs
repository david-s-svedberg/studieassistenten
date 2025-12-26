namespace StudieAssistenten.Server.Tests.Integration;

/// <summary>
/// Collection definition for tests that must run sequentially (not in parallel).
/// This is necessary because integration tests share an in-memory database.
/// </summary>
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class SequentialCollectionDefinition
{
}
