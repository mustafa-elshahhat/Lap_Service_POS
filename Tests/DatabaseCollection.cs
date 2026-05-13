using Xunit;

[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace CarPartsShopWPF.Tests
{
    [CollectionDefinition("Database")]
    public class DatabaseCollection
    {
    }
}
