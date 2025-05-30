using System.Collections.Generic;
using System.Xml.Serialization;

namespace KRGPMagic.Core.Models
{
    /// <summary>
    /// Корневой элемент конфигурации, содержащий список всех плагинов.
    /// </summary>
    [XmlRoot("KRGPMagicConfiguration")]
    public class PluginConfiguration
    {
        #region Properties

        /// <summary>
        /// Список информации о плагинах, загружаемый из KRGPMagic_Schema.xml.
        /// </summary>
        [XmlArray("Plugins")]
        [XmlArrayItem("Plugin")]
        public List<PluginInfo> Plugins { get; set; } = new List<PluginInfo>();

        /// <summary>
        /// Список определений кнопок выпадающего меню.
        /// </summary>
        [XmlArray("PulldownButtonDefinitions")]
        [XmlArrayItem("PulldownButtonDefinition")]
        public List<PulldownButtonDefinitionInfo> PulldownButtonDefinitions { get; set; } = new List<PulldownButtonDefinitionInfo>();

        #endregion
    }

}
