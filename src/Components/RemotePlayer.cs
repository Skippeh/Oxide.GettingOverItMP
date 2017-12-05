using UnityEngine;

namespace Oxide.GettingOverItMP.Components
{
    public class RemotePlayer : MonoBehaviour
    {
        public static GameObject PlayerPrefab { get; private set; }

        public static void CreatePlayerPrefab()
        {
            PlayerPrefab = new GameObject();
            PlayerPrefab.SetActive(false);
            PlayerPrefab.name = "RemotePlayer Prefab";

            
        }
    }
}
