using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using KRGPMagic.Entry; // Для доступа к KRGPMagicApplication.KRGPMagicBasePath
using System;
using System.IO;
using System.Windows.Forms; // Для DialogResult

namespace KRGPMagic.SchemaEditor
{
    // Команда Revit для запуска редактора XML-схемы KRGPMagic.
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class SchemaEditorCommand : IExternalCommand
    {
        #region IExternalCommand Implementation

        // Точка входа для команды. Открывает форму редактора схемы.
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (string.IsNullOrEmpty(KRGPMagicApplication.KRGPMagicBasePath))
                {
                    TaskDialog.Show("Ошибка Редактора Схемы", "Базовый путь KRGPMagic не инициализирован.");
                    return Result.Failed;
                }

                string schemaFilePath = "C:\\ProgramData\\Autodesk\\Revit\\Addins\\2022\\KRGPMagic\\KRGPMagic_Schema.xml";

                if (!File.Exists(schemaFilePath))
                {
                    TaskDialog.Show("Ошибка Редактора Схемы", $"Файл схемы не найден: {schemaFilePath}");
                    return Result.Failed;
                }

                using (var editorForm = new SchemaEditorForm(schemaFilePath))
                {
                    // Получаем HWND главного окна Revit для корректного модального отображения.
                    // Это важно, чтобы форма редактора была модальной относительно окна Revit.
                    IWin32Window revitWindow = new RevitWindowHandle(commandData.Application.MainWindowHandle);

                    if (editorForm.ShowDialog(revitWindow) == DialogResult.OK)
                    {
                        // Можно добавить сообщение об успешном сохранении, если это необходимо
                        // TaskDialog.Show("Редактор Схемы", "Схема успешно сохранена.");
                    }
                }
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Ошибка при запуске редактора схемы: {ex.ToString()}"; // ToString() для полного стека
                TaskDialog.Show("Критическая Ошибка Редактора", message);
                return Result.Failed;
            }
        }

        #endregion
    }

    // Вспомогательный класс для передачи HWND окна Revit в ShowDialog.
    internal class RevitWindowHandle : IWin32Window
    {
        #region Fields
        private readonly IntPtr _handle;
        #endregion

        #region Constructor
        public RevitWindowHandle(IntPtr handle)
        {
            _handle = handle;
        }
        #endregion

        #region IWin32Window Implementation
        public IntPtr Handle => _handle;
        #endregion
    }
}
