using System;
using System.Collections.Generic;
using LightConsole.Data;
using LightConsole.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace LightConsole
{
    public class LightConsoleController : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset m_LogListEntryTemplate;
        [SerializeField] private VisualTreeAsset m_StackTraceListEntryTemplate;
        [SerializeField] private VisualTreeAsset m_CommandButtonTemplate;
        /// <summary>
        /// If this is populated the console will be destroyed on build if one of these scripting defines is not present
        /// </summary>
        [SerializeField] private List<string> m_EnableDefines;

        /// <summary>
        /// If set, this will automatically show the console when an exception is thrown
        /// </summary>
        [SerializeField] private bool m_AutoShowOnException=true;
        /// <summary>
        /// Pause the game on show (set's timescale to 0
        /// </summary>
        [SerializeField] private bool m_PauseOnShow;

        private LogListController m_LogListController;
        private CommandBarController m_CommandBarController;
        private UIDocument m_Document;
        private VisualElement m_RootVisualElement;
        private VisualElement m_ConsoleVisualElement;
        
        private bool m_Visible;
        /// <summary>
        /// Cache time scale before pause
        /// </summary>
        private float m_TimeScaleBeforePause;

        public List<string> EnableDefines => m_EnableDefines;
        
        void Awake()
        {
            // Ensure remains between scenes
            DontDestroyOnLoad(gameObject);
            
            // Get a reference to the UI Document
            m_Document = GetComponent<UIDocument>();
            // Enable (disabled by default)
            m_Document.enabled = true;
            m_RootVisualElement = m_Document.rootVisualElement;
            
            // Listen to close events
            var closeButton=m_RootVisualElement.Q<Button>("close-console-button");
            closeButton.clicked += CloseButtonClicked;
            
            var clearLogButton=m_RootVisualElement.Q<Button>("clear-log-button");
            clearLogButton.clicked += ClearLogButtonClicked;
            
            m_ConsoleVisualElement=m_RootVisualElement.Q<VisualElement>("console");
            m_ConsoleVisualElement.RegisterCallback<TransitionEndEvent>(VisibilityTransitionComplete);
            
            // Initialize the log list controller
            m_LogListController = new LogListController();
            m_LogListController.Initialise(m_RootVisualElement, m_LogListEntryTemplate,m_StackTraceListEntryTemplate);

            m_CommandBarController = new CommandBarController();
            m_CommandBarController.Initialise(m_RootVisualElement,m_CommandButtonTemplate);
            
            // Listen to logs
            Application.logMessageReceivedThreaded += HandleLog;
            
            // Start hidden
            m_RootVisualElement.visible = false;
            m_Visible = false;
            m_ConsoleVisualElement.style.translate = new StyleTranslate(new Translate(0, Length.Percent(-100)));
        }
        
        public void Show()
        {
            m_RootVisualElement.visible = true;
            m_Visible = true;
            m_ConsoleVisualElement.style.translate = new StyleTranslate(new Translate(0, Length.Percent(0)));
            m_LogListController.ScrollToEndOfList(false);
            // Ensure focus on command bar for entry
            m_CommandBarController.SetCommandEntryFocus();
            
            if (m_PauseOnShow)
            {
                m_TimeScaleBeforePause = Time.timeScale;
                Time.timeScale = 0;
            }
        }

        public void Hide()
        { 
            m_Visible = false;
            m_ConsoleVisualElement.style.translate = new StyleTranslate(new Translate(0, Length.Percent(-100)));
            
            if (m_PauseOnShow)
            {
                // Restore time scale
                Time.timeScale=m_TimeScaleBeforePause;
            }
        }
        
        public void Update()
        {
            if (!m_Visible) return;
            m_LogListController.FrameUpdate();
        }

        private void CloseButtonClicked()
        {
            Hide();
        }
        
        private void ClearLogButtonClicked()
        {
            m_LogListController.ClearLog();
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            var logEntry = new LogEntry { LogString = logString, StackTrace = stackTrace, LogType = type };
            m_LogListController.AddLogEntry(logEntry);
            // If this is an exception and auto show on exceptions is enabled, launch
            if (logEntry.LogType == LogType.Exception && m_AutoShowOnException)
            {
                if (!m_Visible) Show();
            }
        }

        public void ToggleActivation()
        {
            if (!m_Visible) Show();
            else Hide();
        }
        
        /// <summary>
        /// Called after the slide transition complete. Sets to no rendering
        /// </summary>
        /// <param name="evt"></param>
        private void VisibilityTransitionComplete(TransitionEndEvent evt)
        {
            if (m_Visible)
            {
                m_CommandBarController.SetCommandEntryFocus();
            }
            else
            {
                m_RootVisualElement.visible = false;
            }
        }
    }
}
