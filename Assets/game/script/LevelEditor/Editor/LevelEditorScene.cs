using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class LevelEditorScene
{
    static LevelEditorScene()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        // Avoid opening/focusing the window if it's closed
        LevelEditorWindow[] windows = Resources.FindObjectsOfTypeAll<LevelEditorWindow>();
        if (windows == null || windows.Length == 0)
            return;

        LevelEditorWindow window = windows[0];
        if (window == null)
            return;

        LevelEditorWindow.EditMode mode = window.CurrentMode;
        GameObject prefab = window.GetSelectedPrefab();

        if (mode == LevelEditorWindow.EditMode.Place && prefab == null)
            return;

        Event e = Event.current;
        int controlID = GUIUtility.GetControlID(FocusType.Passive);

        // Prevent default Unity selection when mouse clicked in scene
        if (e.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(controlID);
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Vector3 pos = Vector3.zero;
        bool hasHit = false;
        GameObject hitObject = null;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            hitObject = hit.collider.gameObject;
            if (mode == LevelEditorWindow.EditMode.Place)
            {
                // Offset by half a unit along the normal so block snaps to the outside of the hit face
                pos = hit.point + hit.normal * 0.55f;
            }
            else
            {
                // Target the hit object directly for erase/rotate
                pos = hitObject.transform.position;
            }
            hasHit = true;
        }
        else if (mode == LevelEditorWindow.EditMode.Place)
        {
            // Fallback plane at Y = 0 (only for placing blocks)
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float enter))
            {
                pos = ray.GetPoint(enter);
                hasHit = true;
            }
        }

        if (!hasHit)
            return;

        if (mode == LevelEditorWindow.EditMode.Place)
        {
            float snapSize = 0.5f;
            pos.x = Mathf.Round(pos.x / snapSize) * snapSize;
            pos.y = Mathf.Round(pos.y / snapSize) * snapSize;
            pos.z = Mathf.Round(pos.z / snapSize) * snapSize;
        }

        // Draw preview wire cube based on active mode
        if (mode == LevelEditorWindow.EditMode.Place)
        {
            Handles.color = Color.green;
            Handles.DrawWireCube(pos, Vector3.one);
        }
        else if (mode == LevelEditorWindow.EditMode.Erase)
        {
            Handles.color = Color.red;
            Handles.DrawWireCube(pos, Vector3.one * 1.05f);
        }
        else if (mode == LevelEditorWindow.EditMode.Rotate)
        {
            Handles.color = Color.cyan;
            Handles.DrawWireCube(pos, Vector3.one * 1.05f);
        }
        else if (mode == LevelEditorWindow.EditMode.RotateVertical)
        {
            Handles.color = Color.yellow;
            Handles.DrawWireCube(pos, Vector3.one * 1.05f);
        }

        // Repaint scene view when mouse moves to update preview position without infinite loop
        if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
        {
            sceneView.Repaint();
        }

        if (e.type == EventType.MouseDown &&
            e.button == 0 &&
            !e.alt)
        {
            if (mode == LevelEditorWindow.EditMode.Place)
            {
                GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (go != null)
                {
                    go.transform.position = pos;
                    Undo.RegisterCreatedObjectUndo(go, "Place Block");
                }
            }
            else if (mode == LevelEditorWindow.EditMode.Erase && hitObject != null)
            {
                Undo.DestroyObjectImmediate(hitObject);
            }
            else if (mode == LevelEditorWindow.EditMode.Rotate && hitObject != null)
            {
                Undo.RecordObject(hitObject.transform, "Rotate Block");
                hitObject.transform.root.Rotate(0, 45, 0);
            }
            else if (mode == LevelEditorWindow.EditMode.RotateVertical && hitObject != null)
            {
                Undo.RecordObject(hitObject.transform, "Rotate Block Vertically");
                hitObject.transform.root.Rotate(45, 0, 0);
            }
            e.Use();
        }
    }
}