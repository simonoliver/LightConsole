using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LightConsole.Data;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
#endif

namespace LightConsole.UI
{
    public class LogListController
    {
        /// <summary>
        /// UXML template for log list entries
        /// </summary>
        private VisualTreeAsset m_LogListEntryTemplate;

        /// <summary>
        /// UXML template for stack trace list entries
        /// </summary>
        private VisualTreeAsset m_StackTraceListEntryTemplate;

        // UI element references
        private ListView m_LogList;
        private ListView m_StackTraceList;
        private TextField m_FilterTextField;

        // Toggles for filtering
        private Toggle m_ToggleLevelLog;
        private Toggle m_ToggleLevelWarning;
        private Toggle m_ToggleLevelError;
        private Toggle m_ToggleLevelException;
        private Toggle m_ToggleLevelAssert;

        private List<LogEntry> m_RawLogEntries = new();
        private List<LogEntry> m_FilteredLogEntries = new();
        private List<string> m_StackTraceEntries = new();

        private int m_ScrollToEndListFrameCountdown = 0;

        public void Initialise(VisualElement root, VisualTreeAsset listElementTemplate,
            VisualTreeAsset stackTraceElementTemplate)
        {
            // Store a reference to the template for the list entries

            m_LogListEntryTemplate = listElementTemplate;
            m_StackTraceListEntryTemplate = stackTraceElementTemplate;

            // Store a reference to the character list element
            m_LogList = root.Q<ListView>("log-list");
            m_StackTraceList = root.Q<ListView>("stack-trace-list");
            m_FilterTextField = root.Q<TextField>("filter-text-field");

            m_ToggleLevelLog = root.Q<Toggle>("toggle-level-log");
            m_ToggleLevelWarning = root.Q<Toggle>("toggle-level-warning");
            m_ToggleLevelError = root.Q<Toggle>("toggle-level-error");
            m_ToggleLevelException = root.Q<Toggle>("toggle-level-exception");
            m_ToggleLevelAssert = root.Q<Toggle>("toggle-level-assert");

            // Listen for changes in toggle filter
            m_FilterTextField.RegisterValueChangedCallback(FilterTextChanged);
            m_ToggleLevelLog.RegisterValueChangedCallback(LogToggleChanged);
            m_ToggleLevelWarning.RegisterValueChangedCallback(LogToggleChanged);
            m_ToggleLevelError.RegisterValueChangedCallback(LogToggleChanged);
            m_ToggleLevelException.RegisterValueChangedCallback(LogToggleChanged);
            m_ToggleLevelAssert.RegisterValueChangedCallback(LogToggleChanged);
                
            BindLogList();
            BindStackTraceList();

            // Register to get a callback when an item is selected
            m_LogList.selectionChanged += OnLogSelected;
            m_StackTraceList.selectionChanged += OnStackTraceSelected;

            m_LogList.itemsAdded += OnItemsAdded;
        }

        private void OnItemsAdded(IEnumerable<int> obj)
        {
            // Trying to workaround scroll not working until after set
            // Scroll to end of list
            m_LogList.ScrollToItem(-1);
        }

        public void AddLogEntry(LogEntry logEntry)
        {
            m_RawLogEntries.Add(logEntry);
            if (!TestPassFilter(logEntry, m_FilterTextField.text)) return;
            m_FilteredLogEntries.Add(logEntry);
            m_LogList.RefreshItems();

            // There must be a log entry at this point
            if (!Equals(m_LogList.itemsSource, m_FilteredLogEntries))
                m_LogList.itemsSource = m_FilteredLogEntries;
            
            // Scroll to end of list
            ScrollToEndOfList(true);
        }


        private void BindLogList()
        {
            // Set up a make item function for a list entry
            m_LogList.makeItem = () =>
            {
                // Instantiate the UXML template for the entry
                var newListEntry = m_LogListEntryTemplate.Instantiate();
                // Instantiate a controller for the data
                var newListEntryController = new LogListEntryController();
                // Assign the controller script to the visual element
                newListEntry.userData = newListEntryController;
                // Initialize the controller script
                newListEntryController.SetVisualElement(newListEntry);
                // Return the root of the instantiated visual tree
                return newListEntry;
            };

            // Set up bind function for a specific list entry
            m_LogList.bindItem = (item, index) =>
            {
                (item.userData as LogListEntryController)?.SetLogEntryData(m_FilteredLogEntries[index]);
            };

            // Set a fixed item height
            m_LogList.fixedItemHeight = 25;

            // Set the actual item's source list/array
            
            // Set to null so as not to have "List is empty" showing
            m_LogList.itemsSource = m_FilteredLogEntries.Count > 0 ? m_FilteredLogEntries : null;

            ScrollToEndOfList(false);
        }

        public void ScrollToEndOfList(bool delay)
        {
            m_LogList.RefreshItems();
            if (delay)
            {

                m_ScrollToEndListFrameCountdown = 2;
            }
            else
            {
                // Scroll straight away
                m_LogList.ScrollToItem(-1);
            }
                
        }

        private void BindStackTraceList()
        {
            m_StackTraceList.makeItem = () =>
            {
                // Instantiate the UXML template for the entry
                var newListEntry = m_StackTraceListEntryTemplate.Instantiate();
                // Instantiate a controller for the data
                var newListEntryController = new StackTraceListEntryController();
                // Assign the controller script to the visual element
                newListEntry.userData = newListEntryController;
                // Initialize the controller script
                newListEntryController.SetVisualElement(newListEntry);
                // Return the root of the instantiated visual tree
                return newListEntry;
            };

            // Set up bind function for a specific list entry
            m_StackTraceList.bindItem = (item, index) =>
            {
                (item.userData as StackTraceListEntryController)?.SetStackTraceData(m_StackTraceEntries[index]);
            };

            // Set a fixed item height
            m_StackTraceList.fixedItemHeight = 25;

            // Set the actual item's source list/array
            m_StackTraceList.itemsSource = m_StackTraceEntries.Count > 0 ? m_StackTraceEntries : null;
        }

        void OnLogSelected(IEnumerable<object> selectedItems)
        {
            m_StackTraceEntries.Clear();
            m_StackTraceList.selectedIndex = -1;
            // Get the currently selected item directly from the ListView
            var selectedLogEntry = m_LogList.selectedItem as LogEntry;
            if (selectedLogEntry == null) return;
            var stackTrace = selectedLogEntry.StackTrace;
            var stackTraceLines = stackTrace.Split(
                '\n',
                StringSplitOptions.None
            );
            m_StackTraceEntries.Add(selectedLogEntry.LogString);
            // Remove empty lines
            m_StackTraceEntries.AddRange(stackTraceLines.Where(line=>!string.IsNullOrEmpty(line)));
            // Ensure assigned
            m_StackTraceList.itemsSource = m_StackTraceEntries;
            m_StackTraceList.Rebuild();
        }

        //Debug.Log($"Stack trace lines {stackTraceLines.Length}");


        /*
        // Handle none-selection (Escape to deselect everything)
        if (selectedCharacter == null)
        {
            // Clear
            CharClassLabel.text = "";
            CharNameLabel.text = "";
            CharPortrait.style.backgroundImage = null;

            return;
        }

        // Fill in character details
        CharClassLabel.text = selectedCharacter.Class.ToString();
        CharNameLabel.text = selectedCharacter.CharacterName;
        CharPortrait.style.backgroundImage = new StyleBackground(selectedCharacter.PortraitImage);
        */

        // Launch editor
        // From https://stackoverflow.com/questions/49918539/how-to-wrap-unityengine-debug-log-but-keeping-the-line-of-code-when-clicked
        // https://discussions.unity.com/t/debugconsole-console-clicking/40086

        private void OnStackTraceSelected(IEnumerable<object> selectedItems)
        {
#if UNITY_EDITOR
            
            // Maybe include in future release - go to line number
            /*
            // Format is 
            // [METHOD] (at [FILENAME]:[LINE])
            var selectedStackTrace = m_StackTraceList.selectedItem as string;
            if (selectedStackTrace == null) return;
            var filePrefix = "(at ";
            var fileSuffix = ":";
            var fileDetailsIndex = selectedStackTrace.IndexOf(filePrefix);
            if (fileDetailsIndex > 0)
            {
                var fileDetails = selectedStackTrace.Substring(fileDetailsIndex + filePrefix.Length);
                var colonPos = fileDetails.LastIndexOf(fileSuffix, StringComparison.Ordinal);
                var fileName = fileDetails.Substring(0, colonPos);
                var lineNumberString = fileDetails.Substring(colonPos + 1,
                    fileDetails.IndexOf(")", StringComparison.Ordinal) - colonPos - 1);
                Debug.Log($"File '{fileName}' pos '{lineNumberString}'");
            }
            */


            //var file;
            //int lineNum;
            //InternalEditorUtility.OpenFileAtLineExternal()
#endif
        }
        
        private void LogToggleChanged(ChangeEvent<bool> evt)
        {
            UpdateFilter();
        }

        private void FilterTextChanged(ChangeEvent<string> evt)
        {
            UpdateFilter();
        }

        private bool TestPassFilter(LogEntry entry, string filterString)
        {
            if (!string.IsNullOrEmpty(filterString) &&
                !entry.LogString.ToLower().Contains(filterString.ToLower())) return false;

            switch (entry.LogType)
            {
                case LogType.Log when !m_ToggleLevelLog.value:
                case LogType.Error when !m_ToggleLevelError.value:
                case LogType.Warning when !m_ToggleLevelWarning.value:
                case LogType.Exception when !m_ToggleLevelException.value:
                case LogType.Assert when !m_ToggleLevelAssert.value:
                    return false;
                default:
                    return true;
            }
        }

        
        public void FrameUpdate()
        {
            // Workaround as can't update list pos immediately
            if (m_ScrollToEndListFrameCountdown > 0)
            {
                m_ScrollToEndListFrameCountdown--;
                if (m_ScrollToEndListFrameCountdown == 0)
                {
                    m_LogList.ScrollToItem(-1);
                }
            }
        }

        private void UpdateFilter()
        {
            m_LogList.Clear();
            m_LogList.selectedIndex = -1;
            m_FilteredLogEntries.Clear();
            var filterText = m_FilterTextField.text;
            
            // Add by filter
            m_FilteredLogEntries.AddRange(
            m_RawLogEntries.Where(entry => TestPassFilter(entry, filterText)).ToList());

            // Needs to be null or will show list is empty
            m_LogList.itemsSource=m_FilteredLogEntries.Count>0 ? m_FilteredLogEntries : null;
            
            m_LogList.RefreshItems();
            ScrollToEndOfList(true);
        }

        public void ClearLog()
        {
            m_LogList.Clear();
            m_RawLogEntries.Clear();
            m_FilteredLogEntries.Clear();
            m_LogList.RefreshItems();
            // Ensure dont show list is empty
            m_LogList.itemsSource = null;
        }
    }
}