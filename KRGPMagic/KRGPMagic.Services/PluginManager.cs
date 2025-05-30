using Autodesk.Revit.UI;
using KRGPMagic.Core.Interfaces;
using KRGPMagic.Core.Models; // Для PluginInfo
using System;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.IO; // Для Path
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging; // Для Assembly

namespace KRGPMagic.Services
{
    /// <summary>
    /// Реализация <see cref="IPluginManager"/>.
    /// Управляет загрузкой, инициализацией плагинов и созданием их UI.
    /// </summary>
    public class PluginManager : IPluginManager
    {
        #region Fields

        private readonly IConfigurationReader _configurationReader;
        private readonly IPluginLoader _pluginLoader;
        private readonly List<IPlugin> _loadedPlugins; // Хранит экземпляры плагинов, реализующих IPlugin
        private readonly List<PluginInfo> _allPluginInfos; // Хранит информацию обо всех плагинах из XML

        #endregion

        #region Constructor

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="PluginManager"/>.
        /// </summary>
        /// <param name="configurationReader">Сервис для чтения конфигурации.</param>
        /// <param name="pluginLoader">Сервис для загрузки плагинов.</param>
        public PluginManager(IConfigurationReader configurationReader, IPluginLoader pluginLoader)
        {
            _configurationReader = configurationReader ?? throw new ArgumentNullException(nameof(configurationReader));
            _pluginLoader = pluginLoader ?? throw new ArgumentNullException(nameof(pluginLoader));
            _loadedPlugins = new List<IPlugin>();
            _allPluginInfos = new List<PluginInfo>();
        }

        #endregion

        #region IPluginManager Implementation

        /// <summary>
        /// Получает коллекцию загруженных плагинов, реализующих IPlugin.
        /// </summary>
        public IReadOnlyCollection<IPlugin> LoadedPlugins => _loadedPlugins.AsReadOnly();

        /// <summary>
        /// Загружает информацию о плагинах из конфигурации и загружает сами плагины (если они реализуют IPlugin).
        /// </summary>
        /// <param name="configurationPath">Путь к файлу конфигурации.</param>
        /// <param name="basePath">Базовый путь для разрешения относительных путей сборок.</param>
        public void LoadPlugins(string configurationPath, string basePath)
        {
            if (string.IsNullOrWhiteSpace(configurationPath))
                throw new ArgumentException("Путь к файлу конфигурации не может быть пустым.", nameof(configurationPath));
            if (string.IsNullOrWhiteSpace(basePath))
                throw new ArgumentException("Базовый путь не может быть пустым.", nameof(basePath));

            try
            {
                var configuration = _configurationReader.ReadConfiguration(configurationPath);
                _allPluginInfos.Clear();
                _allPluginInfos.AddRange(configuration.Plugins);

                _loadedPlugins.Clear();
                // Загружаем только те плагины, которые реализуют IPlugin и предназначены для загрузки
                foreach (var pluginInfo in _allPluginInfos.Where(pi => pi.Enabled && pi.LoadOnStartup))
                {
                    try
                    {
                        IPlugin pluginInstance = _pluginLoader.LoadPlugin(pluginInfo, basePath);
                        if (pluginInstance != null) // LoadPlugin вернет null, если класс не реализует IPlugin или плагин отключен
                        {
                            _loadedPlugins.Add(pluginInstance);
                        }
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Plugin Load Error", $"Ошибка при попытке загрузить экземпляр плагина '{pluginInfo.Name}': {ex.Message}");
                        // Продолжаем загрузку других плагинов
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Критическая ошибка при загрузке конфигурации плагинов из '{configurationPath}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Инициализирует загруженные плагины (вызывает их метод Initialize)
        /// и создает элементы UI для всех сконфигурированных и активных плагинов.
        /// </summary>
        /// <param name="application">Контролируемое приложение Revit UI.</param>
        public void InitializePluginsAndCreateUI(UIControlledApplication application)
        {
            if (application == null) throw new ArgumentNullException(nameof(application));

            // Сначала инициализируем плагины, реализующие IPlugin
            foreach (var plugin in _loadedPlugins) // _loadedPlugins уже отфильтрованы по Enabled и LoadOnStartup
            {
                try
                {
                    plugin.Initialize(); // Вызываем Initialize без параметров
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Plugin Initialization Error", $"Ошибка при внутренней инициализации плагина '{plugin.Info?.Name}': {ex.Message}");
                }
            }

            // Затем создаем UI для всех плагинов, которые должны быть отображены при старте
            foreach (var pluginInfo in _allPluginInfos.Where(pi => pi.Enabled && pi.LoadOnStartup))
            {
                try
                {
                    CreatePluginUI(application, pluginInfo);
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Plugin UI Creation Error", $"Ошибка при создании UI для плагина '{pluginInfo.Name}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Выполняет процедуру завершения работы для всех загруженных плагинов (реализующих IPlugin).
        /// </summary>
        public void ShutdownPlugins()
        {
            foreach (var plugin in _loadedPlugins)
            {
                try
                {
                    plugin.Shutdown();
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Plugin Shutdown Error", $"Ошибка при завершении работы плагина '{plugin.Info?.Name}': {ex.Message}");
                }
            }
            _loadedPlugins.Clear();
            _allPluginInfos.Clear();
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Создает элементы пользовательского интерфейса для указанного плагина.
        /// </summary>
        /// <param name="application">Контролируемое приложение Revit UI.</param>
        /// <param name="pluginInfo">Конфигурационная информация о плагине.</param>
        private void CreatePluginUI(UIControlledApplication application, PluginInfo pluginInfo)
        {
            RibbonPanel ribbonPanel = GetOrCreateRibbonPanel(application, pluginInfo.RibbonTab, pluginInfo.RibbonPanel);

            // Путь к сборке плагина, где находится класс команды
            string assemblyLocation = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), // Базовый путь KRGPMagic.dll
                pluginInfo.AssemblyPath
            );

            if (!File.Exists(assemblyLocation))
            {
                TaskDialog.Show("Plugin UI Error", $"Не найдена сборка для плагина '{pluginInfo.Name}' по пути: {assemblyLocation}");
                return;
            }

            var pushButtonData = new PushButtonData(
                name: $"cmd_{pluginInfo.Name.Replace(" ", "_")}_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                text: pluginInfo.DisplayName,
                assemblyName: assemblyLocation, // Путь к DLL плагина
                className: pluginInfo.ClassName  // Класс, реализующий IExternalCommand
            );
            try
            {
                pushButtonData.LargeImage = LoadIcon(GetIconPath(pluginInfo, false));
                pushButtonData.Image = LoadIcon(GetIconPath(pluginInfo, true));
            }
            catch (Exception) { }

            ribbonPanel.AddItem(pushButtonData);
        }

        private BitmapImage LoadIcon(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    return new BitmapImage(new Uri(path));
                }
            }
            catch (Exception) { }
            return null;
        }

        private string GetIconPath(PluginInfo pluginInfo, bool isSmall)
        {
            string iconName = isSmall ? $"Image_small.png" : $"Image.png";
            string directory = Path.GetDirectoryName(Path.GetFullPath(pluginInfo.AssemblyPath));
            string path = Path.Combine(directory, iconName);
            return path;
        }

        /// <summary>
        /// Получает существующую панель на ленте или создает новую, если она не существует.
        /// Также создает вкладку, если она не существует.
        /// </summary>
        private RibbonPanel GetOrCreateRibbonPanel(UIControlledApplication application, string tabName, string panelName)
        {
            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch (Exception) { }
            // Ищем существующую панель
            var panels = application.GetRibbonPanels(tabName);
            var existingPanel = panels.FirstOrDefault(p => p.Name == panelName);

            if (existingPanel != null)
                return existingPanel;

            // Создаем новую панель
            return application.CreateRibbonPanel(tabName, panelName);
        }

        #endregion
    }
}
