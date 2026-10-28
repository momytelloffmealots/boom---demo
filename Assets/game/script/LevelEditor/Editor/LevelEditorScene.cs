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

        // In Select mode Unity owns Scene View picking and transform handles.
        if (mode == LevelEditorWindow.EditMode.Select)
            return;

        if (mode == LevelEditorWindow.EditMode.Place && prefab == null)
            return;

        Event e = Event.current;
        int controlID = GUIUtility.GetControlID(FocusType.Passive);

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Vector3 pos = Vector3.zero;
        Vector3 surfacePoint = Vector3.zero;
        Vector3 surfaceNormal = Vector3.up;
        bool hasHit = false;
        GameObject hitObject = null;
        GameObject targetBlock = null;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            hitObject = hit.collider.gameObject;
            surfacePoint = hit.point;
            surfaceNormal = hit.normal;
            if (mode == LevelEditorWindow.EditMode.Place)
            {
                // Offset by half a unit along the normal so block snaps to the outside of the hit face
                pos = surfacePoint + surfaceNormal * 0.25f;
            }
            else
            {
                // Resolve the palette item root. Shelves may not have a Block component,
                // while block colliders may live on child objects.
                targetBlock = window.ResolveEditableTarget(hitObject);
                if (targetBlock != null)
                {
                    pos = targetBlock.transform.position;
                }
                else
                {
                    pos = hitObject.transform.position;
                }
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
                surfacePoint = pos;
                surfaceNormal = Vector3.up;
                hasHit = true;
            }
        }

        // Let normal Scene View selection work when the pointer is over scenery,
        // and capture the click only when this tool can act on its target.
        if (e.type == EventType.Layout && (mode == LevelEditorWindow.EditMode.Place || targetBlock != null))
        {
            HandleUtility.AddDefaultControl(controlID);
        }

        if ((mode == LevelEditorWindow.EditMode.Rotate || mode == LevelEditorWindow.EditMode.RotateVertical) && window.RotationTarget != null)
        {
            Handles.color = GetRotationColor(window.CurrentRotationAxis);
            DrawBlockBounds(window.RotationTarget, window.RotationTarget.transform.position, Vector3.one * 0.5f);
        }

        if (!hasHit) return;

        if (mode == LevelEditorWindow.EditMode.Place)
        {
            float stepXZ = Mathf.Max(0.01f, window.HorizontalSnap);
            float stepY = Mathf.Max(0.01f, window.VerticalSnap);

            // Nẩy tâm block khỏi bề mặt nửa bước lưới rồi căn vị trí theo lưới.
            Vector3 rawPos = surfacePoint + new Vector3(
                surfaceNormal.x * (stepXZ / 2f),
                surfaceNormal.y * (stepY / 2f),
                surfaceNormal.z * (stepXZ / 2f)
            );

            // X/Z and Y can use different grid steps for shelves and stacked blocks.
            pos.x = Mathf.Round(rawPos.x / stepXZ) * stepXZ;
            pos.z = Mathf.Round(rawPos.z / stepXZ) * stepXZ;
            pos.y = Mathf.Round(rawPos.y / stepY) * stepY;
        }

        Vector3 previewSize = new Vector3(window.HorizontalSnap, window.VerticalSnap, window.HorizontalSnap);
        if (mode == LevelEditorWindow.EditMode.Place)
        {
            Handles.color = Color.green;
            Handles.DrawWireCube(pos, previewSize);
        }
        else if (mode == LevelEditorWindow.EditMode.Erase)
        {
            if (targetBlock != null)
            {
                Handles.color = Color.red;
                DrawBlockBounds(targetBlock, pos, previewSize);
            }
        }
        else if (mode == LevelEditorWindow.EditMode.Rotate || mode == LevelEditorWindow.EditMode.RotateVertical)
        {
            if (window.RotationTarget == null && targetBlock != null)
            {
                Handles.color = GetRotationColor(window.CurrentRotationAxis);
                DrawBlockBounds(targetBlock, pos, previewSize);
            }
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
                    if (prefab.GetComponentInChildren<DigitalScale>(true) != null)
                        go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                    Undo.RegisterCreatedObjectUndo(go, "Place Block");
                    e.Use();
                }
            }
            else if (mode == LevelEditorWindow.EditMode.Erase && targetBlock != null)
            {
                Undo.DestroyObjectImmediate(targetBlock);
                e.Use();
            }
            else if ((mode == LevelEditorWindow.EditMode.Rotate || mode == LevelEditorWindow.EditMode.RotateVertical) && targetBlock != null)
            {
                window.SelectRotationTarget(hitObject);
                e.Use();
            }
        }
    }

    private static Color GetRotationColor(LevelEditorWindow.RotationAxis axis)
    {
        switch (axis)
        {
            case LevelEditorWindow.RotationAxis.X: return new Color(1f, 0.35f, 0.35f);
            case LevelEditorWindow.RotationAxis.Z: return new Color(0.35f, 0.65f, 1f);
            default: return Color.cyan;
        }
    }

    private static void DrawBlockBounds(GameObject block, Vector3 fallbackCenter, Vector3 fallbackSize)
    {
        Renderer[] renderers = block.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            Handles.DrawWireCube(bounds.center, bounds.size);
            return;
        }

        Collider[] colliders = block.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            Bounds bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                bounds.Encapsulate(colliders[i].bounds);
            }

            Handles.DrawWireCube(bounds.center, bounds.size);
            return;
        }

        Handles.DrawWireCube(fallbackCenter, fallbackSize);
    }
}
