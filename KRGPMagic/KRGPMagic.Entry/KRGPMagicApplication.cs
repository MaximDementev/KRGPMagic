using Autodesk.Revit.UI;
using KRGPMagic.Core.Services;
using KRGPMagic.Services;
using System;
using System.IO;
using System.Reflection;

namespace KRGPMagic.Entry
{
    /// <summary>
    /// Главный класс приложения, точка входа для системы плагинов KRGPMagic в Revit.
    /// Отвечает за инициализацию менеджера плагинов, регистрацию сервисов и создание UI для них.
    /// </summary>
    public class KRGPMagicApplication : IExternalApplication
    {
        #region Fields

        private IPluginManager _pluginManager;
        private string _basePath; // Директория, где находится KRGPMagic.dll и KRGPMagic_Schema.xml

        #endregion

        #region Public Static Properties

        // Базовый путь к директории, где находится KRGPMagic.dll и папка Config.
        public static string KRGPMagicBasePath { get; private set; }

        #endregion

        #region IExternalApplication Implementation

        /// <summary>
        /// Вызывается при запуске Revit. Инициализирует систему плагинов и их UI.
        /// </summary>
        /// <param name="application">Контролируемое приложение Revit UI.</param>
        /// <returns>Результат операции.</returns>
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // _basePath теперь указывает на директорию, где лежит KRGPMagic.dll
                _basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                KRGPMagicBasePath = _basePath;

                InitializeServices(application);
                LoadPluginsAndCreateUI(application);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("KRGPMagic Startup Error", $"Ошибка при запуске системы KRGPMagic: {ex.Message}\n{ex.StackTrace}");
                return Result.Failed;
            }
        }

        /// <summary>
        /// Вызывается при завершении работы Revit. Освобождает ресурсы плагинов.
        /// </summary>
        /// <param name="application">Контролируемое приложение Revit UI.</param>
        /// <returns>Результат операции.</returns>
        public Result OnShutdown(UIControlledApplication application)
        {
            try
            {
                _pluginManager?.ShutdownPlugins();
                KRGPMagicServiceProvider.ClearAllServices();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("KRGPMagic Shutdown Error", $"Ошибка при завершении работы системы KRGPMagic: {ex.Message}");
                return Result.Failed;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Инициализирует и регистрирует все сервисы системы KRGPMagic.
        /// </summary>
        /// <param name="application">Контролируемое приложение Revit UI.</param>
        private void InitializeServices(UIControlledApplication application)
        {
            // Определяем версию Revit
            var revitVersion = "2022";

            // Создаем и регистрируем базовые сервисы
            var pathService = new PathService(revitVersion);
            KRGPMagicServiceProvider.RegisterService<IPathService>(pathService);

            var assemblyService = new AssemblyService(pathService);
            KRGPMagicServiceProvider.RegisterService<IAssemblyService>(assemblyService);

            var initializationService = new PluginInitializationService(pathService, assemblyService);
            KRGPMagicServiceProvider.RegisterService<IPluginInitializationService>(initializationService);

            // Создаем менеджер плагинов с зависимостями
            var configurationReader = new XmlConfigurationReader();
            var pluginLoader = new ReflectionPluginLoader();
            _pluginManager = new PluginManager(configurationReader, pluginLoader);

            // Валидируем окружение
            if (!initializationService.ValidateEnvironment())
            {
                TaskDialog.Show("KRGPMagic Warning", "Обнаружены проблемы с окружением. Некоторые функции могут работать некорректно.");
            }
        }

        /// <summary>
        /// Загружает плагины и создает для них элементы пользовательского интерфейса.
        /// </summary>
        /// <param name="application">Контролируемое приложение Revit UI.</param>
        private void LoadPluginsAndCreateUI(UIControlledApplication application)
        {
            var configurationFilePath = Path.Combine(_basePath, "KRGPMagic_Schema.xml");
            _pluginManager.LoadPlugins(configurationFilePath, _basePath); // _basePath для разрешения AssemblyPath
            _pluginManager.InitializePluginsAndCreateUI(application); // Новый метод
        }

        #endregion
    }
}
