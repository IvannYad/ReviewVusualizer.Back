using System.ComponentModel;

namespace ReviewVisualizer.AuthLibrary.Enums
{
    public enum ClaimTypes
    {
        [Description("processor_access")]
        ProcessorAccess,

        [Description("system_role")]
        SystemRole,

        [Description("generator_modification")]
        GeneratorModifications
    }
}
