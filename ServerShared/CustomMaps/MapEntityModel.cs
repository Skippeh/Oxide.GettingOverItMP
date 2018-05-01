using System.Collections.Generic;
using ServerShared.CustomMaps.ComponentModels;
using UnityEngine;

namespace ServerShared.CustomMaps
{
    public sealed class MapEntityModel
    {
        public uint Id;
        public Vector3 Position = Vector3.zero;
        public Quaternion Rotation = Quaternion.identity;
        public Vector3 Scale = Vector3.one;
        public bool Kinematic = true;

        public List<MapEntityComponentModel> Components { get; } = new List<MapEntityComponentModel>();
    }
}
