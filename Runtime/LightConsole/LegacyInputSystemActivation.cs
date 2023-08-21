using UnityEngine;

namespace LightConsole
{
    [RequireComponent(typeof(LightConsoleController))]
    public class LegacyInputSystemActivation : MonoBehaviour
    {
        public string ActivationKeys = "~`";
        private LightConsoleController m_LightConsoleController;
    
        void Awake()
        {
            m_LightConsoleController = GetComponent<LightConsoleController>();
        }

        public void Update()
        {
            // Get 
            var keyboardInput = Input.inputString;
            for (var i = 0; i < keyboardInput.Length; i++)
            {
                for (var j = 0; j < ActivationKeys.Length; j++)
                {
                    if (keyboardInput[i] == ActivationKeys[j])
                    {
                        ToggleActivation();
                        return;
                    }
                }
            }
        }

        private void ToggleActivation()
        {
            m_LightConsoleController.ToggleActivation();
        }
    }
}
