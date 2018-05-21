using System;
using System.IO;
using System.Reflection;
using UnityEngine;

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

        public static void SetLayerRecursively(this GameObject gameObject, int layer)
        {
            gameObject.layer = layer;

            foreach (Transform transform in gameObject.transform)
            {
                if (transform == null)
                    continue;

                SetLayerRecursively(transform.gameObject, layer);
            }
        }
    }
}
