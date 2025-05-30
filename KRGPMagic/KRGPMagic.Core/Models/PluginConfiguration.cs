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

        #endregion
    }

    /// <summary>
    /// Содержит всю необходимую информацию для загрузки и инициализации одного плагина,
    /// включая данные для создания его пользовательского интерфейса в Revit.
    /// </summary>
    public class PluginInfo
    {
        #region Properties

        /// <summary>
        /// Уникальное имя плагина, используемое для идентификации.
        /// </summary>
        [XmlElement("Name")]
        public string Name { get; set; }

        /// <summary>
        /// Относительный путь к сборке (.dll) плагина.
        /// Путь отсчитывается от директории, где находится KRGPMagic.dll.
        /// Пример: "KRGPMagic.Plugins\SamplePlugin\SamplePlugin.dll"
        /// </summary>
        [XmlElement("AssemblyPath")]
        public string AssemblyPath { get; set; }

        /// <summary>
        /// Полное имя класса (включая пространство имен), который реализует IExternalCommand.
        /// Этот класс будет вызван при нажатии кнопки плагина.
        /// </summary>
        [XmlElement("ClassName")]
        public string ClassName { get; set; }

        /// <summary>
        /// Определяет, активен ли плагин. Если false, плагин не будет загружен и его UI не будет создан.
        /// </summary>
        [XmlElement("Enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// Определяет, должен ли плагин загружаться и его UI создаваться при старте Revit.
        /// Работает только если Enabled = true.
        /// </summary>
        [XmlElement("LoadOnStartup")]
        public bool LoadOnStartup { get; set; }

        /// <summary>
        /// Текст, отображаемый на кнопке плагина в ленте Revit.
        /// </summary>
        [XmlElement("DisplayName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Имя вкладки в ленте Revit, на которой будет размещена панель с кнопкой плагина.
        /// </summary>
        [XmlElement("RibbonTab")]
        public string RibbonTab { get; set; }

        /// <summary>
        /// Имя панели на вкладке, где будет размещена кнопка плагина.
        /// </summary>
        [XmlElement("RibbonPanel")]
        public string RibbonPanel { get; set; }

        /// <summary>
        /// Краткое описание функциональности плагина.
        /// </summary>
        [XmlElement("Description")]
        public string Description { get; set; }

        /// <summary>
        /// Версия плагина.
        /// </summary>
        [XmlElement("Version")]
        public string Version { get; set; }

        #endregion
    }
}
