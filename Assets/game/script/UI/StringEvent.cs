using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewStringEvent", menuName = "Events/Primitive/String Event")]
public class StringEvent : ScriptableObject
{
    // Dùng Action của C# để làm danh sách lắng nghe cho gọn nhẹ
    public event Action<string> OnEventRaised;

    public void Raise(string value)
    {
        OnEventRaised?.Invoke(value);
    }
}