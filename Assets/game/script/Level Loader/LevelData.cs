using System;
using System.Collections.Generic;
using UnityEngine;

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
    public List<BlockData> blocks = new List<BlockData>();
}

