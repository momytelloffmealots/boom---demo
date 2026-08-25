using System.Collections.Generic;
using UnityEngine;

public class LevelLoader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BlockLevel blockLevelDatabase;
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

        // 2. Đồng bộ đạn cho SimpleCannon (Ép súng gốc nhận số đạn từ JSON)
        if (cannon != null)
        {
            cannon.SetMaxBullets(data.MaxBullets);
        }

        // 3. Sinh ra các Block Prefab từ Database
        foreach (BlockData bData in data.blocks)
        {
            GameObject prefab = blockLevelDatabase != null ? blockLevelDatabase.GetPrefabByName(bData.prefabName) : null;
            if (prefab != null)
            {
                // 🔥 CHỐT CHẶN BẢO VỆ: Quét xem "cục gạch" này có phải là súng không?
                if (prefab.GetComponent<SimpleCannon>() != null)
                {
                    Debug.LogWarning("🚨 Phát hiện JSON chứa Súng! Đã chặn đứng hành vi đẻ thêm súng Clone!");
                    continue; // Bỏ qua ngay lập tức, không cho Instantiate!
                }

                GameObject obj = Instantiate(prefab, bData.position, Quaternion.Euler(bData.rotation), levelParent);
                _spawnedBlocks.Add(obj);

                // Báo cáo cục gạch vừa đẻ cho Trọng Tài để hệ thống đếm số lượng gạch
                if (obj.TryGetComponent<Block>(out Block blockScript))
                {
                    if (GameRuleController.Instance != null)
                    {
                        GameRuleController.Instance.RegisterBlock(blockScript);
                    }
                }
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

