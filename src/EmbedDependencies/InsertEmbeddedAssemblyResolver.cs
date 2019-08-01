using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;

namespace Techsola.EmbedDependencies
{
    public sealed class InsertEmbeddedAssemblyResolver : Task
    {
        [Required]
        public ITaskItem TargetAssembly { get; set; }

        public override bool Execute()
        {
            using (var stream = new FileStream(TargetAssembly.ItemSpec, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Utils.InsertEmbeddedAssemblyResolver(stream);
            }

            return true;
        }
    }
}
