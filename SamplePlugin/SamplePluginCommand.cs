using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using KRGPMagic.Core.Interfaces; // Для IPlugin
using KRGPMagic.Core.Models;    // Для PluginInfo
using System;

namespace KRGPMagic.Plugins.SamplePlugin
{
    /// <summary>
    /// Пример плагина. Этот класс реализует <see cref="IExternalCommand"/> для выполнения основной логики
    /// и опционально <see cref="IPlugin"/> для дополнительной инициализации/завершения, не связанной с UI.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class SamplePluginCommand : IExternalCommand, IPlugin // Реализация IPlugin опциональна
    {
        #region IPlugin Implementation (Опционально)

        /// <summary>
        /// Конфигурационная информация о плагине, устанавливается системой.
        /// </summary>
        public PluginInfo Info { get; set; }

        /// <summary>
        /// Указывает, активен ли плагин.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Выполняет внутреннюю инициализацию плагина, если это необходимо.
        /// Не должен создавать элементы UI.
        /// </summary>
        /// <returns>True, если инициализация успешна.</returns>
        public bool Initialize()
        {
            // Здесь может быть логика, специфичная для плагина,
            // например, подписка на события Revit или загрузка внутренних ресурсов.
            // TaskDialog.Show(Info?.Name ?? "Sample Plugin", "Внутренняя инициализация SamplePluginCommand завершена.");
            return true;
        }

        /// <summary>
        /// Освобождает ресурсы, используемые плагином.
        /// </summary>
        public void Shutdown()
        {
            // Освобождение ресурсов, отписка от событий.
            // TaskDialog.Show(Info?.Name ?? "Sample Plugin", "Завершение работы SamplePluginCommand.");
        }

        #endregion

        #region IExternalCommand Implementation

        /// <summary>
        /// Точка входа для выполнения команды плагина, вызывается при нажатии кнопки в Revit.
        /// </summary>
        /// <param name="commandData">Данные, связанные с внешней командой.</param>
        /// <param name="message">Сообщение, которое может быть возвращено Revit в случае ошибки.</param>
        /// <param name="elements">Набор элементов, которые могут быть переданы команде.</param>
        /// <returns>Результат выполнения команды.</returns>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Получаем информацию о плагине, если она была установлена (если класс реализует IPlugin)
                string pluginName = "Sample Plugin";
                string pluginVersion = "1.0.2";
                string pluginDescription = "Описание не доступно.";

                if (this is IPlugin pluginInterface && pluginInterface.Info != null)
                {
                    pluginName = pluginInterface.Info.Name;
                    pluginVersion = pluginInterface.Info.Version;
                    pluginDescription = pluginInterface.Info.Description;
                }

                TaskDialog.Show(pluginName, $"Плагин '{pluginName}' (v{pluginVersion}) успешно выполнен!");
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
