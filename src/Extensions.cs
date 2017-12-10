using System;
using System.Reflection;

namespace Oxide.GettingOverItMP
{
    public static class Extensions
    {
        public static bool IsPaused(this PlayerControl control)
        {
            if (control == null) throw new ArgumentNullException(nameof(control));
            var fieldInfo = typeof(PlayerControl).GetField("menuPause", BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool) fieldInfo.GetValue(control);
        }
    }
}
