using ServerShared.CustomMaps.ComponentModels;
using ServerShared.CustomMaps.ComponentModels.Collision;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components.CustomMaps.EntityComponents.Collision
{
    public class PolygonColliderComponent : MapComponent
    {
        protected override void UpdateFromModel()
        {
            var model = (EntityPolygonColliderComponentModel) Model;

            var collider = gameObject.GetComponent<PolygonCollider2D>() ?? gameObject.AddComponent<PolygonCollider2D>();
            collider.points = model.Points;
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
            Destroy(GetComponent<PolygonCollider2D>());
            Destroy(GetComponent<GroundCol>());
        }
    }
}
