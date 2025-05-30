using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using KRGPMagic.Core.Interfaces;
using KRGPMagic.Core.Models;
using System;

namespace KRGPMagic.Plugins.MultiTool
{
    // Команда B для MultiTool плагина.
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ToolBCommand : IExternalCommand, IPlugin // Реализация IPlugin опциональна
    {
        #region IPlugin Implementation

        public PluginInfo Info { get; set; } // Будет установлено родительским PluginInfo для MultiToolPlugin
        public bool IsEnabled { get; set; }

        // Выполняет внутреннюю инициализацию плагина.
        public bool Initialize()
        {
            // Логика инициализации для ToolB, если необходимо
            // TaskDialog.Show(Info?.Name ?? "ToolB", "ToolBCommand Initialized.");
            return true;
        }

        // Освобождает ресурсы.
        public void Shutdown()
        {
            // Логика завершения для ToolB
            // TaskDialog.Show(Info?.Name ?? "ToolB", "ToolBCommand Shutdown.");
        }
        #endregion

        #region IExternalCommand Implementation

        // Точка входа для выполнения команды.
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Попытка получить DisplayName из SubCommandInfo
                string pluginDisplayName = "Инструмент Б"; // Значение по умолчанию
                if (this is IPlugin pluginInstance && pluginInstance.Info != null)
                {
                    var subCommand = pluginInstance.Info.SubCommands?.Find(sc => sc.ClassName == this.GetType().FullName);
                    if (subCommand != null)
                    {
                        pluginDisplayName = subCommand.DisplayName;
                    }
                }

                TaskDialog.Show("MultiTool", $"Команда '{pluginDisplayName}' успешно выполнена! И это не плохо)))");
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
