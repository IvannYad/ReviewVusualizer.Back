namespace ReviewVisualizer.AuthLibrary.Enums
{
    [Flags]
    public enum GeneratorModifications
    {
        None = 0,
        View = 1,
        ModifyFireAndForget = 2,
        ModifyDelayed = 4,
        ModifyRecurring = 8
    }
}
