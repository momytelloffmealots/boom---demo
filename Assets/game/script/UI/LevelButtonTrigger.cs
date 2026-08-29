using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class LevelButtonTrigger : MonoBehaviour
{
    public PlayPanelController playController;
    public TextMeshProUGUI levelTextUI;

    [Header("Bảng Mua Mạng")]
    public GameObject panelMoreLives; // Kéo Panle_MoreLives vào đây

    private Button myButton;

    private void Awake()
    {
        myButton = GetComponent<Button>();
        myButton.onClick.AddListener(OnLevelButtonClicked);
    }

    private void OnEnable()
    {
        if (levelTextUI != null)
        {
            int currentLevel = PlayerPrefs.GetInt("CURRENT_LEVEL_INDEX", 1);
            levelTextUI.text = "LEVEL " + currentLevel;
        }
    }

    private void OnLevelButtonClicked()
    {
        // KIỂM TRA MẠNG: Hết mạng thì cấm chơi, bật bảng MoreLives lên
        if (LivesManager.Instance != null && LivesManager.Instance.GetCurrentLives() <= 0)
        {
            if (panelMoreLives != null) panelMoreLives.SetActive(true);
            return; // Dừng luôn, không cho mở bảng Play
        }

        // Còn mạng thì chơi bình thường
        if (playController != null) playController.OpenPopup();
    }
}