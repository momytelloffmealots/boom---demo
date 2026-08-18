using UnityEngine;
using UnityEngine.UI;

public class ItemDetailPopup : MonoBehaviour
{
    [Header("Popup Elements")]
    [SerializeField] private Image imgLargeIcon;

    [Header("Overlay Settings")]
    [SerializeField] private Button btnOverlayBackground;

    private void Awake()
    {
        // Gán nút bấm nền mờ để đóng
        if (btnOverlayBackground == null)
        {
            btnOverlayBackground = GetComponent<Button>();
        }

        if (btnOverlayBackground != null)
        {
            btnOverlayBackground.onClick.RemoveAllListeners();
            btnOverlayBackground.onClick.AddListener(ClosePopup);
        }
    }

    public void ShowPopup(ItemSO data)
    {
        if (imgLargeIcon != null && data != null && data.icon != null)
        {
            imgLargeIcon.sprite = data.icon;
        }

        // Bật popup khi bấm vào item
        gameObject.SetActive(true);
    }

    public void ClosePopup()
    {
        // Tắt popup khi bấm ra ngoài
        gameObject.SetActive(false);
    }
}