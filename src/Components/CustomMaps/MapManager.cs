using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Oxide.GettingOverItMP.Debugging;
using ServerShared.CustomMaps;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components.CustomMaps
{
    public class MapManager : MonoBehaviour
    {
        public static readonly Dictionary<string, GameObject> ObjectPrefabs = new Dictionary<string, GameObject>();

        private static readonly Regex numberSuffixRegex = new Regex(@"(\s*(\(\d+\)\s*))+$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static readonly List<GameObject> defaultMapObjects = new List<GameObject>();
        private static Vector3 playerPosition;
        private static bool defaultMapEnabled = true;

        public GameMap CurrentMap { get; private set; }

        private GameObject mapObject;

        public static void LoadMapObjects()
        {
            var resourceObjects = Resources.FindObjectsOfTypeAll<GameObject>().Where(go => go.GetComponentInChildren<MeshRenderer>() != null);
            var allGameObjects = resourceObjects.Where(go => !go.name.ToLower().Contains("lod") && (go.GetComponent<MeshFilter>()?.sharedMesh.isReadable ?? false))
                .Where(go =>
                {
                    string pathString = go.ToHierarchicalString();
                    return pathString.StartsWith("Mountain") || pathString.StartsWith("Props");
                }).ToList();

            var uniqueGameObjects = new Dictionary<string, GameObject>();

            defaultMapObjects.AddRange(new[]
            {
                GameObject.Find("Mountain"),
                GameObject.Find("Mountain_NoCollide"),
                GameObject.Find("Props"),
                GameObject.Find("Background"),
                GameObject.Find("Water"),
                //GameObject.Find("Splashes"),
                GameObject.Find("Ground"),
                GameObject.Find("Snake"),
                GameObject.Find("Invisible Walls"),
                GameObject.Find("LevelSpline"),
                GameObject.Find("ZeroGrav"),
                GameObject.Find("AmbientSounds")
            });
            
            foreach (var go in allGameObjects)
            {
                var path = go.ToHierarchicalString();
                path = RemoveNumberSuffix(path);

                if (uniqueGameObjects.ContainsKey(path))
                    continue;

                uniqueGameObjects.Add(path, go);
            }

            foreach (GameObject gameObject in uniqueGameObjects.Values)
            {
                string key = RemoveNumberSuffix(gameObject.ToHierarchicalString());

                GameObject prefab = Instantiate(gameObject);
                prefab.SetActive(false);
                prefab.transform.position = Vector3.zero;
                prefab.transform.rotation = Quaternion.identity;
                prefab.transform.localScale = Vector3.one;
                
                ObjectPrefabs.Add(key, prefab);
            }

            var cubePrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubePrefab.SetActive(false);
            DestroyImmediate(cubePrefab.GetComponent<Collider>());
            ObjectPrefabs.Add("GOIMP/Cube", cubePrefab);

            var spherePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spherePrefab.SetActive(false);
            DestroyImmediate(spherePrefab.GetComponent<Collider>());
            ObjectPrefabs.Add("GOIMP/Sphere", spherePrefab);
        }

        public static void DestroyMapObjects()
        {
            foreach (var kv in ObjectPrefabs)
            {
                GameObject.Destroy(kv.Value);
            }

            ObjectPrefabs.Clear();

            EnableDefaultMap();
            defaultMapObjects.Clear();
        }

        public static void DisableDefaultMap()
        {
            if (!defaultMapEnabled)
                return;

            foreach (var obj in defaultMapObjects)
                obj.SetActive(false);

            playerPosition = GameObject.Find("Player").transform.position;
        }

        public static void EnableDefaultMap()
        {
            if (defaultMapEnabled)
                return;

            foreach (var obj in defaultMapObjects)
                obj.SetActive(true);

            GameObject.Find("Player").transform.position = playerPosition;
        }

        private static string RemoveNumberSuffix(string path)
        {
            return numberSuffixRegex.Replace(path, string.Empty);
        }

        public void LoadMap(GameMapModel model)
        {
            DisableDefaultMap();

            if (CurrentMap != null)
                UnloadMap();

            mapObject = new GameObject("GOIMP.Map");
            CurrentMap = mapObject.AddComponent<GameMap>();

            foreach (MapEntityModel entityModel in model.Entities)
            {
                var entity = CurrentMap.SpawnEntity(entityModel);
                //entity.gameObject.AddComponent<PolygonColliderVisualizer>();
            }
        }

        public void UnloadMap(bool enableDefaultMap = false)
        {
            Destroy(mapObject);
            mapObject = null;
            CurrentMap = null;

            if (enableDefaultMap)
                EnableDefaultMap();
        }
    }
}
