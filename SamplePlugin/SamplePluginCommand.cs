﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using KRGPMagic.Core.Interfaces;
using KRGPMagic.Core.Models;
using KRGPMagic.Core.Services;
using System;
using System.IO;

namespace KRGPMagic.Plugins.SamplePlugin
{
    /// <summary>
    /// Пример плагина, демонстрирующий использование централизованных сервисов KRGPMagic.
    /// Показывает, как плагин может использовать сервисы для работы с путями, инициализацией и сборками.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class SamplePluginCommand : IExternalCommand, IPlugin
    {
        #region IPlugin Implementation

        /// <summary>
        /// Конфигурационная информация о плагине, устанавливается системой.
        /// </summary>
        public PluginInfo Info { get; set; }

        /// <summary>
        /// Указывает, активен ли плагин.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Выполняет внутреннюю инициализацию плагина при загрузке системы.
        /// Использует централизованные сервисы для подготовки рабочей среды.
        /// </summary>
        /// <returns>True, если инициализация успешна.</returns>
        public bool Initialize()
        {
            try
            {
                // Получаем сервисы из провайдера
                var pathService = KRGPMagicServiceProvider.GetService<IPathService>();
                var initService = KRGPMagicServiceProvider.GetService<IPluginInitializationService>();

                if (pathService == null || initService == null)
                    return false;

                // Инициализируем плагин через сервис
                var pluginName = Info?.Name ?? "SamplePlugin";
                if (!initService.InitializePlugin(pluginName))
                    return false;

                // Создаем файл настроек плагина, если его нет
                var settingsPath = pathService.GetPluginUserDataFilePath(pluginName, "settings.txt");
                if (!File.Exists(settingsPath))
                {
                    File.WriteAllText(settingsPath, "SamplePlugin Settings\nInitialized: " + DateTime.Now);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Освобождает ресурсы, используемые плагином.
        /// </summary>
        public void Shutdown()
        {
            // Логика завершения работы плагина
        }

        #endregion

        #region IExternalCommand Implementation

        /// <summary>
        /// Точка входа для выполнения команды плагина, вызывается при нажатии кнопки в Revit.
        /// Демонстрирует использование централизованных сервисов.
        /// </summary>
        /// <param name="commandData">Данные, связанные с внешней командой.</param>
        /// <param name="message">Сообщение, которое может быть возвращено Revit в случае ошибки.</param>
        /// <param name="elements">Набор элементов, которые могут быть переданы команде.</param>
        /// <returns>Результат выполнения команды.</returns>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Получаем сервисы
                var pathService = KRGPMagicServiceProvider.GetService<IPathService>();
                var initService = KRGPMagicServiceProvider.GetService<IPluginInitializationService>();
                var assemblyService = KRGPMagicServiceProvider.GetService<IAssemblyService>();

                if (pathService == null || initService == null || assemblyService == null)
                {
                    TaskDialog.Show("Ошибка", "Сервисы KRGPMagic недоступны");
                    return Result.Failed;
                }

                // Получаем информацию о плагине
                string pluginName = Info?.DisplayName ?? "Sample Plugin";
                string pluginDataPath = pathService.GetPluginUserDataPath(Info?.Name ?? "SamplePlugin");
                string pluginStatus = initService.GetPluginStatus(Info?.Name ?? "SamplePlugin");

                // Формируем сообщение с информацией
                var infoMessage = $"Плагин: {pluginName}\n" +
                                 $"Статус: {pluginStatus}\n" +
                                 $"Папка данных: {pluginDataPath}\n" +
                                 $"Доступные сборки KRGP: {string.Join(", ", assemblyService.GetAvailableKRGPAssemblies())}";

                TaskDialog.Show("Информация о плагине", infoMessage);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Plugin Execution Error", $"Ошибка при выполнении плагина: {ex.Message}");
                return Result.Failed;
            }
        }

        #endregion
    }
}
