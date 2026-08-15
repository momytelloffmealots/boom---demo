using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // THÊM DÒNG NÀY: Để sử dụng lệnh LoadScene

public class InGameUIController : MonoBehaviour
{
    [Header("--- In-Game HUD ---")]
    public Button btnSettings;       // Kéo nút Bánh răng vào đây
    public GameObject expandedMenu;  // Kéo cục chứa 4 nút mở rộng (SubButtons) vào đây

    [Header("--- Quit Popup ---")]
    public Button btnOpenQuitPopup;  // Kéo nút Thoát (màu đỏ có mũi tên) vào đây
    public GameObject panelQuitConfirm; // Kéo Panel "Are You Sure?" vào đây
    public Button btnCloseQuitPopup; // Kéo nút X trên popup vào đây
    public Button btnConfirmQuit;    // Kéo nút Quit to màu đỏ vào đây

    [Header("--- Hệ thống chuyển cảnh ---")]
    public GameObject gameplayRoot;  // Kéo cục 3D (Game Root) vào đây
    public GameObject mainCanvasUI;  // Kéo Canvas chứa Menu tổng vào đây
    public StringEvent onTabSelectedEvent; // Kéo file Event OnTabSelected vào đây

    private void Awake()
    {
        // 1. Logic nút Cài đặt: Bật/tắt danh sách nút mở rộng
        if (btnSettings != null)
        {
            btnSettings.onClick.AddListener(() =>
            {
                expandedMenu.SetActive(!expandedMenu.activeSelf);
            });
        }

        // 2. Logic nút mở Popup Thoát
        if (btnOpenQuitPopup != null)
        {
            btnOpenQuitPopup.onClick.AddListener(() =>
            {
                panelQuitConfirm.SetActive(true);
                expandedMenu.SetActive(false); // Ẩn danh sách nút đi cho gọn
            });
        }

        // 3. Logic nút X: Đóng Popup Thoát
        if (btnCloseQuitPopup != null)
        {
            btnCloseQuitPopup.onClick.AddListener(() =>
            {
                panelQuitConfirm.SetActive(false);
            });
        }

        // 4. Logic nút QUIT: Đồng ý thoát về Home
        if (btnConfirmQuit != null)
        {
            btnConfirmQuit.onClick.AddListener(QuitToHome);
        }
    }

    // THÊM HÀM NÀY: Chạy mỗi khi màn hình InGame bật lên để dọn dẹp các Panel kẹt
    private void OnEnable()
    {
        if (expandedMenu != null) expandedMenu.SetActive(false);
        if (panelQuitConfirm != null) panelQuitConfirm.SetActive(false);
    }

    private void QuitToHome()
    {
        // Thay vì chỉ tắt UI, ta load lại toàn bộ Scene để dọn sạch rác 3D (đạn, gạch vỡ...)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}