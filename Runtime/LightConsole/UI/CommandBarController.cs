using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace LightConsole.UI
{
    public class CommandBarController
    {
        private VisualTreeAsset m_CommandButtonTemplate;
        private VisualElement m_RootVisualElement;
        private TextField m_CommandEntryTextField;
        private Label m_CommandResultLabel;
        private ScrollView m_CommandButtonsScrollView;

        private List<TemplateContainer> m_ActiveCommandButtonContainers = new();
        
        public CommandBarController()
        {
            
        }

        public void Initialise(VisualElement rootVisualElement,VisualTreeAsset commandButtonTemplate)
        {
            // Get relevant items
            m_RootVisualElement = rootVisualElement;
            m_CommandButtonTemplate = commandButtonTemplate;
            m_CommandEntryTextField=m_RootVisualElement.Q<TextField>("command-entry-textfield");

            m_CommandButtonsScrollView=m_RootVisualElement.Q<ScrollView>("command-buttons-scrollview");
            
            m_CommandResultLabel=m_RootVisualElement.Q<Label>("command-result-label");
            
            // Listen for command entry change
            m_CommandEntryTextField.RegisterCallback<KeyDownEvent>(CommandEntryKeyDown);
            m_CommandResultLabel.text = string.Empty; // Empty

            // Build command list for any commands that have already been registered
            BuildCommandList();
            
            // Listen for any added
            LightConsoleCommands.CommandAdded+=CommandAdded;
            LightConsoleCommands.CommandRemoved+=CommandRemoved;
        }

        public void SetCommandEntryFocus()
        {
            m_CommandEntryTextField.Focus();
        }

        private void CommandAdded(CommandData commandData)
        {
            CreateButton(commandData);
            SortButtons();
        }

        private void CommandRemoved(CommandData commandData)
        {
            RemoveButton(commandData);
            SortButtons();
        }


        private void CommandEntryKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return)
            {
                // On enter execute command
                ExecuteCommand();
            }

            if (evt.keyCode == KeyCode.Tab)
            {
                Debug.Log("Tab pressed");
            }
        }

        private void ExecuteCommand()
        {
            var commandString = m_CommandEntryTextField.text;
            var (result,command)=LightConsoleCommands.ExecuteCommand(commandString);
            if (result == CommandExecuteResult.Success)
            {
                // Clear
                m_CommandEntryTextField.value = string.Empty;
                m_CommandEntryTextField.Focus();
            }
            ShowResultForCommand(result, command,commandString);
        }

        private void ShowResultForCommand(CommandExecuteResult result, CommandData command,string input)
        {
            m_CommandResultLabel.text = result switch
            {
                CommandExecuteResult.Success => string.Empty,
                CommandExecuteResult.UnknownCommandType => $"Unknown command type '{input}'",
                CommandExecuteResult.CommandNotFound => $"Unknown Command '{input}'",
                CommandExecuteResult.InvalidParameters => $"Invalid parameters - should be '{command.Name} {LightConsoleCommands.GetParamString(command)}'",
                CommandExecuteResult.EmptyCommandString => string.Empty,
                CommandExecuteResult.TargetObjectDestroyed=> "Target object no longer exists",
                _ => "Unknown result"
            };
            m_CommandResultLabel.style.color = result switch
            {
                CommandExecuteResult.InvalidParameters => new StyleColor(Color.yellow),
                _ => new StyleColor(Color.red)
            };
        }


        private void BuildCommandList()
        {
            // Build all commands that may not have yet
            var allRegisteredCommands = LightConsoleCommands.AllRegisteredCommands;
            foreach (var command in allRegisteredCommands) CreateButton(command);
            SortButtons();
        }

        private void SortButtons()
        {
            // TODO - Implement
        }

        private void CreateButton(CommandData commandData)
        {
            // Clone button
            var buttonContainer=m_CommandButtonTemplate.Instantiate();
            m_CommandButtonsScrollView.Add(buttonContainer);
            
           
            var commandButton = buttonContainer.Q<Button>("command-button");
            // If instanced, show . notation
            commandButton.text = commandData.CommandType== CommandType.InstanceReflectionMethod
                ? $"{commandData.CommandObjectName}.{commandData.Name}"
                : commandData.Name;
            commandButton.tooltip = commandData.Tooltip;
            commandButton.userData = commandData;
            commandButton.RegisterCallback<ClickEvent>(CommandClicked);
            // Also associate with container, to help find when filtering
            buttonContainer.userData = commandData;
            
            m_ActiveCommandButtonContainers.Add(buttonContainer);
        }

        /// <summary>
        /// Removes a specific command button from the list
        /// </summary>
        /// <param name="commandData"></param>
        private void RemoveButton(CommandData commandData)
        {
            for (var i = 0; i < m_ActiveCommandButtonContainers.Count; i++)
            {
                var container = m_ActiveCommandButtonContainers[i];
                if (container.userData==commandData)
                {
                    // Remove from hierarchy and list
                    container.RemoveFromHierarchy();
                    m_ActiveCommandButtonContainers.RemoveAt(i);
                    // TODO - Does this also need to be destroyed?
                    return;
                }
            }
        }

        private void CommandClicked(ClickEvent evt)
        {
            // Has params?
            if (evt.currentTarget is Button commandButton)
            {
                var commandData = commandButton.userData as CommandData;
                if (commandData == null) return;
                if (LightConsoleCommands.HasParams(commandData))
                {
                   m_CommandEntryTextField.value =commandData.CommandType== CommandType.InstanceReflectionMethod
                       ? $"{commandData.CommandObjectName}.{commandData.Name} "
                       : $"{commandData.Name} ";
                   
                   m_CommandEntryTextField.Focus();
                   // Move cursor to the end
                   m_CommandEntryTextField.SelectNone();
                   m_CommandEntryTextField.cursorIndex = m_CommandEntryTextField.value.Length;
                   ShowParamsForCommand(commandData);
                }
                else
                {
                    LightConsoleCommands.ExecuteCommand(commandData, new string[] { });
                }
            }
        }

        private void ShowParamsForCommand(CommandData command)
        {
            m_CommandResultLabel.text = $"{command.Name} {LightConsoleCommands.GetParamString(command)}";
            m_CommandResultLabel.style.color = new StyleColor(Color.white);
        }
        
    }
}
