using ServerShared.CustomMaps.ComponentModels;
using ServerShared.CustomMaps.ComponentModels.Visual;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components.CustomMaps.EntityComponents.Visual
{
    public class MeshComponent : MapComponent<EntityMeshComponentModel>
    {
        private GameObject prefabInstance;

        protected override void UpdateFromModel()
        {
            if (prefabInstance)
            {
                Destroy(prefabInstance);
                prefabInstance = null;
            }

            if (!MapManager.ObjectPrefabs.TryGetValue(Model.PrefabId, out var prefab))
            {
                Debug.LogError($"Could not find prefab: {Model.PrefabId}");
                return;
            }
            
            prefabInstance = Instantiate(prefab);
            prefabInstance.transform.SetParent(transform, false);
            prefabInstance.SetActive(true);
        }

        private void OnDestroy()
        {
            if (prefabInstance)
                Destroy(prefabInstance);
        }
    }
}
