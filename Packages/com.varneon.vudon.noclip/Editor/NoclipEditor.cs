using UnityEditor;
using UnityEngine;
using UdonSharpEditor;

namespace Varneon.VUdon.Noclip.Editor
{
    /// <summary>
    /// Simple editor for Noclip
    /// </summary>
    [CustomEditor(typeof(Noclip))]
    public class NoclipEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Check if this is a proxy
            UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Documentation", GUILayout.Height(30)))
            {
                Application.OpenURL("https://github.com/Varneon/VUdon-Noclip/wiki/Settings");
            }
            
            EditorGUILayout.Space();
            
            // Draw the default inspector
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
