using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelEditorWindow : EditorWindow
{
    private const string EditorScenePath = "Assets/Scenes/editor.unity";

    public enum EditMode
    {
        Place,
        Erase,
        Rotate,
        // Kept for serialized editor-window state created by older versions.
        RotateVertical,
        Select
    }

    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    [SerializeField] private BlockPalette palette;
    private Vector2 scroll;
    private GameObject selectedPrefab;
    [SerializeField] private EditMode currentMode = EditMode.Place;
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.Y;
    [SerializeField] private float rotationStepDegrees = 90f;
    [SerializeField] private float horizontalSnap = 0.25f;
    [SerializeField] private float verticalSnap = 0.5f;
    private GameObject rotationTarget;

    // --- LEVEL SETTINGS ---
    [SerializeField] private int maxAmmo = 10;
    [SerializeField] private LevelDifficulty difficulty = LevelDifficulty.Normal;
    [SerializeField] private float minWinWeight = 10f;
    [SerializeField] private float maxWinWeight = 20f;

    public EditMode CurrentMode => currentMode;
    public RotationAxis CurrentRotationAxis => rotationAxis;
    public float RotationStepDegrees => rotationStepDegrees;
    public float HorizontalSnap => horizontalSnap;
    public float VerticalSnap => verticalSnap;
    public GameObject RotationTarget => rotationTarget;

    private void OnEnable()
    {
        // Older versions stored X rotation as a separate mode with value 3.
        if (currentMode == EditMode.RotateVertical)
        {
            currentMode = EditMode.Rotate;
            rotationAxis = RotationAxis.X;
        }
    }

    [MenuItem("Tools/Level Editor")]
    public static void Open()
    {
        GetWindow<LevelEditorWindow>("Level Editor");
    }

    private void OnGUI()
    {
        GUILayout.Space(5);
        GUILayout.Label("LEVEL EDITOR", EditorStyles.boldLabel);
        GUILayout.Space(10);

        int modeIndex = GetModeToolbarIndex(currentMode);
        modeIndex = GUILayout.Toolbar(modeIndex, new string[] {
            "Chọn",
            "Đặt block",
            "Xóa block",
            "Xoay block"
        });
        currentMode = GetModeFromToolbarIndex(modeIndex);

        GUILayout.Space(10);

        // --- PALETTE & MODES ---
        if (currentMode == EditMode.Select)
        {
            EditorGUILayout.HelpBox("Chọn object trong Scene rồi dùng Move (W), Rotate (E) hoặc Scale (R) để chỉnh tự do.", MessageType.Info);
        }
        else if (currentMode == EditMode.Place)
        {
            palette = (BlockPalette)EditorGUILayout.ObjectField("Palette", palette, typeof(BlockPalette), false);
            GUILayout.Space(10);

            if (palette == null)
            {
                EditorGUILayout.HelpBox("Hãy gán Block Palette để chọn block.", MessageType.Info);
            }
            else
            {
                GUILayout.Label("Danh sách block");
                scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));

                foreach (GameObject prefab in palette.prefabs)
                {
                    if (!IsLevelPrefab(prefab)) continue;

                    bool selected = selectedPrefab == prefab;
                    GUI.backgroundColor = selected ? Color.green : Color.white;

                    if (GUILayout.Button(prefab.name, GUILayout.Height(35)))
                    {
                        selectedPrefab = prefab;
                    }
                }

                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndScrollView();

                GUILayout.Space(5);
                EditorGUILayout.HelpBox(
                    selectedPrefab == null ? "Đã chọn: Chưa có" : $"Đã chọn: {selectedPrefab.name}",
                    MessageType.None);
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Lưới căn block", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            horizontalSnap = Mathf.Max(0.01f, EditorGUILayout.FloatField("X/Z", horizontalSnap));
            verticalSnap = Mathf.Max(0.01f, EditorGUILayout.FloatField("Y", verticalSnap));
            EditorGUILayout.EndHorizontal();
        }
        else if (currentMode == EditMode.Erase)
        {
            EditorGUILayout.HelpBox("Nhấp vào block trong Scene View để xóa toàn bộ block. Có thể Undo bằng Ctrl+Z.", MessageType.Info);
        }
        else if (currentMode == EditMode.Rotate || currentMode == EditMode.RotateVertical)
        {
            EditorGUILayout.LabelField("Xoay block đang chọn", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("X", GUILayout.Height(30))) RotateTarget(RotationAxis.X);
            if (GUILayout.Button("Y", GUILayout.Height(30))) RotateTarget(RotationAxis.Y);
            if (GUILayout.Button("Z", GUILayout.Height(30))) RotateTarget(RotationAxis.Z);
            EditorGUILayout.EndHorizontal();
            rotationStepDegrees = Mathf.Clamp(EditorGUILayout.FloatField("Góc xoay (°)", rotationStepDegrees), 1f, 360f);
            EditorGUILayout.HelpBox("Chọn block trong Scene View, sau đó mỗi lần bấm X/Y/Z sẽ xoay block theo số độ bên dưới.", MessageType.Info);
            EditorGUILayout.HelpBox(
                rotationTarget == null ? "Block đang chọn: Chưa có" : $"Block đang chọn: {rotationTarget.name}",
                rotationTarget == null ? MessageType.Warning : MessageType.None);
        }

        // --- LEVEL SETTINGS ---
        GUILayout.Space(10);
        EditorGUILayout.LabelField("LEVEL SETTINGS", EditorStyles.boldLabel);
        maxAmmo = EditorGUILayout.IntField("Max Ammo (Số đạn)", Mathf.Max(1, maxAmmo));
        difficulty = (LevelDifficulty)EditorGUILayout.EnumPopup("Difficulty (Độ khó)", difficulty);

        // 🔥 MỚI: Chỉ hiện cài đặt cân nặng khi chọn chế độ Digital
        if (difficulty == LevelDifficulty.Digital)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("DIGITAL SCALE TARGET (KG)", EditorStyles.miniBoldLabel);
            minWinWeight = EditorGUILayout.FloatField("Min Win Weight", Mathf.Max(0f, minWinWeight));
            maxWinWeight = EditorGUILayout.FloatField("Max Win Weight", Mathf.Max(minWinWeight, maxWinWeight));
            EditorGUILayout.EndVertical();
        }

        // --- SAVE / LOAD SYSTEM ---
        GUILayout.Space(15);
        EditorGUILayout.LabelField("SAVE / LOAD SYSTEM", EditorStyles.boldLabel);

        if (LevelEditorPlaytestSession.IsActive)
        {
            EditorGUILayout.HelpBox("Đang Playtest level trong editor.unity với gameplay Core. Dừng Play Mode để quay lại chỉnh.", MessageType.Info);
            GUI.backgroundColor = new Color(0.8f, 0.35f, 0.25f);
            if (GUILayout.Button("Dừng Playtest", GUILayout.Height(30)))
                LevelEditorPlaytestSession.StopPlaytest();
            GUI.backgroundColor = Color.white;
        }
        else
        {
            GUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.25f, 0.65f, 0.9f);
            if (GUILayout.Button("▶ Playtest", GUILayout.Height(30)))
                PlaytestCurrentLevel();

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("Export Level (JSON)", GUILayout.Height(30)))
                ExportLevelToJSON();

            GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f);
            if (GUILayout.Button("Import Level (JSON)", GUILayout.Height(30)))
                ImportLevelFromJSON();

            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
        }
        GUILayout.Space(10);
    }

    public GameObject GetSelectedPrefab() => selectedPrefab;

    private static int GetModeToolbarIndex(EditMode mode)
    {
        switch (mode)
        {
            case EditMode.Place: return 1;
            case EditMode.Erase: return 2;
            case EditMode.Rotate:
            case EditMode.RotateVertical: return 3;
            default: return 0;
        }
    }

    private static EditMode GetModeFromToolbarIndex(int index)
    {
        switch (index)
        {
            case 1: return EditMode.Place;
            case 2: return EditMode.Erase;
            case 3: return EditMode.Rotate;
            default: return EditMode.Select;
        }
    }

    public GameObject ResolveEditableTarget(GameObject hitObject)
    {
        if (hitObject == null || PrefabUtility.IsPartOfPrefabAsset(hitObject))
            return null;

        Block block = hitObject.GetComponentInParent<Block>();
        if (block != null)
        {
            GameObject blockPrefabRoot = FindPalettePrefabRoot(block.gameObject);
            return blockPrefabRoot != null ? blockPrefabRoot : block.gameObject;
        }

        return FindPalettePrefabRoot(hitObject);
    }

    public bool SelectRotationTarget(GameObject hitObject)
    {
        rotationTarget = ResolveEditableTarget(hitObject);
        if (rotationTarget == null)
            return false;

        Selection.activeGameObject = rotationTarget;
        Repaint();
        SceneView.RepaintAll();
        return true;
    }

    private GameObject FindPalettePrefabRoot(GameObject objectInInstance)
    {
        if (palette == null || palette.prefabs == null)
            return null;

        GameObject nearestRoot = PrefabUtility.GetNearestPrefabInstanceRoot(objectInInstance);
        if (IsPalettePrefabInstance(nearestRoot))
            return nearestRoot;

        GameObject outermostRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(objectInInstance);
        if (outermostRoot != nearestRoot && IsPalettePrefabInstance(outermostRoot))
            return outermostRoot;

        return null;
    }

    private bool IsPalettePrefabInstance(GameObject instanceRoot)
    {
        if (instanceRoot == null)
            return false;

        string instancePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instanceRoot);
        if (string.IsNullOrEmpty(instancePath))
            return false;

        foreach (GameObject palettePrefab in palette.prefabs)
        {
            if (palettePrefab != null && AssetDatabase.GetAssetPath(palettePrefab) == instancePath)
                return true;
        }

        return false;
    }

    private void RotateTarget(RotationAxis axis)
    {
        rotationAxis = axis;

        if (Selection.activeGameObject != null)
        {
            GameObject selectedTarget = ResolveEditableTarget(Selection.activeGameObject);
            if (selectedTarget != null)
                rotationTarget = selectedTarget;
        }

        if (rotationTarget == null)
        {
            ShowNotification(new GUIContent("Hãy chọn block trong Scene trước."));
            return;
        }

        Transform target = rotationTarget.transform;
        Undo.RecordObject(target, $"Rotate Block {axis}");
        target.Rotate(GetRotationVector(axis), rotationStepDegrees, Space.Self);
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        Selection.activeGameObject = rotationTarget;
        SceneView.RepaintAll();
        Repaint();
    }

    private static Vector3 GetRotationVector(RotationAxis axis)
    {
        switch (axis)
        {
            case RotationAxis.X: return Vector3.right;
            case RotationAxis.Z: return Vector3.forward;
            default: return Vector3.up;
        }
    }

    private void ExportLevelToJSON()
    {
        if (palette == null || palette.prefabs == null || palette.prefabs.Count == 0)
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng gán Block Palette trước khi xuất JSON!", "OK");
            return;
        }

        LevelData levelData = BuildCurrentLevelData();

        if (levelData.blocks.Count == 0)
        {
            EditorUtility.DisplayDialog("Thông báo", "Không tìm thấy block hoặc kệ hợp lệ trong Scene Editor để xuất!", "OK");
            return;
        }

        string path = EditorUtility.SaveFilePanel("Save Level JSON", Application.dataPath, "Level_1", "json");
        if (!string.IsNullOrEmpty(path))
        {
            levelData.levelName = Path.GetFileNameWithoutExtension(path);
            string json = JsonUtility.ToJson(levelData, true);
            File.WriteAllText(path, json);
            AssetDatabase.Refresh();
            Debug.Log($"[LevelEditor] Đã xuất Level thành công: {path}");
        }
    }

    private void PlaytestCurrentLevel()
    {
        if (palette == null || palette.prefabs == null || palette.prefabs.Count == 0)
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng gán Block Palette trước khi chạy thử.", "OK");
            return;
        }

        Scene editingScene = SceneManager.GetActiveScene();
        if (!editingScene.IsValid() || !editingScene.isLoaded || editingScene.path != EditorScenePath)
        {
            EditorUtility.DisplayDialog("Playtest", "Hãy mở Assets/Scenes/editor.unity và đặt Scene này làm active để chạy thử layout.", "OK");
            return;
        }

        LevelData levelData = BuildCurrentLevelData();
        if (levelData.blocks.Count == 0)
        {
            EditorUtility.DisplayDialog("Thông báo", "Hãy đặt ít nhất một block hoặc kệ trong editor.unity trước khi chạy thử.", "OK");
            return;
        }

        levelData.levelName = "EditorPlaytest";
        if (LevelEditorPlaytestSession.TryStartPlaytest(levelData))
        {
            ShowNotification(new GUIContent("Playtest đang mở bằng gameplay Core."));
            Repaint();
        }
    }

    private LevelData BuildCurrentLevelData()
    {
        LevelData levelData = new LevelData
        {
            levelName = "CustomLevel",
            MaxBullets = Mathf.Max(1, maxAmmo),
            Difficulty = difficulty,
            minWinWeight = minWinWeight,
            maxWinWeight = maxWinWeight
        };

        if (palette == null || palette.prefabs == null)
            return levelData;

        HashSet<string> validPrefabNames = new HashSet<string>();
        foreach (GameObject prefab in palette.prefabs)
        {
            if (IsLevelPrefab(prefab))
                validPrefabNames.Add(prefab.name);
        }

        Scene editingScene = SceneManager.GetActiveScene();
        HashSet<GameObject> collectedRoots = new HashSet<GameObject>();
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.scene != editingScene)
                continue;

            GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
            if (prefabRoot == null || prefabRoot != obj || !collectedRoots.Add(prefabRoot))
                continue;

            GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(prefabRoot);
            if (prefabSource == null || !validPrefabNames.Contains(prefabSource.name))
                continue;

            levelData.blocks.Add(new BlockData(
                prefabSource.name,
                prefabRoot.transform.position,
                prefabRoot.transform.eulerAngles));
        }

        return levelData;
    }

    private void ImportLevelFromJSON()
    {
        if (palette == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng gán Block Palette trước khi Import!", "OK");
            return;
        }

        string path = EditorUtility.OpenFilePanel("Load Level JSON", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path)) return;

        string json = File.ReadAllText(path);
        LevelData data = JsonUtility.FromJson<LevelData>(json);
        if (data == null || data.blocks == null) return;

        maxAmmo = data.MaxBullets;
        difficulty = data.Difficulty;
        minWinWeight = data.minWinWeight;
        maxWinWeight = data.maxWinWeight;

        Dictionary<string, GameObject> lookup = new Dictionary<string, GameObject>();
        foreach (var p in palette.prefabs)
        {
            if (IsLevelPrefab(p) && !lookup.ContainsKey(p.name)) lookup.Add(p.name, p);
        }

        foreach (BlockData bData in data.blocks)
        {
            if (lookup.TryGetValue(bData.prefabName, out GameObject prefab))
            {
                GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (go != null)
                {
                    go.transform.position = bData.position;
                    go.transform.eulerAngles = bData.rotation;
                    Undo.RegisterCreatedObjectUndo(go, "Import Level Block");
                }
            }
        }

        Repaint();
    }

    private static bool IsLevelPrefab(GameObject prefab)
    {
        return prefab != null &&
               prefab.GetComponentInChildren<SimpleCannon>(true) == null &&
               prefab.GetComponentInChildren<SimpleBulletPool>(true) == null;
    }
}
