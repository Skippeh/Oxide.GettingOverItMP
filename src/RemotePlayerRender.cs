using System.Linq;
using Oxide.Core;
using Oxide.GettingOverIt.Types;
using UnityEngine;
using UnityEngine.PostProcessing;

namespace Oxide.GettingOverIt
{
    public class RemotePlayerRender : MonoBehaviour
    {
        public static RemotePlayerRender Instance => instance;
        private static RemotePlayerRender instance;

        public Camera Camera => camera;
        private Camera camera;

        public static void Setup()
        {
            if (instance)
                return;

            var go = new GameObject("GOIMP.RemotePlayerRender");
            go.transform.SetParent(Camera.main.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            instance = go.AddComponent<RemotePlayerRender>();
        }

        public static void Destroy()
        {
            if (!instance)
                return;

            Destroy(instance.gameObject);
            instance = null;
        }

        private void Update()
        {

        }

        private void Awake()
        {
            var main = Camera.main;

            camera = gameObject.AddComponent<Camera>();
            camera.cullingMask = (int) (LayerTypeFlags.Layer31 | LayerTypeFlags.Terrain); // This will render the terrain twice, but might be worth considering it adds shadows for remote players.
            camera.farClipPlane = 1000;
            camera.nearClipPlane = 1;
            camera.fieldOfView = main.fieldOfView;
            camera.orthographic = main.orthographic;
            camera.orthographicSize = main.orthographicSize;
            camera.clearFlags = CameraClearFlags.Nothing;
            camera.depth = main.depth - 0.5f;
            camera.enabled = false;

            var postProcessing = gameObject.AddComponent<PostProcessingBehaviour>();
            postProcessing.profile = ScriptableObject.CreateInstance<PostProcessingProfile>();
            postProcessing.profile.antialiasing = new AntialiasingModel
            {
                enabled = true,
                settings = new AntialiasingModel.Settings
                {
                    method = AntialiasingModel.Method.Fxaa,
                    fxaaSettings = AntialiasingModel.FxaaSettings.defaultSettings
                }
            };
        }

        private void OnEnable()
        {
            Camera.main.cullingMask &= ~(int) LayerTypeFlags.Layer31;
            camera.enabled = true;
            
            // Global light culling mask should include Layer31.
            var directionalLight = GameObject.FindObjectsOfType<Light>().First(light => light.type == LightType.Directional);
            directionalLight.cullingMask |= (int)LayerTypeFlags.Layer31;
        }

        private void OnDisable()
        {
            Camera.main.cullingMask |= (int) LayerTypeFlags.Layer31;
            camera.enabled = false;

            var directionalLight = GameObject.FindObjectsOfType<Light>().First(light => light.type == LightType.Directional);
            directionalLight.cullingMask &= ~(int) LayerTypeFlags.Layer31;
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(source, destination);
        }
    }
}
