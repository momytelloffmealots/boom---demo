using UnityEngine;

public class TabPanel : MonoBehaviour
{
    [Header("Cấu hình Tab")]
    public string myTabName; // Tên phải khớp chính xác (VD: "Home")
    public StringEvent onTabSelectedEvent;

    [Header("Giao diện cần bật/tắt")]
    public GameObject panelToToggle; // Kéo Panel thực tế vào đây

    private void OnEnable()
    {
        if (onTabSelectedEvent != null)
            onTabSelectedEvent.OnEventRaised += HandleTabChanged;
    }

    private void OnDisable()
    {
        if (onTabSelectedEvent != null)
            onTabSelectedEvent.OnEventRaised -= HandleTabChanged;
    }

    private void HandleTabChanged(string activeTabName)
    {
        // Điều khiển Panel được gán, KHÔNG tắt chính nó
        if (panelToToggle != null)
        {
            panelToToggle.SetActive(activeTabName == myTabName);
        }
    }
}