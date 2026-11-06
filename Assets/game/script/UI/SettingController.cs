using UnityEngine;
using UnityEngine.UI;

public class SettingController : MonoBehaviour
{
    // =====================================================
    // MUSIC UI
    // =====================================================

    [Header("Music")]
    public Button btnMusic;
    public GameObject musicOn;
    public GameObject musicOff;


    // =====================================================
    // SOUND UI
    // =====================================================

    [Header("Sound")]
    public Button btnSound;
    public GameObject soundOn;
    public GameObject soundOff;


    // =====================================================
    // VIBRATION UI
    // =====================================================

    [Header("Vibration")]
    public Button btnVibration;
    public GameObject vibrationOn;
    public GameObject vibrationOff;


    // =====================================================
    // TRẠNG THÁI
    // =====================================================

    private bool isMusicOn;
    private bool isSoundOn;
    private bool isVibrationOn;


    private void Start()
    {
        btnMusic.onClick.AddListener(ToggleMusic);
        btnSound.onClick.AddListener(ToggleSound);
        btnVibration.onClick.AddListener(ToggleVibration);

        LoadSettings();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.AddSoundToButtons(btnSound);
        }
    }


    private void OnEnable()
    {
        // Khi mở lại Setting, cập nhật UI theo trạng thái mới nhất
        if (Application.isPlaying)
        {
            LoadSettings();
        }
    }


    // =====================================================
    // LOAD SETTING
    // =====================================================

    public void LoadSettings()
    {
        isMusicOn =
            PlayerPrefs.GetInt("MusicOn", 1) == 1;

        isSoundOn =
            PlayerPrefs.GetInt("SoundOn", 1) == 1;

        isVibrationOn =
            PlayerPrefs.GetInt("VibrationOn", 1) == 1;

        UpdateMusicUI();
        UpdateSoundUI();
        UpdateVibrationUI();

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
        isMusicOn = !isMusicOn;

        PlayerPrefs.SetInt(
            "MusicOn",
            isMusicOn ? 1 : 0
        );

        PlayerPrefs.Save();

        UpdateMusicUI();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusic(isMusicOn);
        }
    }


    private void UpdateMusicUI()
    {
        // ON: hiện ảnh nốt nhạc bình thường
        musicOn.SetActive(isMusicOn);

        // OFF: hiện ảnh nốt nhạc có gạch chéo
        musicOff.SetActive(!isMusicOn);
    }


    // =====================================================
    // SOUND
    // =====================================================

    private void ToggleSound()
    {
        isSoundOn = !isSoundOn;

        PlayerPrefs.SetInt(
            "SoundOn",
            isSoundOn ? 1 : 0
        );

        PlayerPrefs.Save();

        UpdateSoundUI();

        if (AudioManager.Instance != null)
        {
            if (isSoundOn)
            {
                AudioManager.Instance.SetSound(true);
                AudioManager.Instance.PlayButtonClick();
            }
            else
            {
                AudioManager.Instance.SetSound(false);
            }
        }
    }


    private void UpdateSoundUI()
    {
        soundOn.SetActive(isSoundOn);
        soundOff.SetActive(!isSoundOn);
    }


    // =====================================================
    // VIBRATION
    // =====================================================

    private void ToggleVibration()
    {
        isVibrationOn = !isVibrationOn;

        PlayerPrefs.SetInt(
            "VibrationOn",
            isVibrationOn ? 1 : 0
        );

        PlayerPrefs.Save();

        UpdateVibrationUI();

        if (isVibrationOn)
        {
            Vibrate();
        }
    }


    private void UpdateVibrationUI()
    {
        vibrationOn.SetActive(isVibrationOn);
        vibrationOff.SetActive(!isVibrationOn);
    }


    // =====================================================
    // RUNG ĐIỆN THOẠI
    // =====================================================

    public void Vibrate()
    {
        bool vibrationEnabled =
            PlayerPrefs.GetInt("VibrationOn", 1) == 1;

        if (!vibrationEnabled)
        {
            return;
        }

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
    // XÓA LISTENER
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