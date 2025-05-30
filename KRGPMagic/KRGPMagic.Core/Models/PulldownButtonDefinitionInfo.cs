using System.Xml.Serialization;

namespace KRGPMagic.Core.Models
{
    // Содержит информацию для определения PulldownButton.
    public class PulldownButtonDefinitionInfo
    {
        #region Properties

        // Уникальное имя (идентификатор) PulldownButton.
        [XmlElement("Name")]
        public string Name { get; set; }

        // Отображаемое имя на кнопке.
        [XmlElement("DisplayName")]
        public string DisplayName { get; set; }

        // Имя вкладки Revit.
        [XmlElement("RibbonTab")]
        public string RibbonTab { get; set; }

        // Имя панели Revit.
        [XmlElement("RibbonPanel")]
        public string RibbonPanel { get; set; }

        // Путь к большой иконке (относительно базовой директории KRGPMagic).
        [XmlElement("LargeIcon")]
        public string LargeIcon { get; set; }

        // Путь к маленькой иконке (относительно базовой директории KRGPMagic).
        [XmlElement("SmallIcon")]
        public string SmallIcon { get; set; }

        // Описание (tooltip).
        [XmlElement("Description")]
        public string Description { get; set; }

        #endregion
    }
}
