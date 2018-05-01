using ServerShared.CustomMaps.ComponentModels.Collision;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components.CustomMaps.EntityComponents.Collision
{
    public class CircleColliderComponent : MapComponent
    {
        protected override void UpdateFromModel()
        {
            var model = (EntityCircleColliderComponentModel) Model;

            var collider = gameObject.GetComponent<CircleCollider2D>() ?? gameObject.AddComponent<CircleCollider2D>();
            collider.radius = model.Radius;
            collider.sharedMaterial = new PhysicsMaterial2D
            {
                bounciness = model.Bounciness,
                friction = model.Friction
            };

            var groundCol = gameObject.GetComponent<GroundCol>() ?? gameObject.AddComponent<GroundCol>();
            groundCol.material = (GroundCol.SoundMaterial) model.Material;
        }

        private void OnDestroy()
        {
            Destroy(GetComponent<CircleCollider2D>());
            Destroy(GetComponent<GroundCol>());
        }
    }
}
