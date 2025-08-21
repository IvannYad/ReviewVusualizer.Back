using System.ComponentModel;

namespace ReviewVisualizer.AuthLibrary.Enums
{
    public enum ClaimTypes
    {
        [Description("system_role")]
        SystemRole,

        [Description("generator_modification")]
        GeneratorModifications
    }
}