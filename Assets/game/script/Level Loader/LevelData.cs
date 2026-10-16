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
    public string levelName = "CustomLevel";
    public int MaxBullets = 10;
    public LevelDifficulty Difficulty = LevelDifficulty.Normal;

    // 🔥 MỚI: Thông số điều kiện thắng dành riêng cho chế độ Digital
    public float minWinWeight = 10f;
    public float maxWinWeight = 20f;

    public List<BlockData> blocks = new List<BlockData>();
}