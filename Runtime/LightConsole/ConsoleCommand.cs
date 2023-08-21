using System;

namespace LightConsole
{
    /// <summary>
    /// Specifies a method to be registered as a command in LightConsole
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ConsoleCommand : Attribute
    {

        public string Name;
        public string Tooltip;
        
        public ConsoleCommand(string name="",string tooltip="")
        {
            Name = name;
            Tooltip = tooltip;
        }
    }
}