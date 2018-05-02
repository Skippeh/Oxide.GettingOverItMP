using UnityEngine;

namespace Oxide.GettingOverItMP.Components.CustomMaps.Editing
{
    [RequireComponent(typeof(MapEntity))]
    public class EditableEntity : MonoBehaviour
    {
        private MapEntity entity;

        private void Awake()
        {
            entity = GetComponent<MapEntity>();
        }

        public void OnEditModeEnabled()
        {

        }

        public void OnEditModeDisabled()
        {

        }
    }
}
