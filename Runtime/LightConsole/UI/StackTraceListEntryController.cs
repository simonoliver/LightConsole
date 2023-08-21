using LightConsole.Data;
using UnityEngine.UIElements;

namespace LightConsole.UI
{
    public class StackTraceListEntryController
    {
        /// <summary>
        /// Reference to the stack trace label
        /// </summary>
        private Label m_StackTraceEntryLabel;
        
        /// <summary>
        /// Set the visual element used by this log entry controller
        /// </summary>
        /// <param name="visualElement"></param>
        public void SetVisualElement(VisualElement visualElement)
        {
            m_StackTraceEntryLabel = visualElement.Q<Label>("stack-trace-string");
        }
        
        /// <summary>
        /// Assigns data for this stack trace
        /// </summary>
        /// <param name="stackTraceLine"></param>
        public void SetStackTraceData(string stackTraceLine)
        {
            m_StackTraceEntryLabel.text = stackTraceLine;
        }
    }
}
