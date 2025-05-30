using System.Collections.Generic;
using System.Xml.Serialization;

namespace KRGPMagic.Core.Models
{
    // Содержит информацию для одного плагина (который может быть PushButton или SplitButton).
    public class PluginInfo
    {
        #region Enums
        public enum ButtonUIType
        {
            PushButton,
            SplitButton
        }
        #endregion

        #region Properties

        [XmlElement("Name")]
        public string Name { get; set; }

        [XmlElement("AssemblyPath")]
        public string AssemblyPath { get; set; }

        [XmlElement("ClassName")]
        public string ClassName { get; set; }

        [XmlElement("Enabled")]
        public bool Enabled { get; set; }

        [XmlElement("LoadOnStartup")]
        public bool LoadOnStartup { get; set; }

        [XmlElement("DisplayName")]
        public string DisplayName { get; set; }

        [XmlElement("RibbonTab")]
        public string RibbonTab { get; set; }

        [XmlElement("RibbonPanel")]
        public string RibbonPanel { get; set; }

        // Имя группы PulldownButton, к которой принадлежит этот плагин. Если пусто, плагин добавляется напрямую на панель.
        [XmlElement("PulldownGroupName")]
        public string PulldownGroupName { get; set; }

        [XmlElement("Description")]
        public string Description { get; set; }

        [XmlElement("Version")]
        public string Version { get; set; }

        [XmlElement("UIType")]
        public ButtonUIType UIType { get; set; } = ButtonUIType.PushButton;

        [XmlElement("LargeIcon")]
        public string LargeIcon { get; set; }

        [XmlElement("SmallIcon")]
        public string SmallIcon { get; set; }

        [XmlArray("SubCommands")]
        [XmlArrayItem("Command")]
        public List<SubCommandInfo> SubCommands { get; set; } = new List<SubCommandInfo>();

        #endregion
    }
}
