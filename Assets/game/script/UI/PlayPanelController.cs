using UnityEngine;
using UnityEngine.UI;

public class PlayPanelController : MonoBehaviour
{
    [Header("UI Buttons")]
    public Button btnClose;    // Kéo btn_x vào đây
    public Button btnPlayGame; // Kéo btn_play vào đây

    [Header("Game Systems")]
    public GameObject gameplayRoot; // Kéo cụm 3D Gameplay_Root vào đây
    public GameObject canvasUI;     // Kéo Canvas tổng vào đây

    private void Awake()
    {
        // Đăng ký sự kiện bằng code (Chuẩn MVC)
        if (btnClose != null)
        {
            btnClose.onClick.AddListener(ClosePopup);
        }

        if (btnPlayGame != null)
        {
            btnPlayGame.onClick.AddListener(StartGameplay);
        }
    }

    // Hàm dùng để mở Popup
    public void OpenPopup()
    {
        gameObject.SetActive(true);
    }

    // Hàm dùng để đóng Popup
    public void ClosePopup()
    {
        gameObject.SetActive(false);
    }

    // Hàm xử lý khi bấm nút Play màu xanh
    public void StartGameplay()
    {
        // 1. Tắt chính cái popup này
        gameObject.SetActive(false);

        // 2. Tắt toàn bộ giao diện Menu (Canvas)
        if (canvasUI != null) canvasUI.SetActive(false);

        // 3. Bật môi trường 3D lên để chơi
        if (gameplayRoot != null) gameplayRoot.SetActive(true);
    }
}