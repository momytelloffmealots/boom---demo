using System;
using System.Collections.Generic;
using UnityEngine;

// 1. Thêm Digital vào enum cấp độ khó
public enum LevelDifficulty
{
    Normal,
    Hard,
    SuperHard,
    Digital // 🔥 MỚI: Chế độ Cân Điện Tử
}

[Serializable]
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

[Serializable]
public class LevelData
{
    public const int CurrentFormatVersion = 1;

    // Missing version fields in older JSON deserialize as 0.
    public int formatVersion;
    public string levelName = "CustomLevel";
    public int MaxBullets = 10;
    public LevelDifficulty Difficulty = LevelDifficulty.Normal;

    // 🔥 MỚI: Thông số điều kiện thắng dành riêng cho chế độ Digital
    public float minWinWeight = 10f;
    public float maxWinWeight = 20f;

    public List<BlockData> blocks = new List<BlockData>();

    public static Vector3 ResolveBlockRotation(LevelData data, BlockData block, GameObject prefab)
    {
        Vector3 rotation = block.rotation;
        if (data == null || data.formatVersion >= CurrentFormatVersion || prefab == null)
            return rotation;

        // Legacy levels were authored with the DigitalScale prefab's default orientation,
        // while the gameplay scene uses its front-facing Y=180 placement.
        if (prefab.GetComponentInChildren<DigitalScale>(true) != null &&
            Mathf.Abs(Mathf.DeltaAngle(rotation.y, 0f)) < 0.01f)
        {
            rotation.y = 180f;
        }

        return rotation;
    }
}
