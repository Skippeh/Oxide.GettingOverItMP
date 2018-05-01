using ServerShared.CustomMaps.ComponentModels;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components.CustomMaps.EntityComponents
{
    public class MapComponent<T> : MonoBehaviour where T : MapEntityComponentModel
    {
        public T Model { get; private set; }

        public void UpdateModel(T model)
        {
            Model = model;
            UpdateFromModel();
        }

        protected virtual void UpdateFromModel()
        {
        }
    }
}
