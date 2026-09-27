using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;   // Nhạc nền
    public AudioSource soundSource;   // Sound Effect

    [Header("Sound")]
    public AudioClip buttonClickClip; // Tiếng click button
    public AudioClip cannonShotClip;

    private void Awake()
    {
        // 🔥 FIX 2 LỖI NHẠC: Chặn đứng bản sao ngay lập tức
        if (Instance != null && Instance != this)
        {
            // Tắt ngay Object sao chép để AudioSource không có cơ hội phát tiếng
            gameObject.SetActive(false);
            Destroy(gameObject);
            return; // Ép dừng chạy code bên dưới
        }

        // Khởi tạo bản gốc
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        // Đồng bộ trạng thái nhạc ngay khi game vừa mở
        SyncAudioSettings();
    }

    // Hàm tự động đọc cài đặt để Mute/Unmute
    public void SyncAudioSettings()
    {
        bool musicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;
        bool soundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;

        SetMusic(musicOn);
        SetSound(soundOn);
    }

    // =====================================================
    // MUSIC
    // =====================================================

    public void SetMusic(bool isOn)
    {
        if (musicSource != null)
        {
            musicSource.mute = !isOn;
        }
    }

    // =====================================================
    // SOUND
    // =====================================================

    public void SetSound(bool isOn)
    {
        if (soundSource != null)
        {
            soundSource.mute = !isOn;
        }
    }

    // Phát tiếng click của Button
    public void PlayButtonClick()
    {
        PlaySound(buttonClickClip);
    }

    // Tự gắn tiếng vào Button
    public void AddSoundToButtons(Button btnSound)
    {
        Button[] buttons = FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Button btn in buttons)
        {
            if (btn == btnSound)
            {
                continue;
            }

            btn.onClick.RemoveListener(PlayButtonClick);
            btn.onClick.AddListener(PlayButtonClick);
        }
    }

    // =====================================================
    // PHÁT SOUND EFFECT KHÁC
    // =====================================================

    public void PlaySound(AudioClip clip)
    {
        bool soundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;

        if (!soundOn)
        {
            return;
        }

        if (soundSource != null && clip != null)
        {
            soundSource.PlayOneShot(clip);
        }
    }

    public void PlayCannonShot()
    {
        PlaySound(cannonShotClip);
    }
}