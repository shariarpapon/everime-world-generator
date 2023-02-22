using UnityEngine;
using UnityEditor;

namespace Everime.WorldGeneration.CustomEditors
{
    [CustomEditor(typeof(WorldMaster))]
    public class WorldMasterEditor : Editor
    {
        private static bool WorldEditorEnabled = true;
        private static bool WorldSettingsFoldout = false;

        private WorldMaster worldMaster;
        private Editor worldSettingsEditor;

        private void OnEnable()
        {
            worldMaster = target as WorldMaster;
            worldSettingsEditor = CreateEditor(worldMaster.worldSettings);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawLine(1);

            WorldEditorEnabled = EditorGUILayout.Toggle("Enable World Editor", WorldEditorEnabled);
            if (WorldEditorEnabled)
            {
                DrawWorldEditor();
                EditorGUILayout.Space();
            }
        }

        private void DrawWorldEditor() 
        {
            if (worldMaster.worldSettings == null)
            {
                GUI.contentColor = Color.yellow;
                GUILayout.Label("Please assign world settings.");
                GUI.contentColor = Color.white;
                return;
            }   
            EditorGUILayout.Space();

            if(worldSettingsEditor == null && worldMaster.worldSettings != null)
                worldSettingsEditor = CreateEditor(worldMaster.worldSettings);

            WorldSettingsFoldout = EditorGUILayout.Foldout(WorldSettingsFoldout, "World Settings", true);
            if (WorldSettingsFoldout)
            {
                worldSettingsEditor.DrawDefaultInspector();
            }

            EditorGUILayout.Space();
            DrawLine();

            GUI.contentColor = Color.yellow;
            GUILayout.Label("WARNING!");
            GUI.contentColor = Color.white;
            GUILayout.Label("Creating a world will delete any existing world linked to this World Master.");
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create World"))
                worldMaster.GenerateWorld();
            if (GUILayout.Button("Delete World"))
                worldMaster.ClearExistingWorld();
            EditorGUILayout.EndHorizontal();

            if (worldMaster.world != null && worldMaster.IsGeneratingWorld == false)
            {
                EditorGUILayout.Space();
                GUILayout.Label("(De)Activating chunks will only work properly if chunk visibility updater is disabled.");
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Activate All Chunks"))
                    worldMaster.SetChunkVisibility(true);
                if (GUILayout.Button("Deactivate All Chunks"))
                    worldMaster.SetChunkVisibility(false);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawLine(int height = 1)
        {
            GUILayout.Space(4);
            Rect rect = GUILayoutUtility.GetRect(10, height, GUILayout.ExpandWidth(true));
            rect.height = height;
            rect.xMin = 0;
            rect.xMax = EditorGUIUtility.currentViewWidth;

            Color lineColor = new Color(0.10196f, 0.10196f, 0.10196f, 1);
            EditorGUI.DrawRect(rect, lineColor);
            GUILayout.Space(4);
        }

    }
}
