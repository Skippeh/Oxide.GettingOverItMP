using System;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Extensions;

namespace Oxide.GettingOverIt
{
    public class MPExtension : Extension
    {
        internal static readonly Assembly Assembly = Assembly.GetExecutingAssembly();
        internal static readonly AssemblyName AssemblyName = Assembly.GetName();
        internal static readonly VersionNumber AssemblyVersion = new VersionNumber(AssemblyName.Version.Major, AssemblyName.Version.Minor, AssemblyName.Version.Build);
        internal static readonly string AssemblyAuthors = ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(Assembly, typeof(AssemblyCompanyAttribute), false)).Company;
        
        public MPExtension(ExtensionManager manager) : base(manager)
        {
        }

        public override string Name => "Getting Over It with Bennett Foddy Multiplayer";
        public override string Author => AssemblyAuthors;
        public override VersionNumber Version => AssemblyVersion;

        public override void Load() => Manager.RegisterPluginLoader(new MPPluginLoader());
    }
}
