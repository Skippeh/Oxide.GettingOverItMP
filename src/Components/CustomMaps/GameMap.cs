using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.GettingOverIt;
using Oxide.GettingOverItMP.Components.CustomMaps.EntityComponents;
using Oxide.GettingOverItMP.Components.CustomMaps.EntityComponents.Collision;
using Oxide.GettingOverItMP.Components.CustomMaps.EntityComponents.Visual;
using RuntimeGizmos;
using ServerShared.CustomMaps;
using ServerShared.CustomMaps.ComponentModels;
using ServerShared.CustomMaps.ComponentModels.Collision;
using ServerShared.CustomMaps.ComponentModels.Informational;
using ServerShared.CustomMaps.ComponentModels.Visual;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components.CustomMaps
{
    public class GameMap : MonoBehaviour
    {
        private readonly Dictionary<uint, MapEntity> entities = new Dictionary<uint, MapEntity>();
        private List<MapEntity> spawnPoints = new List<MapEntity>();
        
        public MapEntity SpawnEntity(MapEntityModel entityModel)
        {
            GameObject newObject = new GameObject("MapEntity_" + entityModel.Id);
            newObject.transform.SetParent(transform, false);

            foreach (MapEntityComponentModel baseComponent in entityModel.Components)
            {
                var componentObject = new GameObject("Component_" + baseComponent.Id);
                componentObject.transform.SetParent(newObject.transform, false);

                if (baseComponent is EntityMeshComponentModel meshModel)
                {
                    var component = componentObject.AddComponent<MeshComponent>();
                    component.UpdateModel(meshModel);
                }
                else if (baseComponent is EntityPolygonColliderComponentModel polygonCollisionModel)
                {
                    var component = componentObject.AddComponent<PolygonColliderComponent>();
                    component.UpdateModel(polygonCollisionModel);
                }
                else if (baseComponent is EntityCircleColliderComponentModel circleCollisionModel)
                {
                    var component = componentObject.AddComponent<CircleColliderComponent>();
                    component.UpdateModel(circleCollisionModel);
                }
                else if (baseComponent is EntitySpawnPointComponentModel)
                {
                    // No action required
                }
                else
                {
                    throw new NotImplementedException($"Component model not implemented: {baseComponent.GetType().FullName}.");
                }
                
                componentObject.transform.localPosition = baseComponent.Position;
                componentObject.transform.localRotation = baseComponent.Rotation;
                componentObject.transform.localScale = baseComponent.Scale;
            }
            
            var entity = newObject.AddComponent<MapEntity>();
            entity.Id = entityModel.Id;

            newObject.transform.localPosition = entityModel.Position;
            newObject.transform.localRotation = entityModel.Rotation;
            newObject.transform.localScale = entityModel.Scale;
            
            var body = newObject.AddComponent<Rigidbody2D>();
            body.bodyType = entityModel.Kinematic ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;

            newObject.SetLayerRecursively(LayerMask.NameToLayer("Terrain"));
            entities.Add(entity.Id, entity);

            MPCore.LogGameObjects(new[] {newObject});
            if (entityModel.Components.Any(component => component is EntitySpawnPointComponentModel))
                spawnPoints.Add(entity);
            
            return entity;
        }

        public void RemoveEntity(MapEntity entity)
        {
            if (entities.Remove(entity.Id))
                Destroy(entity.gameObject);
        }

        /// <summary>
        /// Returns a random spawn point.
        /// </summary>
        public Vector3 GetSpawnPoint()
        {
            if (spawnPoints.Count == 0)
                return Vector3.zero;

            var spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count - 1)];
            return spawnPoint.transform.position;
        }
    }
}
