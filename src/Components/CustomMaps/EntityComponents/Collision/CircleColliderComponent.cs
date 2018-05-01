using ServerShared.CustomMaps.ComponentModels;
using ServerShared.CustomMaps.ComponentModels.Collision;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components.CustomMaps.EntityComponents.Collision
{
    public class CircleColliderComponent : MapComponent<EntityCircleColliderComponentModel>
    {
        protected override void UpdateFromModel()
        {
            var collider = gameObject.GetComponent<CircleCollider2D>() ?? gameObject.AddComponent<CircleCollider2D>();
            collider.radius = Model.Radius;
            collider.sharedMaterial = new PhysicsMaterial2D
            {
                bounciness = Model.Bounciness,
                friction = Model.Friction
            };

            var groundCol = gameObject.GetComponent<GroundCol>() ?? gameObject.AddComponent<GroundCol>();
            groundCol.material = (GroundCol.SoundMaterial) Model.Material;
        }

        private void OnDestroy()
        {
            Destroy(GetComponent<CircleCollider2D>());
            Destroy(GetComponent<GroundCol>());
        }
    }
}
