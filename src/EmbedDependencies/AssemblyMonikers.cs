using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Techsola.EmbedDependencies
{
    internal static class AssemblyMonikers
    {
        public const string CoreLibrary = nameof(CoreLibrary);
        public const string HasAppDomain = nameof(HasAppDomain);
        public const string HasCollections = nameof(HasCollections);
        public const string HasStringComparer = nameof(HasStringComparer);
        public const string HasStream = nameof(HasStream);

        public static IReadOnlyCollection<string> GetAll()
        {
            return (
                from field in typeof(AssemblyMonikers).GetFields(BindingFlags.Public | BindingFlags.Static)
                where field.FieldType == typeof(string)
                select (string)field.GetValue(null)).ToList();
        }
    }
}
