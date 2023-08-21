using System;
using LightConsole.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace LightConsole.UI
{
    public class LogListEntryController
    {
        /// <summary>
        /// Reference to the log label to style
        /// </summary>
        private Label m_LogEntryLabel;

        private VisualElement m_RootElement;

        private string m_LastClassId;
        
        /// <summary>
        /// Set the visual element used by this log entry controller
        /// </summary>
        /// <param name="visualElement"></param>
        public void SetVisualElement(VisualElement visualElement)
        {
            m_RootElement = visualElement;
            m_LogEntryLabel = visualElement.Q<Label>("log-string");
        }
        
        /// <summary>
        /// Assigns data for this log entry, to setup visual style
        /// (this will be reused in the list view)
        /// </summary>
        /// <param name="logEntry"></param>
        public void SetLogEntryData(LogEntry logEntry)
        {
            m_LogEntryLabel.text = logEntry.LogString;

            // Get matching class ID according to log type
            // (these contain overrides for icon and text)
            var classId = logEntry.LogType switch
            {
                LogType.Log => "log-log",
                LogType.Error => "log-error",
                LogType.Assert => "log-assert",
                LogType.Warning => "log-warning",
                LogType.Exception => "log-exception",
                _ => throw new ArgumentOutOfRangeException()
            };

            // No change
            if (m_LastClassId == classId) return;

            // Remove a previous applied class if needed
            if (!string.IsNullOrEmpty(m_LastClassId))
            {
                m_RootElement.RemoveFromClassList(m_LastClassId);
            }
            
            m_LastClassId = classId;
            // Assign new id
            m_RootElement.AddToClassList(classId);
        }

        
    }
}
