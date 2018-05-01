using ServerShared.CustomMaps.ComponentModels.Visual;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components.CustomMaps.EntityComponents.Visual
{
    public class MeshComponent : MapComponent
    {
        private GameObject prefabInstance;

        protected override void UpdateFromModel()
        {
            var model = (EntityMeshComponentModel) Model;

            if (prefabInstance)
            {
                Destroy(prefabInstance);
                prefabInstance = null;
            }

            if (!MapManager.ObjectPrefabs.TryGetValue(model.PrefabId, out var prefab))
            {
                Debug.LogError($"Could not find prefab: {model.PrefabId}");
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
