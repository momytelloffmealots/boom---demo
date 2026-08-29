using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EnergyUIView : MonoBehaviour
{
    [Header("Giao Diện Chữ")]
    public TextMeshProUGUI txtCount;
    public TextMeshProUGUI txtTime;

    [Header("Chức năng của Nút (Tùy chọn)")]
    [Tooltip("Kéo nút bấm vào đây (VD: Nút Energy ngoài Home hoặc Nút X đóng bảng)")]
    public Button myButton;

    [Tooltip("Nếu đây là nút Home: Kéo Panle_MoreLives vào để mở. Nếu đây là nút X: Bỏ trống!")]
    public GameObject panelToOpen;

    [Header("Nút Nạp Mạng (Chỉ dùng cho bảng More Lives)")]
    [Tooltip("Kéo nút Refill màu xanh lá vào đây")]
    public Button btnRefill;

    private void Start()
    {
        // Lắng nghe lệnh từ Bộ Não (LivesManager)
        if (LivesManager.Instance != null)
        {
            LivesManager.Instance.OnLivesUpdated += UpdateUI;
            LivesManager.Instance.ForceUpdateUI(); // Lấy số ngay lần đầu tiên
        }

        // Gắn sự kiện cho nút bấm thông thường (Mở bảng / Đóng bảng)
        if (myButton != null)
        {
            myButton.onClick.AddListener(OnButtonClicked);
        }

        // 🔥 Gắn sự kiện cho nút Refill
        if (btnRefill != null)
        {
            btnRefill.onClick.AddListener(OnRefillClicked);
        }
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
        // Trường hợp là Nút Energy ngoài Home
        if (panelToOpen != null)
        {
            if (LivesManager.Instance.GetCurrentLives() < LivesManager.Instance.maxLives)
            {
                panelToOpen.SetActive(true);
            }
        }
        // Trường hợp là nút X trong bảng MoreLives
        else
        {
            gameObject.SetActive(false);
        }
    }

    // 🔥 Xử lý khi bấm nút REFILL
    private void OnRefillClicked()
    {
        if (LivesManager.Instance != null)
        {
            // (Chỗ này sau này bạn viết code trừ tiền vàng nhé)
            // ...

            // Báo cho Model nạp đầy mạng
            LivesManager.Instance.RefillAllLives();

            // Nạp xong thì tự động đóng bảng mua mạng lại cho gọn màn hình
            gameObject.SetActive(false);
        }
    }
}