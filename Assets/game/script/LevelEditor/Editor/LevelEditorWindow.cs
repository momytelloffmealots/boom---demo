//using System.Collections.Generic;
//using System.IO;
//using UnityEditor;
//using UnityEngine;

//#region Data Schema for JSON
//[System.Serializable]
//public struct BlockData
//{
//    public string prefabName;
//    public Vector3 position;
//    public Vector3 rotation;

//    public BlockData(string name, Vector3 pos, Vector3 rot)
//    {
//        prefabName = name;
//        position = pos;
//        rotation = rot;
//    }
//}

//[System.Serializable]
//public class LevelData
//{
//    public string levelName = "CustomLevel";
//    public List<BlockData> blocks = new List<BlockData>();
//}
//#endregion

//public class LevelEditorWindow : EditorWindow
//{
//    public enum EditMode
//    {
//        Place,
//        Erase,
//        Rotate,
//        RotateVertical 
//    }

//    [SerializeField]
//    private BlockPalette palette;

//    private Vector2 scroll;
//    private GameObject selectedPrefab;

//    [SerializeField]
//    private EditMode currentMode = EditMode.Place;

//    public EditMode CurrentMode => currentMode;

//    [MenuItem("Tools/Level Editor")]
//    public static void Open()
//    {
//        GetWindow<LevelEditorWindow>("Level Editor");
//    }

//    private void OnGUI()
//    {
//        GUILayout.Space(5);
//        GUILayout.Label("LEVEL EDITOR", EditorStyles.boldLabel);
//        GUILayout.Space(10);

//        currentMode = (EditMode)GUILayout.Toolbar((int)currentMode, new string[] {
//            "Place (Thêm)",
//            "Erase (Xóa)",
//            "Rotate (Xoay)",
//            "Rotate Vertical (Xoay Ngang)"});

//        GUILayout.Space(10);

//        // --- PALETTE & MODES ---
//        if (currentMode == EditMode.Place)
//        {
//            palette = (BlockPalette)EditorGUILayout.ObjectField(
//                "Palette",
//                palette,
//                typeof(BlockPalette),
//                false);

//            GUILayout.Space(10);

//            if (palette == null)
//            {
//                EditorGUILayout.HelpBox(
//                    "Please assign a Block Palette asset.",
//                    MessageType.Info);
//                return;
//            }

//            GUILayout.Label("Blocks");
//            scroll = EditorGUILayout.BeginScrollView(scroll);

//            foreach (GameObject prefab in palette.prefabs)
//            {
//                if (prefab == null)
//                    continue;

//                bool selected = selectedPrefab == prefab;

//                GUI.backgroundColor = selected ? Color.green : Color.white;

//                if (GUILayout.Button(prefab.name, GUILayout.Height(35)))
//                {
//                    selectedPrefab = prefab;
//                }
//            }

//            GUI.backgroundColor = Color.white;
//            EditorGUILayout.EndScrollView();

//            GUILayout.Space(10);

//            EditorGUILayout.HelpBox(
//                selectedPrefab == null
//                    ? "Selected : None"
//                    : $"Selected : {selectedPrefab.name}",
//                MessageType.None);
//        }
//        else if (currentMode == EditMode.Erase)
//        {
//            EditorGUILayout.HelpBox(
//                "Erase Mode: Click any block in the Scene View to delete it.",
//                MessageType.Info);
//        }
//        else if (currentMode == EditMode.Rotate)
//        {
//            EditorGUILayout.HelpBox(
//                "Rotate Mode: Click any block in the Scene View to rotate it 90 degrees around Y axis.",
//                MessageType.Info);
//        }
//        else if (currentMode == EditMode.RotateVertical)
//        {
//            EditorGUILayout.HelpBox(
//                "Rotate Vertical Mode: Click any block in the Scene View to rotate it 90 degrees around X axis.",
//                MessageType.Info);
//        }

//        // --- NÚT XUẤT & TẢI JSON (JSON EXPORT / IMPORT) ---
//        GUILayout.Space(15);
//        EditorGUILayout.LabelField("SAVE / LOAD SYSTEM", EditorStyles.boldLabel);

//        GUILayout.BeginHorizontal();

//        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f); // Green
//        if (GUILayout.Button("Export Level (JSON)", GUILayout.Height(30)))
//        {
//            ExportLevelToJSON();
//        }

//        GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f); // Blue
//        if (GUILayout.Button("Import Level (JSON)", GUILayout.Height(30)))
//        {
//            ImportLevelFromJSON();
//        }

//        GUI.backgroundColor = Color.white;
//        GUILayout.EndHorizontal();
//    }

//    public GameObject GetSelectedPrefab()
//    {
//        return selectedPrefab;
//    }

//    #region JSON Export / Import Logic
//    private void ExportLevelToJSON()
//    {
//        LevelData levelData = new LevelData();

//        // Tìm tất cả các GameObjects trong Scene được tạo ra từ Prefab thuộc BlockPalette
//        if (palette == null || palette.prefabs == null || palette.prefabs.Count == 0)
//        {
//            EditorUtility.DisplayDialog("Lỗi", "Vui lòng gán Block Palette trước khi xuất JSON!", "OK");
//            return;
//        }

//        // Tạo HashSet lưu danh sách tên các Prefab hợp lệ có trong Palette
//        HashSet<string> validPrefabNames = new HashSet<string>();
//        foreach (var p in palette.prefabs)
//        {
//            if (p != null) validPrefabNames.Add(p.name);
//        }

//        // Quét toàn bộ Object trên Scene
//        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
//        foreach (GameObject obj in allObjects)
//        {
//            // ✅ CHỈ LẤY OBJECT GỐC NGLẠI CÙNG (Nếu obj là con bên trong thì bỏ qua)
//            GameObject rootInstance = PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
//            if (rootInstance != obj) continue;
//            // Kiểm tra xem Object có phải là Instance của Prefab trong Palette không
//            GameObject prefabParent = PrefabUtility.GetCorrespondingObjectFromSource(obj);
//            if (prefabParent != null && validPrefabNames.Contains(prefabParent.name))
//            {
//                BlockData bData = new BlockData(
//                    prefabParent.name,
//                    obj.transform.position,
//                    obj.transform.eulerAngles
//                );
//                levelData.blocks.Add(bData);
//            }
//        }

//        if (levelData.blocks.Count == 0)
//        {
//            EditorUtility.DisplayDialog("Thông báo", "Không tìm thấy Block hợp lệ nào trong Scene để xuất!", "OK");
//            return;
//        }

//        // Mở cửa sổ chọn vị trí lưu file
//        string path = EditorUtility.SaveFilePanel("Save Level JSON", Application.dataPath, "NewLevel", "json");
//        if (!string.IsNullOrEmpty(path))
//        {
//            string json = JsonUtility.ToJson(levelData, true);
//            File.WriteAllText(path, json);
//            AssetDatabase.Refresh();
//            EditorUtility.DisplayDialog("Thành công", $"Đã xuất {levelData.blocks.Count} blocks ra file JSON!", "OK");
//        }
//    }

//    private void ImportLevelFromJSON()
//    {
//        if (palette == null)
//        {
//            EditorUtility.DisplayDialog("Lỗi", "Vui lòng gán Block Palette trước khi Import JSON để Tool biết danh sách Prefab!", "OK");
//            return;
//        }

//        string path = EditorUtility.OpenFilePanel("Load Level JSON", Application.dataPath, "json");
//        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

//        string json = File.ReadAllText(path);
//        LevelData levelData = JsonUtility.FromJson<LevelData>(json);

//        if (levelData == null || levelData.blocks == null)
//        {
//            EditorUtility.DisplayDialog("Lỗi", "File JSON không đúng định dạng LevelData!", "OK");
//            return;
//        }

//        // Map danh sách Prefab name -> GameObject trong Palette
//        Dictionary<string, GameObject> prefabMap = new Dictionary<string, GameObject>();
//        foreach (var p in palette.prefabs)
//        {
//            if (p != null && !prefabMap.ContainsKey(p.name))
//                prefabMap.Add(p.name, p);
//        }

//        // Instantiate các Object ra Scene
//        int spawnedCount = 0;
//        foreach (BlockData bData in levelData.blocks)
//        {
//            if (prefabMap.TryGetValue(bData.prefabName, out GameObject prefab))
//            {
//                GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
//                if (go != null)
//                {
//                    go.transform.position = bData.position;
//                    go.transform.eulerAngles = bData.rotation;
//                    Undo.RegisterCreatedObjectUndo(go, "Import Level Block");
//                    spawnedCount++;
//                }
//            }
//        }

//        EditorUtility.DisplayDialog("Thành công", $"Đã Import thành công {spawnedCount} blocks vào Scene!", "OK");
//    }
//    #endregion
//}

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

#region Data Schema for JSON
[System.Serializable]
public struct BlockData
{
    public string prefabName;
    public Vector3 position;
    public Vector3 rotation;

    public BlockData(string name, Vector3 pos, Vector3 rot)
    {
        prefabName = name;
        position = pos;
        rotation = rot;
    }
}

[System.Serializable]
public class LevelData
{
    public string levelName = "CustomLevel";
    public int MaxBullets = 10;
    public List<BlockData> blocks = new List<BlockData>();
}
#endregion

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
    [SerializeField] private int levelMaxBullets = 10;

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
            "Rotate Vertical (Xoay Ngang)"});

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
            scroll = EditorGUILayout.BeginScrollView(scroll);

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

            GUILayout.Space(10);
            EditorGUILayout.HelpBox(selectedPrefab == null ? "Selected : None" : $"Selected : {selectedPrefab.name}", MessageType.None);
        }
        else if (currentMode == EditMode.Erase)
        {
            EditorGUILayout.HelpBox("Erase Mode: Click any block in the Scene View to delete it.", MessageType.Info);
        }
        else if (currentMode == EditMode.Rotate)
        {
            EditorGUILayout.HelpBox("Rotate Mode: Click any block in the Scene View to rotate it 90 degrees around Y axis.", MessageType.Info);
        }
        else if (currentMode == EditMode.RotateVertical)
        {
            EditorGUILayout.HelpBox("Rotate Vertical Mode: Click any block in the Scene View to rotate it 90 degrees around X axis.", MessageType.Info);
        }

        // --- LEVEL SETTINGS ---
        GUILayout.Space(10);
        EditorGUILayout.LabelField("LEVEL SETTINGS", EditorStyles.boldLabel);
        levelMaxBullets = EditorGUILayout.IntField("Max Ammo (Số đạn)", levelMaxBullets);
        if (levelMaxBullets < 1) levelMaxBullets = 1;

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
    }

    public GameObject GetSelectedPrefab()
    {
        return selectedPrefab;
    }

    #region JSON Export / Import Logic
    private void ExportLevelToJSON()
    {
        // SỬA LỖI 1: Tool là nguồn sự thật duy nhất cho số đạn khi Export.
        // KHÔNG đọc lại từ Cannon vào levelMaxBullets vì sẽ ghi đè giá trị người dùng vừa nhập trên Tool.
        // levelMaxBullets đã được người dùng cấu hình sẵn trên giao diện Tool.

        LevelData levelData = new LevelData { MaxBullets = levelMaxBullets };

        if (palette == null || palette.prefabs == null || palette.prefabs.Count == 0)
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng gán Block Palette trước khi xuất JSON!", "OK");
            return;
        }

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
                BlockData bData = new BlockData(
                    prefabParent.name,
                    obj.transform.position,
                    obj.transform.eulerAngles
                );
                levelData.blocks.Add(bData);
            }
        }

        if (levelData.blocks.Count == 0)
        {
            EditorUtility.DisplayDialog("Thông báo", "Không tìm thấy Block hợp lệ nào trong Scene để xuất!", "OK");
            return;
        }

        string path = EditorUtility.SaveFilePanel("Save Level JSON", Application.dataPath, "NewLevel", "json");
        if (!string.IsNullOrEmpty(path))
        {
            string json = JsonUtility.ToJson(levelData, true);
            File.WriteAllText(path, json);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Thành công", $"Đã xuất Level với {levelData.MaxBullets} đạn và {levelData.blocks.Count} blocks ra file JSON!", "OK");
        }
    }

    private void ImportLevelFromJSON()
    {
        if (palette == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng gán Block Palette trước khi Import JSON!", "OK");
            return;
        }

        string path = EditorUtility.OpenFilePanel("Load Level JSON", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

        string json = File.ReadAllText(path);
        LevelData levelData = JsonUtility.FromJson<LevelData>(json);

        if (levelData == null || levelData.blocks == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "File JSON không đúng định dạng LevelData!", "OK");
            return;
        }

        // 1. Cập nhật số đạn hiển thị trên Tool
        levelMaxBullets = levelData.MaxBullets;

        // Tạo Dictionary để tra cứu Prefab nhanh hơn
        Dictionary<string, GameObject> prefabMap = new Dictionary<string, GameObject>();
        foreach (var p in palette.prefabs)
        {
            if (p != null && !prefabMap.ContainsKey(p.name))
                prefabMap.Add(p.name, p);
        }

        // 2. Thu thập danh sách Block cũ cần xóa vào List TRƯỚC để tránh InvalidOperationException
        List<GameObject> objectsToDestroy = new List<GameObject>();
        GameObject[] existingObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in existingObjects)
        {
            GameObject prefabParent = PrefabUtility.GetCorrespondingObjectFromSource(obj);
            if (prefabParent != null && prefabMap.ContainsKey(prefabParent.name))
            {
                objectsToDestroy.Add(obj);
            }
        }
        // Thực hiện xóa sau khi đã thu thập xong
        foreach (GameObject obj in objectsToDestroy)
        {
            Undo.DestroyObjectImmediate(obj);
        }

        // 3. Sinh ra các Block mới từ JSON
        int spawnedCount = 0;
        foreach (BlockData bData in levelData.blocks)
        {
            if (prefabMap.TryGetValue(bData.prefabName, out GameObject prefab))
            {
                GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (go != null)
                {
                    go.transform.position = bData.position;
                    go.transform.eulerAngles = bData.rotation;
                    Undo.RegisterCreatedObjectUndo(go, "Import Level Block");
                    spawnedCount++;
                }
            }
        }

        // 4. Cập nhật số đạn cho SimpleCannon trong Scene & lưu thay đổi (Chạy SAU KHI đã sinh toàn bộ Object từ JSON)
        SimpleCannon cannon = null;
        SimpleCannon[] cannons = Resources.FindObjectsOfTypeAll<SimpleCannon>();
        foreach (var c in cannons)
        {
            // Đảm bảo đối tượng thuộc Scene đang mở (không phải Prefab Asset trong project)
            if (c != null && !EditorUtility.IsPersistent(c.gameObject))
            {
                cannon = c;
                break;
            }
        }

        if (cannon != null)
        {
            Undo.RecordObject(cannon, "Update Cannon Ammo");

            // Gọi hàm gán đạn đã chuẩn hóa bên SimpleCannon
            cannon.SetMaxBullets(levelData.MaxBullets);

            // Ghi nhận thay đổi đối với Prefab Instance trên Scene để tránh bị reset khi lưu/tải lại
            PrefabUtility.RecordPrefabInstancePropertyModifications(cannon);
            EditorUtility.SetDirty(cannon);
            EditorSceneManager.MarkSceneDirty(cannon.gameObject.scene);
        }
        else
        {
            Debug.LogWarning("[LevelEditor] Không tìm thấy SimpleCannon nào trong Scene hiện tại để cập nhật số đạn!");
        }

        EditorUtility.DisplayDialog("Thành công", $"Đã Import thành công Level ({levelData.MaxBullets} đạn) và {spawnedCount} blocks!", "OK");
    }
    #endregion
}