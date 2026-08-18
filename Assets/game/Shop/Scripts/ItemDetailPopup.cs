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
        // Tự động ẩn Popup ngay khi scene bắt đầu
        gameObject.SetActive(false);

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

        gameObject.SetActive(true);
    }

    public void ClosePopup()
    {
        gameObject.SetActive(false);
    }
}