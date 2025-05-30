using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using KRGPMagic.Core.Interfaces;
using KRGPMagic.Core.Models;
using System;

namespace KRGPMagic.Plugins.MultiTool
{
    // Команда А для MultiTool плагина.
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ToolACommand : IExternalCommand, IPlugin // Реализация IPlugin опциональна
    {
        #region IPlugin Implementation

        public PluginInfo Info { get; set; }
        public bool IsEnabled { get; set; }

        // Выполняет внутреннюю инициализацию плагина.
        public bool Initialize()
        {
            // Логика инициализации для ToolA, если необходимо   
            // TaskDialog.Show(Info?.Name ?? "ToolA", "ToolACommand Initialized.");
            return true;
        }

        // Освобождает ресурсы.
        public void Shutdown()
        {
            // Логика завершения для ToolA
            // TaskDialog.Show(Info?.Name ?? "ToolA", "ToolACommand Shutdown.");
        }

        #endregion

        #region IExternalCommand Implementation

        // Точка входа для выполнения команды.
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                string pluginDisplayName = (this is IPlugin p && p.Info?.SubCommands?.Find(sc => sc.ClassName == this.GetType().FullName)?.DisplayName != null)
                                           ? p.Info.SubCommands.Find(sc => sc.ClassName == this.GetType().FullName).DisplayName
                                           : "Инструмент А";


                TaskDialog.Show("MultiTool", $"Команда '{pluginDisplayName}' успешно выполнена! Ура, Товарищи!!!");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Execution Error", $"Ошибка при выполнении '{GetType().Name}': {ex.Message}");
                return Result.Failed;
            }
        }

        #endregion
    }
}
