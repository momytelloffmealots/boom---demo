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
            // BẢO VỆ: Nếu không tìm thấy Level (ví dụ chơi hết màn 4 mà chưa làm màn 5)
            // Tự động quay về Level 1 cho người chơi cày lại từ đầu
            Debug.LogWarning($"[LevelManager] Hết level rồi! Quay lại Level 1.");

            currentLevelIndex = 1;
            PlayerPrefs.SetInt(LEVEL_KEY, 1);
            PlayerPrefs.Save();

            // Load lại file Level_1
            TextAsset firstLevelAsset = Resources.Load<TextAsset>("Levels/Level_1");
            if (firstLevelAsset != null) levelLoader.LoadLevelFromJSON(firstLevelAsset.text);
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

