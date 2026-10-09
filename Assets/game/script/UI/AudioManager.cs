using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources Chung")]
    public AudioSource musicSource;   // Nhạc nền chung
    public AudioSource soundSource;   // Sound Effect chung

    [Header("Audio Sources Độc Lập (Kéo thả AudioSource vào đây)")]
    public AudioSource cannonSource;  // 🔥 MỚI: Nguồn phát riêng cho Pháo

    [Header("UI & Vũ khí")]
    public AudioClip buttonClickClip;

    [Header("Thắng / Thua")]
    public AudioClip winSFX;
    public AudioClip loseSFX;

    [Header("Đồng xu")]
    public AudioClip coinAppearSFX;
    public AudioClip coinReachSFX;

    [Header("Tên lửa (Prebooster)")]
    public AudioClip rocketFlySFX;
    public AudioClip rocketExplodeSFX;

    // Biến lưu trữ hệ thống làm mờ âm thanh
    private float defaultMusicVolume = 1f;
    private Coroutine fadeMusicCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        // Lưu lại mức âm lượng gốc của nhạc nền đang set trên Inspector
        if (musicSource != null)
        {
            defaultMusicVolume = musicSource.volume;
        }

        SyncAudioSettings();
    }

    // 🔥 MỚI: Tự động khôi phục nhạc nền về 100% mỗi khi Load lại Scene (Try Again / Next Level)
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RestoreBackgroundMusic(1f);
    }

    public void SyncAudioSettings()
    {
        bool musicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;
        bool soundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;

        SetMusic(musicOn);
        SetSound(soundOn);
    }

    public void SetMusic(bool isOn)
    {
        if (musicSource != null)
        {
            musicSource.mute = !isOn;
        }
    }

    public void SetSound(bool isOn)
    {
        if (soundSource != null)
        {
            soundSource.mute = !isOn;
        }

        // Đồng bộ luôn cho tiếng pháo (vì nó là một AudioSource tách biệt)
        if (cannonSource != null)
        {
            cannonSource.mute = !isOn;
        }
    }

    public void AddSoundToButtons(Button btnSound)
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Button btn in buttons)
        {
            if (btn == btnSound) continue;
            btn.onClick.RemoveListener(PlayButtonClick);
            btn.onClick.AddListener(PlayButtonClick);
        }
    }

    // =====================================================
    // HÀM PHÁT ÂM THANH GỐC
    // =====================================================
    public void PlaySound(AudioClip clip)
    {
        bool soundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;

        if (!soundOn || clip == null || soundSource == null) return;

        soundSource.PlayOneShot(clip);
    }

    // =====================================================
    // CÁC HÀM GỌI NHANH 
    // =====================================================

    public void PlayButtonClick() => PlaySound(buttonClickClip);

    // 🔥 MỚI: Dùng AudioSource riêng để bắn pháo
    public void PlayCannonShot()
    {
        bool soundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;

        // Dùng PlayOneShot trên cannonSource để giữ nguyên thông số Volume, Pitch bạn chỉnh ở Inspector
        // Đồng thời cho phép đạn bắn liên thanh đè lên nhau không bị ngắt quãng.
        if (soundOn && cannonSource != null && cannonSource.clip != null)
        {
            cannonSource.PlayOneShot(cannonSource.clip);
        }
    }

    public void PlayWinSound()
    {
        PlaySound(winSFX);
        FadeBackgroundMusic(0.2f, 1f); // 🔥 MỚI: Nhạc nền tự hạ xuống 20% trong 1 giây khi Thắng
    }

    public void PlayLoseSound()
    {
        PlaySound(loseSFX);
        FadeBackgroundMusic(0.2f, 1f); // 🔥 MỚI: Nhạc nền tự hạ xuống 20% trong 1 giây khi Thua
    }

    public void PlayCoinAppear() => PlaySound(coinAppearSFX);
    public void PlayCoinReach() => PlaySound(coinReachSFX);

    public void PlayRocketFly() => PlaySound(rocketFlySFX);
    public void PlayRocketExplode() => PlaySound(rocketExplodeSFX);

    // =====================================================
    // 🔥 MỚI: HỆ THỐNG AUDIO DUCKING (TỰ ĐỘNG CHỈNH ÂM LƯỢNG)
    // =====================================================

    /// <summary>
    /// Gọi hàm này nếu trong gameplay có khoảnh khắc nào đó bạn muốn nhạc tự động nhỏ lại
    /// </summary>
    public void FadeBackgroundMusic(float targetVolume, float duration = 0.5f)
    {
        if (musicSource == null) return;
        if (fadeMusicCoroutine != null) StopCoroutine(fadeMusicCoroutine);
        fadeMusicCoroutine = StartCoroutine(FadeMusicRoutine(targetVolume, duration));
    }

    /// <summary>
    /// Gọi hàm này nếu muốn nhạc tự động to lên trở lại bình thường
    /// </summary>
    public void RestoreBackgroundMusic(float duration = 0.5f)
    {
        if (musicSource == null) return;
        if (fadeMusicCoroutine != null) StopCoroutine(fadeMusicCoroutine);
        fadeMusicCoroutine = StartCoroutine(FadeMusicRoutine(defaultMusicVolume, duration));
    }

    private IEnumerator FadeMusicRoutine(float targetVolume, float duration)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Dùng unscaled để mượt mà ngay cả khi Time.timeScale = 0 (Pause game)
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        musicSource.volume = targetVolume;
    }
}