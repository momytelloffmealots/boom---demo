using UnityEngine;
using UnityEngine.UI;
using TMPro; // BẮT BUỘC PHẢI CÓ ĐỂ DÙNG CHỮ TEXT MESH PRO

[RequireComponent(typeof(Button))] // Ép buộc phải có Component Button
public class LevelButtonTrigger : MonoBehaviour
{
    [Header("Controller Liên Kết")]
    [Tooltip("Kéo Panel_Play (chứa PlayPanelController) vào đây")]
    public PlayPanelController playController;

    [Header("Giao diện Text")]
    [Tooltip("Kéo object Text (TMP) nằm trong cái nút này vào đây")]
    public TextMeshProUGUI levelTextUI;

    private Button myButton;

    private void Awake()
    {
        myButton = GetComponent<Button>();

        // Gắn sự kiện: Khi bấm nút này thì gọi hàm OpenPopup của Controller
        myButton.onClick.AddListener(OnLevelButtonClicked);
    }

    // Hàm OnEnable tự động chạy mỗi khi màn hình Home được bật lên
    private void OnEnable()
    {
        UpdateLevelText();
    }

    // Hàm đọc thẻ nhớ và đổi chữ
    public void UpdateLevelText()
    {
        if (levelTextUI != null)
        {
            // Đọc level hiện tại (Mặc định là 1 nếu chưa chơi bao giờ)
            int currentLevel = PlayerPrefs.GetInt("CURRENT_LEVEL_INDEX", 1);

            // Đổi chữ trên UI thành "LEVEL X"
            levelTextUI.text = "LEVEL " + currentLevel;
        }
    }

    private void OnLevelButtonClicked()
    {
        if (playController != null)
        {
            playController.OpenPopup();
        }
        else
        {
            Debug.LogError("Bạn chưa kéo Panel_Play vào biến playController của nút Level!");
        }
    }
}