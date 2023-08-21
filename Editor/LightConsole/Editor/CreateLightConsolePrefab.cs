using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LightConsole.Editor
{
    public static class CreateLightConsolePrefab
    {
        [MenuItem("Window/Light Console/Add Light Console to Active Scene")]
        public static void CreatePrefabInScene()
        {
            var lightConsoleInstances = UnityEngine.Object.FindObjectsOfType<LightConsoleController>();
            if (lightConsoleInstances.Length > 0)
            {
                Debug.LogWarning("Scene already has a LightConsole object. Remove before creating another");
                return;
            }
            
            var lightConsolePrefabSource=AssetDatabase.LoadAssetAtPath("Packages/com.simonoliver.lightconsole/Prefabs/LightConsole.prefab", typeof(GameObject));
            var lightConsoleInstance=(GameObject)PrefabUtility.InstantiatePrefab(lightConsolePrefabSource);
            lightConsoleInstance.transform.SetAsLastSibling();
            
#if ENABLE_INPUT_SYSTEM
            // If using new input system
            lightConsoleInstance.AddComponent<InputSystemActivation>();
#else
            // Legacy system
            lightConsoleInstance.AddComponent<LegacyInputSystemActivation>();
#endif
    
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            // TODO - Could enable both using #if ENABLE_LEGACY_INPUT_MANAGER

        }
    }
}