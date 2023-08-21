#if ENABLE_INPUT_SYSTEM
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
namespace LightConsole
{

    [RequireComponent(typeof(LightConsoleController))]
    public class InputSystemActivation : MonoBehaviour
    {

        /// <summary>
        /// List of all paths that can be used to activate the console
        /// </summary>
        public List<string> ActivationPaths = new List<string>
        {
            "<Keyboard>/backslash",
            "<Keyboard>/backquote",
            "<Gamepad>/select"
        };
        
        /// <summary>
        /// Reference to target light console controller
        /// </summary>
        private LightConsoleController m_LightConsoleController;
        
        /// <summary>
        /// Event listener for button presses on input system
        /// </summary>
        private static IDisposable s_EventListener;
        
        
    
        void Awake()
        {
            m_LightConsoleController = GetComponent<LightConsoleController>();
            
            s_EventListener = InputSystem.onAnyButtonPress.Call(OnButtonPressed);
            
        }

        private void OnButtonPressed(InputControl button)
        {
            if (m_LightConsoleController == null) return;
            
            // TODO - find a neater way of substituting
            var devicePathSection="<Unknown>";
            if (button.device is Keyboard) devicePathSection="<Keyboard>";
            if (button.device is Gamepad) devicePathSection="<Gamepad>";
            if (button.device is Mouse) devicePathSection="<Mouse>";
            var fullPath = $"{devicePathSection}/{button.name}";
            
            foreach (var path in ActivationPaths)
            {
               if ( String.Equals(path, fullPath, StringComparison.CurrentCultureIgnoreCase))
               {
                   m_LightConsoleController.ToggleActivation();
                   return;
               }
            }
        }
    }

}

#endif