using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))] // Tự động bắt buộc phải có Button
public class ClosePopupTrigger : MonoBehaviour
{
    [Header("Cài đặt")]
    [Tooltip("Kéo cái Bảng mà bạn muốn tắt vào đây. Nếu để trống, nó sẽ tự động tìm Bảng cha chứa nó để tắt!")]
    public GameObject panelToClose;

    private Button myButton;

    private void Awake()
    {
        myButton = GetComponent<Button>();

        // Tự động cắm dây: Khi bấm nút thì gọi hàm Đóng
        myButton.onClick.AddListener(ClosePopup);
    }

    private void ClosePopup()
    {
        if (panelToClose != null)
        {
            // Tắt bảng được chỉ định
            panelToClose.SetActive(false);
        }
        else
        {
            // TÍNH NĂNG THÔNG MINH: Nếu quên kéo thả, nó sẽ tự động truy ngược lên tìm cái Panel gốc bao bọc ngoài cùng và tắt đi.
            // Điều này áp dụng cực tốt cho các nút X nằm bên trong Popup.
            Transform parentPopup = GetTopLevelPanel(transform);
            if (parentPopup != null)
            {
                parentPopup.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("Không tìm thấy Panel nào để tắt!");
            }
        }
    }

    // Hàm đệ quy: Tự động leo lên cây gia phả để tìm cái Panel to nhất chứa nút bấm này
    private Transform GetTopLevelPanel(Transform current)
    {
        if (current.parent == null || current.parent.GetComponent<Canvas>() != null)
        {
            return current;
        }
        return GetTopLevelPanel(current.parent);
    }
}