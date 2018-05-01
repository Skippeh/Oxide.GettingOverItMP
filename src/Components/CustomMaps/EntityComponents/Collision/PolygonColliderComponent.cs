using ServerShared.CustomMaps.ComponentModels;
using ServerShared.CustomMaps.ComponentModels.Collision;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components.CustomMaps.EntityComponents.Collision
{
    public class PolygonColliderComponent : MapComponent<EntityPolygonColliderComponentModel>
    {
        protected override void UpdateFromModel()
        {
            var collider = gameObject.GetComponent<PolygonCollider2D>() ?? gameObject.AddComponent<PolygonCollider2D>();
            collider.points = Model.Points;
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
            Destroy(GetComponent<PolygonCollider2D>());
            Destroy(GetComponent<GroundCol>());
        }
    }
}
