using UnityEngine;
using UnityEditor;

namespace Everime.WorldManagement.CustomEditors
{
    [CustomEditor(typeof(WorldMaster))]
    public class WorldMasterEditor : Editor
    {
        private static bool WorldEditorEnabled = true;
        private static bool WorldSettingsFoldout = false;
        private static bool AutoUpdate = false;

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
            EditorGUILayout.Space();

            WorldSettingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(WorldSettingsFoldout, "World Settings");
    
            if(worldSettingsEditor == null)
                CreateEditor(worldMaster.worldSettings);

            if (WorldSettingsFoldout)
                worldSettingsEditor.DrawDefaultInspector();

            EditorGUILayout.Space();
            AutoUpdate = EditorGUILayout.Toggle("Auto Update", AutoUpdate);

            if (AutoUpdate) 
            {
                int size = worldMaster.worldSettings.worldUnitSize;
                if (size <= 100) worldMaster.GenerateWorld();
                else 
                {
                    Debug.LogError("Cannot auto update if world size exceeds 100 units!");
                    AutoUpdate = false; 
                }
            }
            else
            {
                if (GUILayout.Button("Create World"))
                    worldMaster.GenerateWorld();
                if (GUILayout.Button("Delete World"))
                    worldMaster.ClearExistingWorld();
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
