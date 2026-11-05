using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MechanicIntroEntry
{
    [Tooltip("Mã định danh duy nhất (VD: star_box, jar, stone)")]
    public string mechanicId = "star_box";

    [Tooltip("Level sẽ xuất hiện bảng giới thiệu này")]
    public int unlockLevel = 2;

    [Header("Nội dung thay đổi (Dùng Ảnh)")]
    public Sprite titleSprite;      
    public Sprite iconSprite;     
    public Sprite descriptionSprite; 
}

[CreateAssetMenu(fileName = "MechanicIntroDatabase", menuName = "Game/Mechanic Intro Database")]
public class MechanicIntroDatabase : ScriptableObject
{
    public List<MechanicIntroEntry> mechanics = new List<MechanicIntroEntry>();

    public MechanicIntroEntry GetEntryForLevel(int levelIndex)
    {
        for (int i = 0; i < mechanics.Count; i++)
        {
            if (mechanics[i] != null && mechanics[i].unlockLevel == levelIndex)
            {
                return mechanics[i];
            }
        }
        return null;
    }
}