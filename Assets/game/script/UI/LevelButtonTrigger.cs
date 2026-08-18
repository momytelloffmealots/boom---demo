using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))] // Ép buộc phải có Component Button
public class LevelButtonTrigger : MonoBehaviour
{
    [Header("Controller Liên Kết")]
    [Tooltip("Kéo Panel_Play (chứa PlayPanelController) vào đây")]
    public PlayPanelController playController;

    private Button myButton;

    private void Awake()
    {
        myButton = GetComponent<Button>();

        // Gắn sự kiện: Khi bấm nút này thì gọi hàm OpenPopup của Controller
        myButton.onClick.AddListener(OnLevelButtonClicked);
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