using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))] // Tự động bắt buộc phải có Button
public class QuitButtonTrigger : MonoBehaviour
{
    private Button myButton;

    private void Awake()
    {
        myButton = GetComponent<Button>();

        // Lập trình tự động cắm dây: Khi bấm nút thì gọi thẳng đến Trọng Tài
        myButton.onClick.AddListener(OnQuitClicked);
    }

    private void OnQuitClicked()
    {
        // Thông qua cánh cửa Instance (Độc tôn), gọi hàm xử phạt mà không cần kéo thả ngoài Editor
        if (GameRuleController.Instance != null)
        {
            GameRuleController.Instance.QuitGameAndLoseLife();
        }
        else
        {
            Debug.LogError("Không tìm thấy GameRuleController trên Scene!");
        }
    }
}