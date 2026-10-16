using System.Collections.Generic;
using UnityEngine;

public class LevelLoader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BlockLevel blockLevelDatabase;
    [SerializeField] private SimpleCannon cannon;
    [SerializeField] private Transform levelParent;

    private readonly List<GameObject> _spawnedBlocks = new List<GameObject>();

    public LevelDifficulty CurrentDifficulty { get; private set; } = LevelDifficulty.Normal;

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

        // 1. Dọn dẹp Level cũ và Reset trạng thái GameRule
        ClearCurrentLevel();
        if (GameRuleController.Instance != null)
        {
            GameRuleController.Instance.ResetRules();
        }

        // 2. Đọc độ khó từ JSON
        CurrentDifficulty = data.Difficulty;

        // 3. Đồng bộ đạn cho SimpleCannon
        if (cannon != null)
        {
            cannon.SetMaxBullets(data.MaxBullets);
        }

        // 4. Báo cáo độ khó cho GameRuleController
        if (GameRuleController.Instance != null)
        {
            GameRuleController.Instance.SetLevelDifficulty(data.Difficulty);
        }

        // 5. Sinh ra các Prefab từ Database
        foreach (BlockData bData in data.blocks)
        {
            GameObject prefab = blockLevelDatabase != null ? blockLevelDatabase.GetPrefabByName(bData.prefabName) : null;
            if (prefab != null)
            {
                if (prefab.GetComponent<SimpleCannon>() != null)
                {
                    Debug.LogWarning("Phát hiện JSON chứa Súng! Đã chặn đứng hành vi đẻ thêm súng Clone!");
                    continue;
                }

                GameObject obj = Instantiate(prefab, bData.position, Quaternion.Euler(bData.rotation), levelParent);
                _spawnedBlocks.Add(obj);

                // 🔥 MỚI: Nếu vật thể sinh ra là DigitalScale (hoặc chứa DigitalScale ở con)
                DigitalScale scaleScript = obj.GetComponentInChildren<DigitalScale>();
                if (scaleScript != null)
                {
                    bool isDigitalLevel = (data.Difficulty == LevelDifficulty.Digital);
                    scaleScript.ConfigureFromLevel(isDigitalLevel, data.minWinWeight, data.maxWinWeight);

                    if (GameRuleController.Instance != null)
                    {
                        GameRuleController.Instance.RegisterDigitalScale(scaleScript);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[LevelLoader] Không tìm thấy Prefab có tên: {bData.prefabName}");
            }
        }

        Debug.Log($"[LevelLoader] Load thành công Level {data.levelName} - Độ khó: [{CurrentDifficulty}] ({data.MaxBullets} đạn, {data.blocks.Count} objects)");
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