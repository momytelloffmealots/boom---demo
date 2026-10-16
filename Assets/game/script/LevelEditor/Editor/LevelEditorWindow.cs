using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class LevelEditorWindow : EditorWindow
{
    public enum EditMode
    {
        Place,
        Erase,
        Rotate,
        RotateVertical
    }

    [SerializeField] private BlockPalette palette;
    private Vector2 scroll;
    private GameObject selectedPrefab;
    [SerializeField] private EditMode currentMode = EditMode.Place;

    // --- LEVEL SETTINGS ---
    [SerializeField] private int maxAmmo = 10;
    [SerializeField] private LevelDifficulty difficulty = LevelDifficulty.Normal;
    [SerializeField] private float minWinWeight = 10f;
    [SerializeField] private float maxWinWeight = 20f;

    public EditMode CurrentMode => currentMode;

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

        currentMode = (EditMode)GUILayout.Toolbar((int)currentMode, new string[] {
            "Place (Thêm)",
            "Erase (Xóa)",
            "Rotate (Xoay)",
            "Rotate Vertical (Xoay Ngang)"
        });

        GUILayout.Space(10);

        // --- PALETTE & MODES ---
        if (currentMode == EditMode.Place)
        {
            palette = (BlockPalette)EditorGUILayout.ObjectField("Palette", palette, typeof(BlockPalette), false);
            GUILayout.Space(10);

            if (palette == null)
            {
                EditorGUILayout.HelpBox("Please assign a Block Palette asset.", MessageType.Info);
                return;
            }

            GUILayout.Label("Blocks");
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));

            foreach (GameObject prefab in palette.prefabs)
            {
                if (prefab == null) continue;

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
                selectedPrefab == null ? "Selected : None" : $"Selected : {selectedPrefab.name}",
                MessageType.None);
        }
        else if (currentMode == EditMode.Erase)
        {
            EditorGUILayout.HelpBox("Erase Mode: Click any block in the Scene View to delete it.", MessageType.Info);
        }
        else if (currentMode == EditMode.Rotate)
        {
            EditorGUILayout.HelpBox("Rotate Mode: Click any block in the Scene View to rotate it around Y axis.", MessageType.Info);
        }
        else if (currentMode == EditMode.RotateVertical)
        {
            EditorGUILayout.HelpBox("Rotate Vertical Mode: Click any block in the Scene View to rotate it around X axis.", MessageType.Info);
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

        GUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        if (GUILayout.Button("Export Level (JSON)", GUILayout.Height(30)))
        {
            ExportLevelToJSON();
        }

        GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f);
        if (GUILayout.Button("Import Level (JSON)", GUILayout.Height(30)))
        {
            ImportLevelFromJSON();
        }

        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
    }

    public GameObject GetSelectedPrefab() => selectedPrefab;

    private void ExportLevelToJSON()
    {
        if (palette == null || palette.prefabs == null || palette.prefabs.Count == 0)
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng gán Block Palette trước khi xuất JSON!", "OK");
            return;
        }

        LevelData levelData = new LevelData
        {
            MaxBullets = maxAmmo,
            Difficulty = difficulty,
            minWinWeight = minWinWeight,
            maxWinWeight = maxWinWeight
        };

        HashSet<string> validPrefabNames = new HashSet<string>();
        foreach (var p in palette.prefabs)
        {
            if (p != null) validPrefabNames.Add(p.name);
        }

        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            GameObject rootInstance = PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
            if (rootInstance != obj) continue;

            GameObject prefabParent = PrefabUtility.GetCorrespondingObjectFromSource(obj);
            if (prefabParent != null && validPrefabNames.Contains(prefabParent.name))
            {
                levelData.blocks.Add(new BlockData(
                    prefabParent.name,
                    obj.transform.position,
                    obj.transform.eulerAngles
                ));
            }
        }

        if (levelData.blocks.Count == 0)
        {
            EditorUtility.DisplayDialog("Thông báo", "Không tìm thấy Block hợp lệ nào trong Scene để xuất!", "OK");
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
            if (p != null && !lookup.ContainsKey(p.name)) lookup.Add(p.name, p);
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
}