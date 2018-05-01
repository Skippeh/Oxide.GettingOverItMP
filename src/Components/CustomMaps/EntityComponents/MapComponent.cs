using ServerShared.CustomMaps.ComponentModels;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components.CustomMaps.EntityComponents
{
    public class MapComponent : MonoBehaviour
    {
        public MapEntityComponentModel Model { get; private set; }

        public void UpdateModel(MapEntityComponentModel model)
        {
            Model = model;
            UpdateFromModel();
        }

        protected virtual void UpdateFromModel()
        {
        }
    }
}
