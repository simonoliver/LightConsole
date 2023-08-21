using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace LightConsole.Editor
{
   
    static class LightConsoleBuildPostProcessor
    {
        /// <summary>
        /// Removes any instances to the LightConsole that are are not needed in this build
        /// </summary>
        [PostProcessScene]
        public static void OnPostprocessScene()
        {
            // Don't execute in editor play mode
            if (EditorApplication.isPlaying) return;

            // Find all light console references in this scene
            var lightConsoleInstances = UnityEngine.Object.FindObjectsOfType<LightConsoleController>();
            
            
            foreach (var consoleInstance in lightConsoleInstances)
            {
                // Check the defines for this instance
                var enableDefines = consoleInstance.EnableDefines;
                
                if (enableDefines == null || enableDefines.Count <= 0) continue;
                var foundMatchingDefine=false;
                foreach (var scriptingDefine in enableDefines)
                {
                    if (EditorUserBuildSettings.activeScriptCompilationDefines.Contains(scriptingDefine))
                    {
                        foundMatchingDefine = true;
                    }
                }
                // If defines are declared, but none found, destroy this object
                if (!foundMatchingDefine)
                {  
                    UnityEngine.Object.DestroyImmediate(consoleInstance.gameObject);
                }
            }
        }
    }
}
