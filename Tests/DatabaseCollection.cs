using Xunit;

[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace AlJohary.ServiceHub.Tests
{
    [CollectionDefinition("Database")]
    public class DatabaseCollection
    {
    }
}
