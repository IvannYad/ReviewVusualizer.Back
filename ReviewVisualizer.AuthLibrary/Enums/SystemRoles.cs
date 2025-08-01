namespace ReviewVisualizer.AuthLibrary.Enums
{
    [Flags]
    public enum SystemRoles
    {
        Visitor = 0, // has access only to main page
        Analyst = 1, // has access to departments page and can check all departments and teachers ratings
        GeneratorAdmin = 2, // has access to generator
        Owner = 4 // has access to everything, including Manage users page.
    }
}
