using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.GettingOverItMP
{
    internal static class Utility
    {
        public static string ToHierarchicalString(this GameObject gameObject)
        {
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));
            
            var builder = new StringBuilder();

            if (gameObject.transform.parent != null)
                builder.Append(gameObject.transform.parent.gameObject.ToHierarchicalString() + "/");

            builder.Append(gameObject.name);
            
            return builder.ToString();
        }

        public static IEnumerable<T> ToIEnumerable<T>(this IEnumerator<T> enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }

        public static IEnumerable<T> ToIEnumerable<T>(this IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return (T) enumerator.Current;
            }
        }

        public static IEnumerable<GameObject> GetChildren(this GameObject gameObject)
        {
            return gameObject.transform.GetEnumerator().ToIEnumerable<Transform>().Select(t => t.gameObject);
        }

        public static T GetComponentInSelfOrChildren<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.GetComponent<T>() ?? gameObject.GetComponentInChildren<T>();
        }

        public static T[] GetComponentsInSelfAndChildren<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.GetComponents<T>().Concat(gameObject.GetComponentsInChildren<T>()).ToArray();
        }
    }
}
