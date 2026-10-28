using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            GameObject container = new GameObject("[LEVEL_CONTAINER]");
            if (gameObject.scene.IsValid() && container.scene != gameObject.scene)
                SceneManager.MoveGameObjectToScene(container, gameObject.scene);
            levelParent = container.transform;
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

        // 5. Sinh ra các Prefab từ Database và tính tổng khối lượng block ban đầu.
        // Chỉ các object có Block mới được tính; kệ/cân không làm tăng giới hạn mặt cân.
        float initialTotalBlockWeight = 0f;
        int initialBlockCount = 0;
        List<DigitalScale> digitalScales = new List<DigitalScale>();

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

                Vector3 blockRotation = LevelData.ResolveBlockRotation(data, bData, prefab);
                GameObject obj = Instantiate(prefab, bData.position, Quaternion.Euler(blockRotation), levelParent);
                _spawnedBlocks.Add(obj);

                Block block = obj.GetComponent<Block>();
                Rigidbody blockRigidbody = obj.GetComponent<Rigidbody>();
                if (block != null && blockRigidbody != null)
                {
                    // Gameplay may still be inactive, so Block.OnEnable has not
                    // necessarily replaced the prefab's default Rigidbody mass.
                    initialTotalBlockWeight += Mathf.Max(0f, block.ApplyConfiguredMass());
                    initialBlockCount++;
                }

                DigitalScale scaleScript = obj.GetComponentInChildren<DigitalScale>(true);
                if (scaleScript != null)
                    digitalScales.Add(scaleScript);
            }
            else
            {
                Debug.LogWarning($"[LevelLoader] Không tìm thấy Prefab có tên: {bData.prefabName}");
            }
        }

        bool isDigitalLevel = data.Difficulty == LevelDifficulty.Digital;
        foreach (DigitalScale scaleScript in digitalScales)
        {
            scaleScript.ConfigureFromLevel(isDigitalLevel, data.minWinWeight, data.maxWinWeight);
            scaleScript.SetLevelTotalBlockWeight(initialTotalBlockWeight);

            if (GameRuleController.Instance != null)
                GameRuleController.Instance.RegisterDigitalScale(scaleScript);
        }

        Debug.Log($"[LevelLoader] Tổng khối lượng ban đầu: {initialBlockCount} block, {initialTotalBlockWeight:F1} kg.");
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
