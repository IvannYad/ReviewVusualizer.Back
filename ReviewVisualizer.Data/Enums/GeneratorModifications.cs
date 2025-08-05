namespace ReviewVisualizer.Data.Enums
{
    [Flags]
    public enum GeneratorModifications
    {
        View = 0,
        ModifyFireAndForget = 1,
        ModifyDelayed = 2,
        ModifyRecurring = 4
    }
}
