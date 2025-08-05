using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReviewVisualizer.AuthLibrary
{
    public static class Policies
    {
        public const string RequireGeneratorAdmin = "RequireGeneratorAdmin";
        public const string RequireAnalyst = "RequireAnalyst";
        public const string RequireOwner = "RequireOwner";
        public const string RequireVisitor = "RequireVisitor";

        public const string ModifyFireAndForget = "ModifyFireAndForget";
        public const string ModifyDelayed = "ModifyDelayed";
        public const string ModifyRecurring = "ModifyRecurring";

        public static IEnumerable<string> All => new[]
        {
            RequireGeneratorAdmin, RequireAnalyst, RequireOwner, RequireVisitor,
            ModifyFireAndForget, ModifyDelayed, ModifyRecurring
        };
    }
}
