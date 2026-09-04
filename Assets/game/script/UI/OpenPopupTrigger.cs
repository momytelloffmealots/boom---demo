using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))] // Tự động bắt buộc phải có Button
public class OpenPopupTrigger : MonoBehaviour
{
    [Header("Cài đặt")]
    [Tooltip("Kéo Panel hoặc Popup bạn muốn MỞ vào đây")]
    public GameObject panelToOpen;

    private Button myButton;

    private void Awake()
    {
        myButton = GetComponent<Button>();

        // Tự động cắm dây: Khi bấm nút thì gọi hàm Mở
        myButton.onClick.AddListener(OpenPopup);
    }

    private void OpenPopup()
    {
        if (panelToOpen != null)
        {
            panelToOpen.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Chưa kéo Panel cần mở vào ô Panel To Open!");
        }
    }
}