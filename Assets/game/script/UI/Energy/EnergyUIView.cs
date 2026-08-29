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

    private void Start()
    {
        // Lắng nghe lệnh từ Bộ Não (LivesManager)
        if (LivesManager.Instance != null)
        {
            LivesManager.Instance.OnLivesUpdated += UpdateUI;
            LivesManager.Instance.ForceUpdateUI(); // Lấy số ngay lần đầu tiên
        }

        // Gắn sự kiện cho nút bấm
        if (myButton != null)
        {
            myButton.onClick.AddListener(OnButtonClicked);
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
        // 1. Trường hợp là Nút Energy ngoài Home
        if (panelToOpen != null)
        {
            // Chỉ mở bảng mua mạng nếu mạng ĐANG NHỎ HƠN 5 (Max)
            if (LivesManager.Instance.GetCurrentLives() < LivesManager.Instance.maxLives)
            {
                panelToOpen.SetActive(true);
            }
        }
        // 2. Trường hợp là nút X trong bảng MoreLives
        else
        {
            // Tự đóng chính cái bảng đang chứa nó lại
            gameObject.SetActive(false); // (Lưu ý: Gắn script này vào thẻ Panel gốc)
        }
    }
}