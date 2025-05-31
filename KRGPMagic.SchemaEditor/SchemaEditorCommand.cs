using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using KRGPMagic.Entry;
using System;
using System.IO;
using System.Windows.Forms;

namespace KRGPMagic.SchemaEditor
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class SchemaEditorCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            string revitVersionYear = "2022"; // Получаем версию Revit, например "2022"
            string schemaFileName = "KRGPMagic_Schema.xml";
            string determinedSchemaFilePath = "";

            // 1. Пытаемся найти файл через KRGPMagicApplication.KRGPMagicBasePath (если он установлен)
            //    Это путь к директории, где лежит KRGPMagic.dll
            if (!string.IsNullOrEmpty(KRGPMagicApplication.KRGPMagicBasePath))
            {
                determinedSchemaFilePath = Path.Combine(KRGPMagicApplication.KRGPMagicBasePath, schemaFileName);
            }

            // 2. Если файл не найден через KRGPMagicBasePath или KRGPMagicBasePath не установлен,
            //    пытаемся найти по стандартному пути в ProgramData, используя текущую версию Revit.
            if (string.IsNullOrEmpty(determinedSchemaFilePath) || !File.Exists(determinedSchemaFilePath))
            {
                string programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                determinedSchemaFilePath = Path.Combine(programDataPath, "Autodesk", "Revit", "Addins", revitVersionYear, "KRGPMagic", schemaFileName);
            }

            string finalSchemaFilePath = determinedSchemaFilePath; // Это путь, который мы пытаемся использовать по умолчанию

            try
            {
                // Если зажата клавиша Shift, принудительно показываем диалог выбора файла
                if (System.Windows.Forms.Control.ModifierKeys == Keys.Shift)
                {
                    using (var openFileDialog = new OpenFileDialog())
                    {
                        string initialDir = Path.GetDirectoryName(finalSchemaFilePath);
                        if (string.IsNullOrEmpty(initialDir) || !Directory.Exists(initialDir))
                        {
                            // Безопасный fallback, если путь по умолчанию некорректен
                            initialDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        }
                        openFileDialog.InitialDirectory = initialDir;
                        openFileDialog.FileName = Path.GetFileName(finalSchemaFilePath);
                        openFileDialog.Filter = "KRGPMagic Schema Files (*.xml)|*.xml|All files (*.*)|*.*";
                        openFileDialog.Title = "Выберите файл схемы KRGPMagic (режим Shift)";
                        openFileDialog.CheckFileExists = false; // Позволяем выбрать/ввести несуществующий файл (для создания)

                        if (openFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            finalSchemaFilePath = openFileDialog.FileName; // Пользователь выбрал новый путь
                        }
                        else
                        {
                            TaskDialog.Show("Редактор Схемы", "Выбор файла отменен (в режиме Shift). Команда не будет выполнена.");
                            return Result.Cancelled; // Если Shift нажат, пользователь должен выбрать файл или отменить команду
                        }
                    }
                }

                // Если файл по итоговому пути (finalSchemaFilePath) не существует
                if (!File.Exists(finalSchemaFilePath))
                {
                    var dialogResult = MessageBox.Show($"Файл схемы '{finalSchemaFilePath}' не найден.\nСоздать новый пустой файл схемы по этому пути?",
                                                       "Файл не найден", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dialogResult == DialogResult.Yes)
                    {
                        try
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(finalSchemaFilePath)); // Создаем директорию, если ее нет
                            File.WriteAllText(finalSchemaFilePath,
                                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<KRGPMagicConfiguration>\n  <PulldownButtonDefinitions />\n  <Plugins />\n</KRGPMagicConfiguration>");
                        }
                        catch (Exception exCreate)
                        {
                            TaskDialog.Show("Ошибка Редактора Схемы", $"Не удалось создать файл схемы: {exCreate.Message}");
                            return Result.Failed;
                        }
                    }
                    else
                    {
                        TaskDialog.Show("Редактор Схемы", "Файл схемы не найден и не был создан. Редактор не будет запущен.");
                        return Result.Failed;
                    }
                }

                // На этом этапе finalSchemaFilePath должен указывать на существующий (возможно, только что созданный) файл
                using (var editorForm = new SchemaEditorForm(finalSchemaFilePath))
                {
                    IWin32Window revitWindow = new RevitWindowHandle(commandData.Application.MainWindowHandle);
                    editorForm.ShowDialog(revitWindow);
                }
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Ошибка при запуске редактора схемы: {ex.ToString()}";
                TaskDialog.Show("Критическая Ошибка Редактора", message);
                return Result.Failed;
            }
        }
    }

    // Вспомогательный класс для передачи HWND окна Revit в ShowDialog.
    internal class RevitWindowHandle : IWin32Window
    {
        private readonly IntPtr _handle;
        public RevitWindowHandle(IntPtr handle) { _handle = handle; }
        public IntPtr Handle => _handle;
    }
}
