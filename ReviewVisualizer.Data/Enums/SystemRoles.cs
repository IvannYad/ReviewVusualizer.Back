namespace ReviewVisualizer.Data.Enums
{
    [Flags]
    public enum SystemRoles
    {
        None = 0,
        Visitor = 1, // has access only to main page
        Analyst = 2, // has access to departments page and can check all departments and teachers ratings
        GeneratorAdmin = 4, // has access to generator
        Owner = 8 // has access to everything, including Manage users page.
    }
}