using UnityEngine;

namespace ServerShared.CustomMaps.ComponentModels
{
    public abstract class MapEntityComponentModel
    {
        public uint Id;
        public Vector3 Position = Vector3.zero;
        public Quaternion Rotation = Quaternion.identity;
        public Vector3 Scale = Vector3.one;
    }
}
