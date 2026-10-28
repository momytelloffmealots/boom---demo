using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class LevelEditorPlaytestSession
{
    private const string EditorScenePath = "Assets/Scenes/editor.unity";
    private const string CoreScenePath = "Assets/Scenes/core.unity";
    private const string SessionKey = "LevelEditor.Playtest.Session";
    private const string PreviewJsonKey = "LevelEditor.Playtest.LevelJson";
    private const string ActiveKey = "LevelEditor.Playtest.Active";
    private const string EditorScenePathKey = "LevelEditor.Playtest.EditorScenePath";

    [Serializable]
    private sealed class SessionSnapshot
    {
        public string editorScenePath;
        public string editorSceneName;
        public bool coreWasLoaded;
        public string[] activeEditorRoots;
    }

    static LevelEditorPlaytestSession()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        // EditorPrefs survive an Editor restart; discard an abandoned preview session.
        if (!SessionState.GetBool(SessionKey + ".active", false))
            ClearStoredPreview();
    }

    public static bool IsActive => SessionState.GetBool(SessionKey + ".active", false);

    public static bool TryStartPlaytest(LevelData levelData)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Playtest", "Hãy dừng Play Mode hiện tại trước khi chạy thử level.", "OK");
            return false;
        }

        Scene editorScene = SceneManager.GetActiveScene();
        if (!editorScene.IsValid() || !editorScene.isLoaded || editorScene.path != EditorScenePath)
        {
            EditorUtility.DisplayDialog("Playtest", "Hãy mở Assets/Scenes/editor.unity và đặt Scene này làm active.", "OK");
            return false;
        }

        List<string> rootsToRestore = new List<string>();
        foreach (GameObject root in editorScene.GetRootGameObjects())
        {
            if (root != null && root.activeSelf)
            {
                string id = GlobalObjectId.GetGlobalObjectIdSlow(root).ToString();
                if (!string.IsNullOrEmpty(id))
                    rootsToRestore.Add(id);
            }
        }

        Scene coreScene = SceneManager.GetSceneByPath(CoreScenePath);
        bool coreWasLoaded = coreScene.IsValid() && coreScene.isLoaded;
        SessionSnapshot snapshot = new SessionSnapshot
        {
            editorScenePath = editorScene.path,
            editorSceneName = editorScene.name,
            coreWasLoaded = coreWasLoaded,
            activeEditorRoots = rootsToRestore.ToArray()
        };

        SessionState.SetString(SessionKey, JsonUtility.ToJson(snapshot));
        SessionState.SetBool(SessionKey + ".active", true);
        EditorPrefs.SetBool(ActiveKey, true);
        EditorPrefs.SetString(PreviewJsonKey, JsonUtility.ToJson(levelData, true));
        EditorPrefs.SetString(EditorScenePathKey, editorScene.path);

        try
        {
            if (!coreWasLoaded)
                coreScene = EditorSceneManager.OpenScene(CoreScenePath, OpenSceneMode.Additive);

            if (!coreScene.IsValid() || !coreScene.isLoaded)
                throw new InvalidOperationException("Không mở được Assets/Scenes/core.unity để lấy hệ thống gameplay.");

            // OpenScene(Additive) can refresh Scene structs and may select the new scene.
            // Reacquire the editor scene and avoid reassigning it when it is already active.
            editorScene = SceneManager.GetSceneByPath(EditorScenePath);
            if (!editorScene.IsValid() || !editorScene.isLoaded)
                throw new InvalidOperationException("Assets/Scenes/editor.unity không còn được tải sau khi mở Core.");

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene != editorScene && !SceneManager.SetActiveScene(editorScene) &&
                SceneManager.GetActiveScene() != editorScene)
            {
                throw new InvalidOperationException("Không thể đặt Assets/Scenes/editor.unity làm Scene chạy thử.");
            }

            EditorApplication.isPlaying = true;
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            RestoreSession(snapshot);
            EditorUtility.DisplayDialog("Không thể chạy Playtest", exception.Message, "OK");
            return false;
        }
    }

    public static void StopPlaytest()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.isPlaying = false;
            return;
        }

        if (IsActive)
            RestoreAfterPlayMode();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode && IsActive)
            EditorApplication.delayCall += RestoreAfterPlayMode;
    }

    private static void RestoreAfterPlayMode()
    {
        if (!IsActive)
            return;

        string json = SessionState.GetString(SessionKey, string.Empty);
        SessionSnapshot snapshot = string.IsNullOrEmpty(json)
            ? null
            : JsonUtility.FromJson<SessionSnapshot>(json);

        RestoreSession(snapshot);
    }

    private static void RestoreSession(SessionSnapshot snapshot)
    {
        if (snapshot != null)
        {
            Scene editorScene = FindScene(snapshot.editorScenePath, snapshot.editorSceneName);
            if (editorScene.IsValid() && editorScene.isLoaded && snapshot.activeEditorRoots != null)
            {
                foreach (string id in snapshot.activeEditorRoots)
                {
                    if (!GlobalObjectId.TryParse(id, out GlobalObjectId globalId))
                        continue;

                    GameObject root = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId) as GameObject;
                    if (root != null && !root.activeSelf)
                        root.SetActive(true);
                }
            }

            Scene coreScene = SceneManager.GetSceneByPath(CoreScenePath);
            if (coreScene.IsValid() && coreScene.isLoaded && !snapshot.coreWasLoaded)
                EditorSceneManager.CloseScene(coreScene, true);

            editorScene = FindScene(snapshot.editorScenePath, snapshot.editorSceneName);
            if (editorScene.IsValid() && editorScene.isLoaded && SceneManager.GetActiveScene() != editorScene)
                SceneManager.SetActiveScene(editorScene);
        }

        SessionState.SetBool(SessionKey + ".active", false);
        SessionState.EraseString(SessionKey);
        ClearStoredPreview();
        SceneView.RepaintAll();
    }

    private static Scene FindScene(string path, string sceneName)
    {
        if (!string.IsNullOrEmpty(path))
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            if (scene.IsValid() && scene.isLoaded)
                return scene;
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.IsValid() && scene.isLoaded && scene.name == sceneName)
                return scene;
        }

        return default;
    }

    private static void ClearStoredPreview()
    {
        EditorPrefs.DeleteKey(ActiveKey);
        EditorPrefs.DeleteKey(PreviewJsonKey);
        EditorPrefs.DeleteKey(EditorScenePathKey);
    }
}
