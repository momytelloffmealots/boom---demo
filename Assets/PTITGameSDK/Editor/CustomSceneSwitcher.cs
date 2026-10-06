#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace CustomEditorTools
{
    [InitializeOnLoad]
    public static class CustomSceneSwitcher
    {
        private static ScriptableObject _toolbar;

        // Đăng ký dò tìm Toolbar khi Unity khởi động
        static CustomSceneSwitcher()
        {
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            if (_toolbar != null) return;

            Type toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
            if (toolbarType == null) return;

            var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
            if (toolbars.Length == 0) return;

            _toolbar = toolbars[0] as ScriptableObject;
            if (_toolbar != null)
            {
                InjectCustomUI();
            }
        }

        // Tiêm UI vào bên trái của Toolbar (gần nút Play ở giữa)
        private static void InjectCustomUI()
        {
            try
            {
                var rootField = _toolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
                if (rootField == null) return;

                var rawRoot = rootField.GetValue(_toolbar);
                var mRoot = rawRoot as VisualElement;
                if (mRoot == null) return;

                // ToolbarZoneLeftAlign nằm ngay bên trái vùng Play Mode (ở giữa)
                var leftZone = mRoot.Q("ToolbarZoneLeftAlign");
                if (leftZone != null)
                {
                    // Chống trùng lặp UI khi script recompile
                    if (leftZone.Q("CustomSceneSwitcherUI") != null) return;

                    var container = new IMGUIContainer(OnGUI);
                    container.name = "CustomSceneSwitcherUI";
                    
                    // Căn chỉnh khoảng cách
                    container.style.marginTop = 2;
                    container.style.marginBottom = 0;
                    container.style.marginLeft = 10;
                    container.style.marginRight = 10;
                    container.style.justifyContent = Justify.Center;

                    // Đưa vào cuối vùng bên trái -> Sẽ sát cạnh nút Play
                    leftZone.Add(container);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CustomToolbar] Không thể tiêm UI: {e.Message}");
            }
        }

        // Giao diện UI cải tiến
        private static void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUIStyle btnStyle = new GUIStyle(EditorStyles.toolbarDropDown);
            btnStyle.fontStyle = FontStyle.Bold;
            btnStyle.alignment = TextAnchor.MiddleCenter;

            var activeScene = EditorSceneManager.GetActiveScene();
            string sceneName = activeScene.IsValid() ? activeScene.name : "Unknown";
            string btnText = $" 🎬 {sceneName} ";

            if (GUILayout.Button(new GUIContent(btnText, "Switch Scenes"), btnStyle, GUILayout.MinWidth(110)))
            {
                ShowSceneMenu();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static void ShowSceneMenu()
        {
            GenericMenu menu = new GenericMenu();
            var scenes = EditorBuildSettings.scenes;

            if (scenes.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("No Scenes in Build Settings"));
            }
            else
            {
                var activeScenePath = EditorSceneManager.GetActiveScene().path;

                foreach (var scene in scenes)
                {
                    if (scene.enabled)
                    {
                        string sceneName = Path.GetFileNameWithoutExtension(scene.path);
                        // [BỌC THÉP]: Phải gán ra biến cục bộ để tránh lỗi Lambda Closure
                        string scenePath = scene.path;
                        bool isActive = scenePath == activeScenePath;

                        // Hiển thị checkmark cho Scene đang mở
                        menu.AddItem(new GUIContent(sceneName), isActive, () => OpenSceneSafe(scenePath));
                    }
                }
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("⚙ Open Build Settings..."), false, () => 
            {
                EditorWindow.GetWindow(Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
            });

            menu.ShowAsContext();
        }

        private static void OpenSceneSafe(string scenePath)
        {
            // [BỌC THÉP 1]: Ép tắt Play Mode nếu đang chạy game
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            // [BỌC THÉP 2]: Cảnh báo lưu Scene hiện tại nếu chưa Save
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }
    }
}
#endif