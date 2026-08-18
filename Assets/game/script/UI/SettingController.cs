using UnityEngine;
using UnityEngine.UI;

public class SettingController : MonoBehaviour
{
    // =====================================================
    // MUSIC UI
    // =====================================================

    [Header("Music")]
    public Button btnMusic;

    // Background xanh lá khi ON
    public GameObject musicOn;

    // Background xanh dương khi OFF
    public GameObject musicOff;

    // Icon Music
    public Image musicIcon;


    // =====================================================
    // SOUND UI
    // =====================================================

    [Header("Sound")]
    public Button btnSound;

    // Background xanh lá khi ON
    public GameObject soundOn;

    // Background xanh dương khi OFF
    public GameObject soundOff;

    // Icon Sound
    public Image soundIcon;


    // =====================================================
    // VIBRATION UI
    // =====================================================

    [Header("Vibration")]
    public Button btnVibration;

    // Background xanh lá khi ON
    public GameObject vibrationOn;

    // Background xanh dương khi OFF
    public GameObject vibrationOff;

    // Icon Vibration
    public Image vibrationIcon;


    // =====================================================
    // ICON COLOR
    // =====================================================

    [Header("Icon Colors")]

    // ON = icon màu trắng
    public Color iconOnColor = Color.white;

    // OFF = icon màu xanh dương đậm
    public Color iconOffColor;


    // =====================================================
    // TRẠNG THÁI
    // =====================================================

    private bool isMusicOn;
    private bool isSoundOn;
    private bool isVibrationOn;


    // Lưu trạng thái lần trước
    // Dùng để phát hiện setting trong game đã thay đổi
    //private int lastMusicValue;
    //private int lastSoundValue;
    //private int lastVibrationValue;


    private void Start()
    {
        // =================================================
        // GẮN SỰ KIỆN BUTTON BẰNG CODE
        // Không cần dùng OnClick trong Inspector
        // =================================================

        btnMusic.onClick.AddListener(ToggleMusic);
        btnSound.onClick.AddListener(ToggleSound);
        btnVibration.onClick.AddListener(ToggleVibration);


        // =================================================
        // LOAD SETTING ĐÃ LƯU
        // =================================================

        LoadSettings();


        // =================================================
        // TỰ GẮN SOUND CLICK CHO CÁC BUTTON
        // Btn_Sound được bỏ qua vì xử lý riêng
        // =================================================

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.AddSoundToButtons(btnSound);
        }
    }


    //private void Update()
    //{
    //    // =================================================
    //    // KIỂM TRA SETTING CÓ BỊ THAY ĐỔI TỪ NƠI KHÁC KHÔNG
    //    //
    //    // Ví dụ:
    //    // Setting trong game thay đổi MusicOn
    //    // thì Setting ngoài sảnh tự cập nhật theo.
    //    // =================================================

    //    int musicValue =
    //        PlayerPrefs.GetInt("MusicOn", 1);

    //    int soundValue =
    //        PlayerPrefs.GetInt("SoundOn", 1);

    //    int vibrationValue =
    //        PlayerPrefs.GetInt("VibrationOn", 1);


    //    // Nếu có bất kỳ setting nào thay đổi
    //    if (musicValue != lastMusicValue ||
    //        soundValue != lastSoundValue ||
    //        vibrationValue != lastVibrationValue)
    //    {
    //        LoadSettings();
    //    }
    //}


    // =====================================================
    // LOAD SETTING
    // =====================================================

    public void LoadSettings()
    {
        // Nếu chưa có dữ liệu thì mặc định ON
        isMusicOn =
            PlayerPrefs.GetInt("MusicOn", 1) == 1;

        isSoundOn =
            PlayerPrefs.GetInt("SoundOn", 1) == 1;

        isVibrationOn =
            PlayerPrefs.GetInt("VibrationOn", 1) == 1;


        // Cập nhật UI
        UpdateMusicUI();
        UpdateSoundUI();
        UpdateVibrationUI();


        // Áp dụng trạng thái thật
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusic(isMusicOn);
            AudioManager.Instance.SetSound(isSoundOn);
        }
    }



    // =====================================================
    // MUSIC
    // =====================================================

    private void ToggleMusic()
{
    // Đảo trạng thái
    isMusicOn = !isMusicOn;

    // Lưu trạng thái
    PlayerPrefs.SetInt(
        "MusicOn",
        isMusicOn ? 1 : 0
    );

    PlayerPrefs.Save();

    // Cập nhật UI
    UpdateMusicUI();

    // Bật / tắt nhạc
    if (AudioManager.Instance != null)
    {
        AudioManager.Instance.SetMusic(isMusicOn);
    }
}


private void UpdateMusicUI()
    {
        // Music ON
        // -> nền xanh lá
        musicOn.SetActive(isMusicOn);

        // Music OFF
        // -> nền xanh dương
        musicOff.SetActive(!isMusicOn);


        // ON  -> icon trắng
        // OFF -> icon xanh đậm
        musicIcon.color =
            isMusicOn
                ? iconOnColor
                : iconOffColor;
    }


// =====================================================
// SOUND
// =====================================================

private void ToggleSound()
{
    // Đảo trạng thái
    isSoundOn = !isSoundOn;

    // Lưu
    PlayerPrefs.SetInt(
        "SoundOn",
        isSoundOn ? 1 : 0
    );

    PlayerPrefs.Save();

    // Cập nhật UI
    UpdateSoundUI();


    if (AudioManager.Instance != null)
    {
        // Nếu vừa bật Sound
        if (isSoundOn)
        {
            // Bật Sound trước
            AudioManager.Instance.SetSound(true);

            // Bấm bật Sound thì có tiếng click
            AudioManager.Instance.PlayButtonClick();
        }
        else
        {
            // Tắt Sound
            // Bấm tắt sẽ không kêu
            AudioManager.Instance.SetSound(false);
        }
    }
}


private void UpdateSoundUI()
    {
        // ON -> xanh lá
        soundOn.SetActive(isSoundOn);

        // OFF -> xanh dương
        soundOff.SetActive(!isSoundOn);


        // Đổi màu icon
        soundIcon.color =
            isSoundOn
                ? iconOnColor
                : iconOffColor;
    }


// =====================================================
// VIBRATION
// =====================================================

private void ToggleVibration()
{
    // Đảo trạng thái
    isVibrationOn = !isVibrationOn;

    // Lưu
    PlayerPrefs.SetInt(
        "VibrationOn",
        isVibrationOn ? 1 : 0
    );

    PlayerPrefs.Save();

    // Cập nhật UI
    UpdateVibrationUI();


    // Nếu vừa bật thì rung thử
    if (isVibrationOn)
    {
        Vibrate();
    }
}



private void UpdateVibrationUI()
    {
        // ON -> xanh lá
        vibrationOn.SetActive(isVibrationOn);

        // OFF -> xanh dương
        vibrationOff.SetActive(!isVibrationOn);


        // Đổi màu icon
        vibrationIcon.color =
            isVibrationOn
                ? iconOnColor
                : iconOffColor;
    }


    // =====================================================
    // RUNG ĐIỆN THOẠI
    // =====================================================

    public void Vibrate()
    {
        // Đọc trực tiếp PlayerPrefs
        // để cả Setting trong game và ngoài sảnh dùng chung
        bool vibrationEnabled =
            PlayerPrefs.GetInt("VibrationOn", 1) == 1;


        // OFF -> không rung
        if (!vibrationEnabled)
        {
            return;
        }


        // Chỉ chạy khi build Android / iOS
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }


    // =====================================================
    // CÁC HÀM CHO SCRIPT KHÁC KIỂM TRA
    // =====================================================

    public bool IsMusicOn()
    {
        return PlayerPrefs.GetInt("MusicOn", 1) == 1;
    }


    public bool IsSoundOn()
    {
        return PlayerPrefs.GetInt("SoundOn", 1) == 1;
    }


    public bool IsVibrationOn()
    {
        return PlayerPrefs.GetInt("VibrationOn", 1) == 1;
    }


    // =====================================================
    // XÓA LISTENER KHI OBJECT BỊ HỦY
    // =====================================================

    private void OnDestroy()
    {
        if (btnMusic != null)
        {
            btnMusic.onClick.RemoveListener(ToggleMusic);
        }

        if (btnSound != null)
        {
            btnSound.onClick.RemoveListener(ToggleSound);
        }

        if (btnVibration != null)
        {
        btnVibration.onClick.RemoveListener(ToggleVibration);
        }
    }
}