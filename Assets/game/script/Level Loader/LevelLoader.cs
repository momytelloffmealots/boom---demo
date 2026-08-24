using System.Collections.Generic;
using UnityEngine;

public class LevelLoader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BlockLevel blockLevelDatabase; // Đã đổi sang kiểu BlockLevel
    [SerializeField] private SimpleCannon cannon;
    [SerializeField] private Transform levelParent;

    private readonly List<GameObject> _spawnedBlocks = new List<GameObject>();

    private void Awake()
    {
        if (blockLevelDatabase != null) blockLevelDatabase.Init();
        if (levelParent == null)
        {
            levelParent = new GameObject("[LEVEL_CONTAINER]").transform;
        }
    }

    public void LoadLevelFromJSON(string jsonText)
    {
        if (string.IsNullOrEmpty(jsonText))
        {
            Debug.LogError("[LevelLoader] Chuỗi JSON rỗng!");
            return;
        }

        LevelData data = JsonUtility.FromJson<LevelData>(jsonText);
        if (data == null || data.blocks == null)
        {
            Debug.LogError("[LevelLoader] Không thể đọc dữ liệu từ JSON!");
            return;
        }

        // 1. Dọn dẹp Level cũ
        ClearCurrentLevel();

        // 2. Đồng bộ đạn cho SimpleCannon
        if (cannon != null)
        {
            cannon.SetMaxBullets(data.MaxBullets);
        }

        // 3. Sinh ra các Block Prefab từ BlockLevel
        foreach (BlockData bData in data.blocks)
        {
            GameObject prefab = blockLevelDatabase != null ? blockLevelDatabase.GetPrefabByName(bData.prefabName) : null;
            if (prefab != null)
            {
                GameObject obj = Instantiate(prefab, bData.position, Quaternion.Euler(bData.rotation), levelParent);
                _spawnedBlocks.Add(obj);
            }
            else
            {
                Debug.LogWarning($"[LevelLoader] Không tìm thấy Prefab có tên: {bData.prefabName}");
            }
        }

        Debug.Log($"[LevelLoader] Load thành công Level {data.levelName} ({data.MaxBullets} đạn, {data.blocks.Count} blocks)");
    }

    public void ClearCurrentLevel()
    {
        foreach (var block in _spawnedBlocks)
        {
            if (block != null) Destroy(block);
        }
        _spawnedBlocks.Clear();
    }
}