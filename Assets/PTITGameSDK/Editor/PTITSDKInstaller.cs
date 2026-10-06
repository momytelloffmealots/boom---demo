#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace PTITGameSDK.Editor
{
    [InitializeOnLoad]
    public static class PTITSDKInstaller
    {
        private static AddRequest addRequest;
        private static Queue<string> packagesToInstall = new Queue<string>();

        static PTITSDKInstaller()
        {
            // Delay auto-installation slightly to ensure Unity is fully loaded
            EditorApplication.delayCall += CheckAndInstallDependencies;
        }

        private static void CheckAndInstallDependencies()
        {
            string manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
            if (File.Exists(manifestPath))
            {
                string manifestJson = File.ReadAllText(manifestPath);
                
                // Check if LevelPlay is missing
                if (!manifestJson.Contains("com.unity.services.levelplay"))
                {
                    packagesToInstall.Enqueue("com.unity.services.levelplay");
                }
                
                // Check if Unity Purchasing (IAP) is missing
                if (!manifestJson.Contains("com.unity.purchasing"))
                {
                    packagesToInstall.Enqueue("com.unity.purchasing");
                }

                if (packagesToInstall.Count > 0)
                {
                    Debug.Log($"[PTITGameSDK] Found {packagesToInstall.Count} missing dependencies. Starting auto-install...");
                    ProcessNextPackage();
                }
            }
        }

        private static void ProcessNextPackage()
        {
            if (packagesToInstall.Count == 0) return;

            string packageId = packagesToInstall.Dequeue();
            Debug.Log($"[PTITGameSDK] Installing {packageId}...");
            addRequest = Client.Add(packageId);
            EditorApplication.update += Progress;
        }

        private static void Progress()
        {
            if (addRequest != null && addRequest.IsCompleted)
            {
                if (addRequest.Status == StatusCode.Success)
                    Debug.Log($"[PTITGameSDK] Successfully installed: {addRequest.Result.packageId}");
                else if (addRequest.Status >= StatusCode.Failure)
                    Debug.LogError($"[PTITGameSDK] Failed to install package. Error: {addRequest.Error.message}");

                EditorApplication.update -= Progress;
                addRequest = null;
                
                // Continue with next package
                ProcessNextPackage();
            }
        }
    }
}
#endif
