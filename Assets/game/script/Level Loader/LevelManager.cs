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

    private const string LEVEL_KEY = "CURRENT_LEVEL_INDEX";
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

