using System.Xml.Serialization;

namespace KRGPMagic.Core.Models
{
    // Содержит информацию для одной команды внутри SplitButton.
    public class SubCommandInfo
    {
        #region Properties

        // Внутреннее имя команды.
        [XmlElement("Name")]
        public string Name { get; set; }

        // Полное имя класса, реализующего IExternalCommand.
        [XmlElement("ClassName")]
        public string ClassName { get; set; }

        // Отображаемое имя на кнопке в выпадающем списке.
        [XmlElement("DisplayName")]
        public string DisplayName { get; set; }

        // Описание команды (tooltip).
        [XmlElement("Description")]
        public string Description { get; set; }

        // Путь к большой иконке относительно папки плагина.
        [XmlElement("LargeIcon")]
        public string LargeIcon { get; set; }

        // Путь к маленькой иконке относительно папки плагина.
        [XmlElement("SmallIcon")]
        public string SmallIcon { get; set; }

        #endregion
    }
}
