using System;
using Oxide.Core.Plugins;

namespace Oxide.GettingOverIt
{
    public class MPPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new [] { typeof(MPCore) };
    }
}
