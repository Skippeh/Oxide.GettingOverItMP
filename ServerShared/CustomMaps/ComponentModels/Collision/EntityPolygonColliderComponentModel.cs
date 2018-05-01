using UnityEngine;

namespace ServerShared.CustomMaps.ComponentModels.Collision
{
    public class EntityPolygonColliderComponentModel : MapEntityComponentModel
    {
        public Vector2[] Points;
        public float Friction;
        public float Bounciness;
        public int Material;
    }
}
