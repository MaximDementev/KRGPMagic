﻿using System.Xml.Serialization;
using System.ComponentModel;

namespace KRGPMagic.Core.Models
{
    // Содержит информацию для одной подкоманды внутри SplitButton.
    public class SubCommandInfo
    {
        #region Properties

        #region Основные параметры
        [Category("1. Основные параметры")]
        [DisplayName("ID (Имя)")]
        [Description("Уникальное внутреннее имя подкоманды. Пример: MySubCommandID")]
        [XmlElement("Name")]
        public string Name { get; set; }
        #endregion

        #region Сборка и класс
        [Category("2. Сборка и класс")]
        [DisplayName("Имя класса")]
        [Description("Полное имя класса (включая пространство имен), реализующего IExternalCommand для этой подкоманды. Пример: MyNamespace.MySubCommandHandler")]
        [XmlElement("ClassName")]
        public string ClassName { get; set; }
        #endregion

        #region Отображение в Revit UI
        [Category("3. Отображение в Revit UI")]
        [DisplayName("Отображаемое имя")]
        [Description("Текст, который будет виден пользователю на кнопке этой подкоманды в выпадающем списке SplitButton.")]
        [XmlElement("DisplayName")]
        public string DisplayName { get; set; }

        [Category("3. Отображение в Revit UI")]
        [DisplayName("Описание (Tooltip)")]
        [Description("Подробное описание функциональности подкоманды, отображаемое при наведении курсора.")]
        [XmlElement("Description")]
        public string Description { get; set; }

        [Category("3. Отображение в Revit UI")]
        [DisplayName("Иконка (большая)")]
        [Description("Относительный путь к файлу большой иконки (32x32 px) для этой подкоманды от папки плагина. Пример: Icons\\SubCmdA_32.png")]
        [XmlElement("LargeIcon")]
        public string LargeIcon { get; set; }

        [Category("3. Отображение в Revit UI")]
        [DisplayName("Иконка (маленькая)")]
        [Description("Относительный путь к файлу маленькой иконки (16x16 px) для этой подкоманды от папки плагина. Пример: Icons\\SubCmdA_16.png")]
        [XmlElement("SmallIcon")]
        public string SmallIcon { get; set; }
        #endregion

        #endregion
    }
}
