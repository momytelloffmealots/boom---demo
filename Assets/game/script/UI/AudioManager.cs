using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;   // Nhạc nền
    public AudioSource soundSource;   // Sound Effect

    [Header("UI & Vũ khí")]
    public AudioClip buttonClickClip; 
    public AudioClip cannonShotClip;

    [Header("Thắng / Thua")]
    public AudioClip winSFX;
    public AudioClip loseSFX;

    [Header("Đồng xu")]
    public AudioClip coinAppearSFX;
    public AudioClip coinReachSFX;

    [Header("Tên lửa (Prebooster)")]
    public AudioClip rocketFlySFX;
    public AudioClip rocketExplodeSFX;

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

        SyncAudioSettings();
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
    }

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
    // HÀM PHÁT ÂM THANH GỐC
    // =====================================================
    public void PlaySound(AudioClip clip)
    {
        bool soundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;

        if (!soundOn || clip == null || soundSource == null)
        {
            return;
        }
        
        soundSource.PlayOneShot(clip);
    }

    // =====================================================
    // CÁC HÀM GỌI NHANH (DÙNG CHO CÁC SCRIPT KHÁC GỌI SANG)
    // =====================================================
    
    public void PlayButtonClick() => PlaySound(buttonClickClip);
    public void PlayCannonShot() => PlaySound(cannonShotClip);
    
    public void PlayWinSound() => PlaySound(winSFX);
    public void PlayLoseSound() => PlaySound(loseSFX);
    
    public void PlayCoinAppear() => PlaySound(coinAppearSFX);
    public void PlayCoinReach() => PlaySound(coinReachSFX);
    
    public void PlayRocketFly() => PlaySound(rocketFlySFX);
    public void PlayRocketExplode() => PlaySound(rocketExplodeSFX);
}