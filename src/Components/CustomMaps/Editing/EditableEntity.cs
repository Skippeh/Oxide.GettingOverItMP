using UnityEngine;
using UnityEngine.Rendering;

namespace Oxide.GettingOverItMP.Components.CustomMaps.Editing
{
    [RequireComponent(typeof(MapEntity))]
    public class EditableEntity : MonoBehaviour
    {
        private MapEntity entity;
        private GameObject iconGizmo;
        private MapEditManager mapEditManager;

        private void Awake()
        {
            mapEditManager = GameObject.FindObjectOfType<MapEditManager>();

            entity = GetComponent<MapEntity>();
            iconGizmo = new GameObject("Icon gizmo");
            iconGizmo.transform.SetParent(transform, false);
            iconGizmo.SetActive(mapEditManager.EditModeEnabled);

            var spriteRenderer = iconGizmo.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = MPContent.Assets.LoadAsset<Sprite>("assets/textures/entity-marker.png");
            spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
            spriteRenderer.receiveShadows = false;
        }

        public void OnEditModeEnabled()
        {
            iconGizmo.SetActive(true);
        }

        public void OnEditModeDisabled()
        {
            iconGizmo.SetActive(false);
        }
    }
}
