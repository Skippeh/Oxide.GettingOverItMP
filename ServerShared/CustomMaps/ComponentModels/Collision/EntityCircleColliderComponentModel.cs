namespace ServerShared.CustomMaps.ComponentModels.Collision
{
    public class EntityCircleColliderComponentModel : MapEntityComponentModel
    {
        public float Friction;
        public float Bounciness;
        public float Radius = 1f;
        public int Material;
    }
}
