namespace ReviewVisualizer.Generator.IntegrationTests.Utils
{
    public enum TestUser
    {
        NotAuthorized = 128,
        Visitor = 1,
        Analyst = 2,
        GeneratorAdmin_FireAndForget = 4,
        GeneratorAdmin_Delayed = 8,
        GeneratorAdmin_Recurring = 16,
        Owner = 32
    }
}