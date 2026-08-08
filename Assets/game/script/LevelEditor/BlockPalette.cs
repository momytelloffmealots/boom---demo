using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockPalette", menuName = "Level Editor/Block Palette")]
public class BlockPalette : ScriptableObject
{
    public List<GameObject> prefabs = new();
}