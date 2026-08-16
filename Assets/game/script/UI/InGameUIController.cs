using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // THÊM DÒNG NÀY: Để sử dụng lệnh LoadScene

public class InGameUIController : MonoBehaviour
{
    [Header("--- In-Game HUD ---")]
    public Button btnSettings;       // Kéo nút Bánh răng vào đây
    public GameObject expandedMenu;  // Kéo cục chứa 4 nút mở rộng (SubButtons) vào đây

    [Header("--- Setting In Game ---")]
    public Button btnNhac;
    public Button btnLoa;
    public Button btnRung;

    public GameObject nhacOn;
    public GameObject nhacOff;

    public GameObject soundOn;
    public GameObject soundOff;

    public GameObject vibrationOn;
    public GameObject vibrationOff;


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


        // ================= SETTING IN GAME =================

        // Music
        btnNhac.onClick.AddListener(ToggleMusicInGame);

        // Sound
        btnLoa.onClick.AddListener(ToggleSoundInGame);

        // Vibration
        btnRung.onClick.AddListener(ToggleVibrationInGame);

        UpdateMusicInGameUI();
        UpdateSoundInGameUI();
        UpdateVibrationInGameUI();

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
    private void UpdateMusicInGameUI()
    {
        bool musicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;

        nhacOn.SetActive(musicOn);
        nhacOff.SetActive(!musicOn);
    }

    private void UpdateSoundInGameUI()
    {
        bool soundOnState =
            PlayerPrefs.GetInt("SoundOn", 1) == 1;

        soundOn.SetActive(soundOnState);
        soundOff.SetActive(!soundOnState);
    }

    private void UpdateVibrationInGameUI()
    {
        bool vibrationOnState =
            PlayerPrefs.GetInt("VibrationOn", 1) == 1;
        vibrationOn.SetActive(vibrationOnState);
        vibrationOff.SetActive(!vibrationOnState);
    }


    // ================= MUSIC =================
    private void ToggleMusicInGame()
    {
        bool musicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;

        musicOn = !musicOn;

        PlayerPrefs.SetInt("MusicOn", musicOn ? 1 : 0);
        PlayerPrefs.Save();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusic(musicOn);
        }
        UpdateMusicInGameUI();
    }


    // ================= SOUND =================
    private void ToggleSoundInGame()
    {
        bool soundOnState =
            PlayerPrefs.GetInt("SoundOn", 1) == 1;

        soundOnState = !soundOnState;

        PlayerPrefs.SetInt(
            "SoundOn",
            soundOnState ? 1 : 0
        );

        PlayerPrefs.Save();

        if (AudioManager.Instance != null)
        {
            if (soundOnState)
            {
                // Bật sound
                AudioManager.Instance.SetSound(true);

                // Bấm bật Sound thì có tiếng
                AudioManager.Instance.PlayButtonClick();
            }
            else
            {
                // Tắt Sound
                // lần bấm này không kêu
                AudioManager.Instance.SetSound(false);
            }
        }

        // Cập nhật hình ON/OFF
        UpdateSoundInGameUI();
    }


    // ================= VIBRATION =================
    private void ToggleVibrationInGame()
    {
        bool vibrationOnState =
            PlayerPrefs.GetInt("VibrationOn", 1) == 1;

        vibrationOnState = !vibrationOnState;

        PlayerPrefs.SetInt(
            "VibrationOn",
            vibrationOnState ? 1 : 0
        );

        PlayerPrefs.Save();

        // Cập nhật hình ON/OFF
        UpdateVibrationInGameUI();

        // Nếu vừa bật vibration thì rung thử
        if (vibrationOnState)
        {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
        }
    }
}