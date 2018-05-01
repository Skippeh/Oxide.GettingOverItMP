using System;
using System.Collections.Generic;
using Oxide.GettingOverIt;
using Oxide.GettingOverItMP.Components.CustomMaps.EntityComponents;
using Oxide.GettingOverItMP.Components.CustomMaps.EntityComponents.Collision;
using Oxide.GettingOverItMP.Components.CustomMaps.EntityComponents.Visual;
using ServerShared.CustomMaps;
using ServerShared.CustomMaps.ComponentModels;
using ServerShared.CustomMaps.ComponentModels.Collision;
using ServerShared.CustomMaps.ComponentModels.Visual;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components.CustomMaps
{
    public class GameMap : MonoBehaviour
    {
        private readonly Dictionary<uint, MapEntity> entities = new Dictionary<uint, MapEntity>();
        
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
                    component.Update(meshModel);
                }
                else if (baseComponent is EntityPolygonColliderComponentModel polygonCollisionModel)
                {
                    var component = componentObject.AddComponent<PolygonColliderComponent>();
                    component.Update(polygonCollisionModel);
                }
                else if (baseComponent is EntityCircleColliderComponentModel circleCollisionModel)
                {
                    var component = componentObject.AddComponent<CircleColliderComponent>();
                    component.Update(circleCollisionModel);
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

            return entity;
        }

        public void RemoveEntity(MapEntity entity)
        {
            if (entities.Remove(entity.Id))
                Destroy(entity.gameObject);
        }

        private void Update()
        {
            foreach (var kv in entities)
            {
                //kv.Value.GetComponent<Rigidbody2D>().MovePosition(new Vector2(5f * Mathf.Sin(Time.time / 5f), kv.Value.transform.position.y));

                var meshObject = kv.Value.GetComponentInChildren<MeshRenderer>().transform.parent.gameObject;
                //Debug.Log($"{meshObject.name} {meshObject.transform.position}");
            }
        }
    }
}
