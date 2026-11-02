using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField] private LevelLoader levelLoader;
    [SerializeField] private int currentLevelIndex = 1;

    [Header("Random Level Settings")]
    [Tooltip("Random trong bao nhiêu level cuối cùng của pool. VD: 20 = random trong 20 level cuối. Để 0 = random toàn bộ pool.")]
    [SerializeField] private int randomLastLevelCount = 0;

    private const string LEVEL_KEY = "CURRENT_LEVEL_INDEX";
    private const string LAST_RANDOM_LEVEL_KEY = "LAST_RANDOM_LEVEL_INDEX";
    private bool isEditorPlaytest;
    private string editorPlaytestJson;

#if UNITY_EDITOR
    private const string PlaytestActiveKey = "LevelEditor.Playtest.Active";
    private const string PlaytestJsonKey = "LevelEditor.Playtest.LevelJson";
    private const string EditorScenePathKey = "LevelEditor.Playtest.EditorScenePath";
#endif

    private void Awake()
    {
        if (Instance == null || Instance == this)
            Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
#if UNITY_EDITOR
        if (EditorPrefs.GetBool(PlaytestActiveKey, false))
        {
            editorPlaytestJson = EditorPrefs.GetString(PlaytestJsonKey, string.Empty);
            if (!string.IsNullOrEmpty(editorPlaytestJson))
            {
                isEditorPlaytest = true;
                PrepareEditorScenePlaytest();
                if (levelLoader != null)
                    levelLoader.LoadLevelFromJSON(editorPlaytestJson);
                else
                    Debug.LogError("[LevelManager] LevelLoader chưa được gán trong Core scene.", this);
                return;
            }
        }
#endif

        // Lấy level người chơi đang ở (mặc định là 1)
        currentLevelIndex = PlayerPrefs.GetInt(LEVEL_KEY, 1);
        LoadCurrentLevel();
    }

    public void LoadCurrentLevel()
    {
#if UNITY_EDITOR
        if (isEditorPlaytest)
        {
            if (levelLoader != null && !string.IsNullOrEmpty(editorPlaytestJson))
                levelLoader.LoadLevelFromJSON(editorPlaytestJson);
            return;
        }
#endif

        // Đọc file JSON từ thư mục Assets/Resources/Levels/Level_X.json
        TextAsset jsonAsset = Resources.Load<TextAsset>($"Levels/Level_{currentLevelIndex}");

        if (jsonAsset != null)
        {
            levelLoader.LoadLevelFromJSON(jsonAsset.text);
        }
        else
        {
            // 🔥 HẾT LEVEL MỚI — RANDOM LEVEL TỪ POOL CÓ SẴN
            LoadRandomLevel();
        }
    }

    /// <summary>
    /// Khi người chơi đã vượt qua tất cả level thiết kế sẵn,
    /// random chọn 1 level cũ để chơi lại (tránh trùng level vừa chơi).
    /// Level index (hiển thị trên UI) vẫn tăng dần bình thường.
    /// </summary>
    private void LoadRandomLevel()
    {
        // 1. Đếm tổng số level có sẵn trong Resources
        int totalLevels = CountAvailableLevels();

        if (totalLevels <= 0)
        {
            Debug.LogError("[LevelManager] Không tìm thấy level nào trong Resources/Levels!");
            return;
        }

        // 2. Xác định khoảng random: lấy XX level cuối của pool
        //    randomLastLevelCount = 0 → random toàn bộ pool
        //    randomLastLevelCount = 20 → chỉ random trong 20 level cuối
        int rangeTo = totalLevels;
        int rangeFrom = (randomLastLevelCount <= 0)
            ? 1
            : Mathf.Max(1, totalLevels - randomLastLevelCount + 1);

        // 3. Random tránh trùng level vừa chơi
        int lastRandomLevel = PlayerPrefs.GetInt(LAST_RANDOM_LEVEL_KEY, 0);
        int randomLevel;

        if (rangeFrom == rangeTo)
        {
            randomLevel = rangeFrom;
        }
        else
        {
            do
            {
                randomLevel = Random.Range(rangeFrom, rangeTo + 1);
            }
            while (randomLevel == lastRandomLevel);
        }

        // 4. Lưu lại level vừa random để lần sau tránh trùng
        PlayerPrefs.SetInt(LAST_RANDOM_LEVEL_KEY, randomLevel);
        PlayerPrefs.Save();

        // 5. Load level
        TextAsset randomAsset = Resources.Load<TextAsset>($"Levels/Level_{randomLevel}");
        if (randomAsset != null)
        {
            Debug.Log($"[LevelManager] 🎲 Random Level! Hiển thị Level {currentLevelIndex}, nội dung từ Level_{randomLevel} (range {rangeFrom}-{rangeTo})");
            levelLoader.LoadLevelFromJSON(randomAsset.text);
        }
        else
        {
            // Fallback: nếu random ra level không tồn tại, load Level_1
            Debug.LogWarning($"[LevelManager] Random ra Level_{randomLevel} nhưng không tìm thấy! Fallback về Level_1.");
            TextAsset fallback = Resources.Load<TextAsset>("Levels/Level_1");
            if (fallback != null) levelLoader.LoadLevelFromJSON(fallback.text);
        }
    }

    /// <summary>
    /// Đếm tổng số file Level_X.json có trong Resources/Levels/
    /// Quét từ Level_1 trở đi cho đến khi không tìm thấy file.
    /// </summary>
    private int CountAvailableLevels()
    {
        int count = 0;
        while (true)
        {
            TextAsset asset = Resources.Load<TextAsset>($"Levels/Level_{count + 1}");
            if (asset == null) break;
            count++;
        }
        return count;
    }

#if UNITY_EDITOR
    public void UpdateEditorPlaytestLevelJson(string json)
    {
        if (isEditorPlaytest && !string.IsNullOrEmpty(json))
            editorPlaytestJson = json;
    }
#endif

    public void NextLevel()
    {
#if UNITY_EDITOR
        if (isEditorPlaytest)
            return;
#endif

        currentLevelIndex++;
        PlayerPrefs.SetInt(LEVEL_KEY, currentLevelIndex);
        PlayerPrefs.Save();
        LoadCurrentLevel();
    }

    public void RestartLevel()
    {
        LoadCurrentLevel();
    }

#if UNITY_EDITOR
    private void PrepareEditorScenePlaytest()
    {
        string editorScenePath = EditorPrefs.GetString(EditorScenePathKey, "Assets/Scenes/editor.unity");
        Scene editorScene = SceneManager.GetSceneByPath(editorScenePath);
        if (!editorScene.IsValid() || !editorScene.isLoaded)
        {
            Debug.LogError("[LevelManager] Scene Editor chưa được tải trong phiên Playtest.", this);
            return;
        }

        if (SceneManager.GetActiveScene() != editorScene)
            SceneManager.SetActiveScene(editorScene);

        // The editor scene contains the editable layout and helper camera/environment.
        // Hide it for gameplay; the level is recreated from the saved preview JSON while
        // Core supplies the real cannon, UI, physics and block-destruction systems.
        foreach (GameObject root in editorScene.GetRootGameObjects())
        {
            if (root != null && root.activeSelf)
                root.SetActive(false);
        }
    }
#endif
}
