using System.Collections.Generic;
using GettingOverItMP.UnitySDK.Components.EntityComponents;
using UnityEngine;

namespace GettingOverItMP.UnitySDK.Components
{
    public class LevelHost : MonoBehaviour
    {
        public Vector2 Gravity = new Vector2(0, -9.81f);
        public string LevelName;

        public SpawnPoint[] GetSpawnPoints()
        {
            return GameObject.FindObjectsOfType<SpawnPoint>();
        }
    }
}
