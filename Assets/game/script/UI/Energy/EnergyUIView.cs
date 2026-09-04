using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EnergyUIView : MonoBehaviour
{
    [Header("Giao Diện Chữ")]
    public TextMeshProUGUI txtCount;
    public TextMeshProUGUI txtTime;

    [Header("Chức năng của Nút (Tùy chọn)")]
    public Button myButton;
    public GameObject panelToOpen;

    [Header("Chức năng Nạp Mạng (Dành cho bảng MoreLives)")]
    public Button btnRefill;
    public int refillPrice = 900; // 🔥 Giá mua mạng (Có thể chỉnh ngoài Editor)
    public GameObject PopupShop;  // 🔥 Kéo Panel_Shop vào đây để hiện lên khi thiếu tiền

    private void Start()
    {
        if (LivesManager.Instance != null)
        {
            LivesManager.Instance.OnLivesUpdated += UpdateUI;
            LivesManager.Instance.ForceUpdateUI();
        }

        if (myButton != null) myButton.onClick.AddListener(OnButtonClicked);
        if (btnRefill != null) btnRefill.onClick.AddListener(OnRefillClicked);
    }

    private void OnDestroy()
    {
        if (LivesManager.Instance != null)
        {
            LivesManager.Instance.OnLivesUpdated -= UpdateUI;
        }
    }

    private void UpdateUI(int lives, string timeStr)
    {
        if (txtCount != null) txtCount.text = lives.ToString();
        if (txtTime != null) txtTime.text = timeStr;
    }

    private void OnButtonClicked()
    {
        if (panelToOpen != null)
        {
            if (LivesManager.Instance.GetCurrentLives() < LivesManager.Instance.maxLives)
            {
                panelToOpen.SetActive(true);
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // ================= XỬ LÝ KHI BẤM NÚT REFILL =================
    private void OnRefillClicked()
    {
        // Yêu cầu Thủ quỹ (CurrencyManager) kiểm tra và trừ tiền
        if (CurrencyManager.Instance != null && CurrencyManager.Instance.TrySpendCoins(refillPrice))
        {
            // Trừ tiền thành công -> Báo Model bơm đầy mạng
            if (LivesManager.Instance != null) LivesManager.Instance.RefillAllLives();

            // Đóng bảng mua mạng lại
            gameObject.SetActive(false);
            Debug.Log("<color=green>Nạp mạng thành công!</color>");
        }
        else
        {
            // Trừ tiền thất bại (Không đủ vàng) -> Mở bảng Shop
            Debug.LogWarning("Không đủ Vàng! Đang mở bảng Shop...");
            if (PopupShop != null) PopupShop.SetActive(true);
        }
    }
}