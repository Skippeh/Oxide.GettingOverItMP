using System.Collections;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components
{
    public class PlayerDebug : MonoBehaviour
    {
        private int textDepth = 0;
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                textDepth += 1;

                if (textDepth > 13)
                    textDepth = 13;
            }
            else if (Input.GetKeyDown(KeyCode.F1))
            {
                textDepth -= 1;

                if (textDepth < 0)
                    textDepth = 0;
            }
        }

        private void OnGUI()
        {
            if (textDepth > 0)
            {
                GUIStyle style = GUI.skin.GetStyle("Label");
                Color originalColor = GUI.color;

                DrawTextOnGameObjects(new[] {gameObject}, style);

                GUI.color = originalColor;
            }
        }

        private void DrawTextOnGameObjects(IEnumerable<GameObject> enumerable, GUIStyle style, int depth = 0)
        {
            foreach (GameObject go in enumerable)
            {
                if (go == null)
                    continue;

                DrawTextOnGameObject(go, depth, style);

                if (depth >= textDepth)
                    continue;

                List<GameObject> children = new List<GameObject>();
                foreach (Transform child in go.transform)
                {
                    children.Add(child.gameObject);
                }

                DrawTextOnGameObjects(children, style, depth + 1);
            }
        }

        private void DrawTextOnGameObject(GameObject gameObject, int depth, GUIStyle style)
        {
            string text = $"{gameObject.name} ({depth} - {gameObject.transform.rotation.eulerAngles.z})";
            Vector3 screenPosition = Camera.current.WorldToScreenPoint(gameObject.transform.position);
            Vector2 size = style.CalcSize(new GUIContent(text));
            Color textColor = Color.white;

            textColor.a = 1;
            int difference = depth - textDepth;
            float alpha = Mathf.Abs(difference) * (1 / 2f);
            alpha = Mathf.Clamp01(alpha);
            alpha = 1f - alpha;
            
            if (alpha > 0)
            {
                textColor.a = alpha;
                GUI.color = textColor;
                GUI.Label(new Rect(screenPosition.x, Screen.height - screenPosition.y, size.x, size.y), text);
            }
        }
    }
}
