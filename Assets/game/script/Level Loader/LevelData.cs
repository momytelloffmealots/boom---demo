using System;
using System.Collections.Generic;
using UnityEngine;

// 1. Định nghĩa enum cấp độ khó
public enum LevelDifficulty
{
    Normal,
    Hard,
    SuperHard
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

    // 2. Thêm độ khó cho Level (mặc định là Normal)
    public LevelDifficulty Difficulty = LevelDifficulty.Normal;

    public List<BlockData> blocks = new List<BlockData>();
}