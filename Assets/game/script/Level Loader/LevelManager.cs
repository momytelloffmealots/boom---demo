using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private LevelLoader levelLoader;
    [SerializeField] private int currentLevelIndex = 1;

    private const string LEVEL_KEY = "CURRENT_LEVEL_INDEX";

    private void Start()
    {
        // Lấy level người chơi đang ở (mặc định là 1)
        currentLevelIndex = PlayerPrefs.GetInt(LEVEL_KEY, 1);
        LoadCurrentLevel();
    }

    public void LoadCurrentLevel()
    {
        // Đọc file JSON từ thư mục Assets/Resources/Levels/Level_X.json
        TextAsset jsonAsset = Resources.Load<TextAsset>($"Levels/Level_{currentLevelIndex}");

        if (jsonAsset != null)
        {
            levelLoader.LoadLevelFromJSON(jsonAsset.text);
        }
        else
        {
            Debug.LogError($"[LevelManager] Không tìm thấy file Levels/Level_{currentLevelIndex} trong Resources!");
        }
    }

    public void NextLevel()
    {
        currentLevelIndex++;
        PlayerPrefs.SetInt(LEVEL_KEY, currentLevelIndex);
        PlayerPrefs.Save();
        LoadCurrentLevel();
    }

    public void RestartLevel()
    {
        LoadCurrentLevel();
    }
}

