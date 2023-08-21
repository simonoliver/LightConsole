using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace LightConsole
{

    public enum CommandType
    {
        Action,
        ReflectionMethod,
        InstanceReflectionMethod
    }
    
    public class CommandData
    {
        // Common
        public string Name;
        public string Tooltip;
        public CommandType CommandType;
        
        // For actions
        public Action CommandAction;
        
        // For reflection execution
        public MethodInfo CommandMethod;
        public object CommandObject;
        /// <summary>
        /// The name of the command object. If empty, becomes global command (no need for dot access)
        /// </summary>
        public string CommandObjectName;
    }
    
    public enum CommandExecuteResult
    {
        Success,
        EmptyCommandString,
        CommandNotFound,
        InvalidParameters,
        UnknownCommandType,
        TargetObjectDestroyed
    }
    
    public static class LightConsoleCommands
    {
        
        private static List<CommandData> s_RegisteredCommands = new();
        public static Action<CommandData> CommandAdded;
        public static Action<CommandData> CommandRemoved;

        public static List<CommandData> AllRegisteredCommands => s_RegisteredCommands;
        
        public static CommandData RegisterGlobalCommand(Action action, string name,string tooltip="")
        {
            var commandData = new CommandData
            {
                CommandAction = action,
                Name = StripSpaces(name),
                Tooltip = tooltip,
                CommandType=CommandType.Action
            };
            AddCommand(commandData);
            return commandData;
        }

        private static void RegisterObject(object targetObject,string objectName="")
        {
            var targetObjectType = targetObject.GetType();
            
            // Get all methods for target object
            var methods = targetObjectType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(m=>m.GetCustomAttributes(typeof(ConsoleCommand), false).Length > 0)
                .ToArray();


            bool isInstance = !string.IsNullOrEmpty(objectName);
            foreach (var method in methods)
            {
                var consoleCommandAttribute = (ConsoleCommand) method.GetCustomAttribute(typeof(ConsoleCommand));

                var commandData = new CommandData
                {
                    // If the name is blank, just use the function name. Also strip spaces
                    Name = string.IsNullOrEmpty(consoleCommandAttribute.Name) ? method.Name :  StripSpaces(consoleCommandAttribute.Name),
                    CommandType=(isInstance) ? CommandType.InstanceReflectionMethod : CommandType.ReflectionMethod,
                    CommandObject= targetObject,
                    CommandObjectName=objectName,
                    CommandMethod = method,
                    Tooltip=consoleCommandAttribute.Tooltip
                };
                AddCommand(commandData);
            }
        }

        public static void RegisterGlobalObject(object targetObject)
        {
            RegisterObject(targetObject);
        }
        
        public static void RegisterInstanceObject(MonoBehaviour targetMonoBehaviour)
        {
            RegisterInstanceObject(targetMonoBehaviour, targetMonoBehaviour.name);
        }

        public static void RegisterInstanceObject(object targetObject, string objectName)
        {
            RegisterObject(targetObject, objectName);
        }
        
        /// <summary>
        /// Unregisters a previously registered command
        /// </summary>
        /// <param name="commandData"></param>
        public static void UnregisterCommand(CommandData commandData)
        {
            if (commandData == null) return;
            s_RegisteredCommands.Remove(commandData);
            CommandRemoved?.Invoke(commandData);
        }
        
        /// <summary>
        /// Unregister all commands for an object
        /// </summary>
        /// <param name="targetObject"></param>
        public static void UnregisterObject(object targetObject)
        {
            for (var i=0;i<s_RegisteredCommands.Count;i++)
            {
                if (s_RegisteredCommands[i].CommandObject != targetObject) continue;
                var removedCommand = s_RegisteredCommands[i];
                s_RegisteredCommands.RemoveAt(i);
                CommandRemoved?.Invoke(removedCommand);
                i--;
            }
        }
        

        /// <summary>
        /// Removes spaces from a string (needed to allow commands to execute with space seperator
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        private static string StripSpaces(string inputString)
        {
            return string.Concat(inputString.Where(c => !char.IsWhiteSpace(c)));
        }

        
        private static void AddCommand(CommandData commandData)
        {
            s_RegisteredCommands.Add(commandData);
            CommandAdded?.Invoke(commandData);
        }

        /// <summary>
        /// Execute a command by string
        /// </summary>
        /// <param name="commandString"></param>
        public static (CommandExecuteResult,CommandData) ExecuteCommand(string commandString)
        {
            if (commandString.Length == 0) return (CommandExecuteResult.EmptyCommandString,null);


            var firstDotIndex = commandString.IndexOf('.');
            var firstSpaceIndex = commandString.IndexOf(' ');

            // Has a dot and dot is before the first space (if space exists)
            var isInstanceCall = (firstDotIndex >-1 &&  (firstSpaceIndex==-1 || firstSpaceIndex>firstDotIndex));
            
            var targetCommandName = (isInstanceCall)
                ? firstSpaceIndex > -1 ? commandString.Substring(firstDotIndex+1,firstSpaceIndex-firstDotIndex-1) : commandString.Substring(firstDotIndex+1)
                : firstSpaceIndex > -1 ? commandString[..firstSpaceIndex] : commandString;
            
            
            // Object name is everything before the dot
            var targetObjectName = isInstanceCall ? commandString[..firstDotIndex] : string.Empty;
            
            // Param string is everything after the space
            var paramString = firstSpaceIndex > -1 ? commandString[(firstSpaceIndex + 1)..] : string.Empty;
            // Use comma separator for params
            var paramsArray= paramString.Split(',').ToArray();

            // Find matching command. If instance call, look it up
            var matchingCommand = isInstanceCall
                ? s_RegisteredCommands.FirstOrDefault(commandData => string.Equals(commandData.Name, targetCommandName, StringComparison.CurrentCultureIgnoreCase) && string.Equals(commandData.CommandObjectName, targetObjectName, StringComparison.CurrentCultureIgnoreCase))
                : s_RegisteredCommands.FirstOrDefault(commandData => string.Equals(commandData.Name, targetCommandName, StringComparison.CurrentCultureIgnoreCase));
            return matchingCommand == null ? (CommandExecuteResult.CommandNotFound,null) :
                // Execute, passing remainder of split as params
                ExecuteCommand(matchingCommand, paramsArray);
        }

        /// <summary>
        /// Execute a given command
        /// </summary>
        /// <param name="commandData"></param>
        /// <param name="commandParameters"></param>
        public static (CommandExecuteResult,CommandData) ExecuteCommand(CommandData commandData, string[] commandParameters)
        {
            switch (commandData.CommandType)
            {
                case CommandType.Action:
                    // Simple action, just invoke
                    commandData.CommandAction?.Invoke();
                    return (CommandExecuteResult.Success,commandData);
                case CommandType.ReflectionMethod:
                case CommandType.InstanceReflectionMethod:
                    // Check object still exists
                    if (commandData.CommandObject == null) return (CommandExecuteResult.TargetObjectDestroyed,commandData);
                    
                    if (!HasParams(commandData))
                    {
                        commandData.CommandMethod.Invoke(commandData.CommandObject, new object[]{});
                        return (CommandExecuteResult.Success,commandData);
                    }
                    
                    // Generate parameters based on param string
                    var parameters = GenerateParametersForMethod(commandData.CommandMethod, commandParameters);
                    if (parameters == null) return (CommandExecuteResult.InvalidParameters,commandData);
                    commandData.CommandMethod.Invoke(commandData.CommandObject, parameters);
                    return (CommandExecuteResult.Success,commandData);
                
                default:
                    return (CommandExecuteResult.UnknownCommandType,commandData);
            }
        }

        private static object[] GenerateParametersForMethod(MethodInfo methodInfo, string[] commandParameters)
        {
            // Build queue from paramaters
            var paramQueue = new Queue<string>(commandParameters);
           
            var outputCommandParameters=new List<System.Object>();
            var methodParameters = methodInfo.GetParameters();
            foreach (var parameterInfo in methodParameters)
            {
                var paramType = parameterInfo.ParameterType;
                if (paramType == typeof(float))
                {
                    if (paramQueue.Count < 1) return null;
                    if (float.TryParse(paramQueue.Dequeue(), out var floatValue))
                        outputCommandParameters.Add(floatValue);
                    else return null; // Invalid
                }
                else if (paramType == typeof(int))
                {
                    if (paramQueue.Count < 1) return null;
                    if (int.TryParse(paramQueue.Dequeue(), out var intValue))
                        outputCommandParameters.Add(intValue);
                    else return null; // Invalid
                }
                else if (paramType == typeof(string))
                {
                    if (paramQueue.Count < 1) return null;
                    outputCommandParameters.Add(paramQueue.Dequeue());
                }
                else if (paramType == typeof(Vector2))
                {
                    var (parseSuccess, floatArray) = GetFloatParams(paramQueue, 2);
                    if (parseSuccess)
                        outputCommandParameters.Add(new Vector2(floatArray[0], floatArray[1]));
                    else return null;
                } 
                else if (paramType == typeof(Vector3))
                {
                    var (parseSuccess, floatArray) = GetFloatParams(paramQueue, 3);
                    if (parseSuccess)
                        outputCommandParameters.Add(new Vector3(floatArray[0], floatArray[1], floatArray[2]));
                    else return null;
                }
                else if (paramType == typeof(Vector4))
                {
                    var (parseSuccess, floatArray) = GetFloatParams(paramQueue, 4);
                    if (parseSuccess)
                        outputCommandParameters.Add(new Vector4(floatArray[0], floatArray[1], floatArray[2],floatArray[3]));
                    else return null;
                }
                else if (paramType == typeof(Vector2Int))
                {
                    var (parseSuccess, intArray) = GetIntParams(paramQueue, 2);
                    if (parseSuccess)
                        outputCommandParameters.Add(new Vector2Int(intArray[0], intArray[1]));
                    else return null;
                }
                else if (paramType == typeof(Vector3Int))
                {
                    var (parseSuccess, intArray) = GetIntParams(paramQueue, 3);
                    if (parseSuccess)
                        outputCommandParameters.Add(new Vector3Int(intArray[0], intArray[1],intArray[2]));
                    else return null;
                } 
                else if (paramType == typeof(Color))
                {
                    var (parseSuccess, floatArray) = GetFloatParams(paramQueue, 4);
                    if (parseSuccess)
                        outputCommandParameters.Add(new Color(floatArray[0], floatArray[1], floatArray[2],floatArray[3]));
                    else return null;
                }
            }

            return outputCommandParameters.ToArray();
            
        }

        /// <summary>
        /// Attempt to get a given number of float parameters from a queue
        /// </summary>
        /// <param name="paramQueue"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private static (bool,float[]) GetFloatParams(Queue<string> paramQueue,int count)
        {
            if (paramQueue.Count < count) return (false, null);
            var outputArray = new float[count];
            for (var i = 0; i < count; i++)
            {
                if (float.TryParse(paramQueue.Dequeue(), out var floatValue))
                    outputArray[i] = floatValue;
                else
                    return (false, null);
                
            }
            return (true, outputArray);
        }
        
        /// <summary>
        /// Attempt to get a given number of float parameters from a queue
        /// </summary>
        /// <param name="paramQueue"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private static (bool,int[]) GetIntParams(Queue<string> paramQueue,int count)
        {
            if (paramQueue.Count < count) return (false, null);
            var outputArray = new int[count];
            for (var i = 0; i < count; i++)
            {
                if (int.TryParse(paramQueue.Dequeue(), out var intValue))
                    outputArray[i] = intValue;
                else
                    return (false, null);
                
            }
            return (true, outputArray);
        }
        
        public static bool HasParams(CommandData commandData)
        {
            if (commandData.CommandType == CommandType.Action) return false;
            return commandData.CommandMethod.GetParameters().Length > 0;
        }

        public static string GetParamString(CommandData command)
        {
            var methodParameters = command.CommandMethod.GetParameters();
            var outputTypesList = new List<string>();
            foreach (var parameterInfo in methodParameters)
            {
                // TODO - Neaten this up a bit
                var paramType = parameterInfo.ParameterType;
                if (paramType == typeof(float)) outputTypesList.Add("float");
                if (paramType == typeof(int)) outputTypesList.Add("int");
                if (paramType == typeof(string)) outputTypesList.Add("string");
                if (paramType == typeof(Vector2)) outputTypesList.AddRange(new []{"float","float"});
                if (paramType == typeof(Vector3)) outputTypesList.AddRange(new []{"float","float","float"});
                if (paramType == typeof(Vector4)) outputTypesList.AddRange(new []{"float","float","float","float"});
                if (paramType == typeof(Vector2Int)) outputTypesList.AddRange(new []{"int","int"});
                if (paramType == typeof(Vector3Int)) outputTypesList.AddRange(new []{"int","int","int"});
                if (paramType == typeof(Color)) outputTypesList.AddRange(new []{"float","float","float","float"});
            }

            //var outNamesList = new List<string>();
            //foreach (var outputType in outputTypesList) outNamesList.Add(outputType.ToString());
            return string.Join(",",outputTypesList);
        }
    }
}
