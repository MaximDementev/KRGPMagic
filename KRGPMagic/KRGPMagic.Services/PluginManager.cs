﻿using Autodesk.Revit.UI;
using KRGPMagic.Core.Interfaces;
using KRGPMagic.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace KRGPMagic.Services
{
    // Управляет загрузкой, инициализацией плагинов и созданием их UI.
    public class PluginManager : IPluginManager
    {
        #region Fields

        private readonly IConfigurationReader _configurationReader;
        private readonly IPluginLoader _pluginLoader;
        private PluginConfiguration _pluginConfiguration;
        private readonly List<IPlugin> _loadedPlugins;
        private string _krgpMagicBasePath;
        private readonly Dictionary<string, PulldownButton> _createdPulldownButtons;

        #endregion

        #region Constructor

        public PluginManager(IConfigurationReader configurationReader, IPluginLoader pluginLoader)
        {
            _configurationReader = configurationReader ?? throw new ArgumentNullException(nameof(configurationReader));
            _pluginLoader = pluginLoader ?? throw new ArgumentNullException(nameof(pluginLoader));
            _loadedPlugins = new List<IPlugin>();
            _createdPulldownButtons = new Dictionary<string, PulldownButton>();
        }

        #endregion

        #region IPluginManager Implementation

        public IReadOnlyCollection<IPlugin> LoadedPlugins => _loadedPlugins.AsReadOnly();

        // Загружает конфигурацию и экземпляры плагинов.
        public void LoadPlugins(string configurationPath, string krgpMagicBasePath)
        {
            if (string.IsNullOrWhiteSpace(configurationPath))
                throw new ArgumentException("Путь к файлу конфигурации не может быть пустым.", nameof(configurationPath));
            if (string.IsNullOrWhiteSpace(krgpMagicBasePath))
                throw new ArgumentException("Базовый путь KRGPMagic не может быть пустым.", nameof(krgpMagicBasePath));

            _krgpMagicBasePath = krgpMagicBasePath;

            try
            {
                _pluginConfiguration = _configurationReader.ReadConfiguration(configurationPath);

                _loadedPlugins.Clear();
                foreach (var pluginInfo in _pluginConfiguration.Plugins.Where(pi => pi.Enabled && pi.LoadOnStartup))
                {
                    try
                    {
                        IPlugin pluginInstance = _pluginLoader.LoadPlugin(pluginInfo, _krgpMagicBasePath);
                        if (pluginInstance != null)
                        {
                            _loadedPlugins.Add(pluginInstance);
                        }
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Plugin Load Error", $"Ошибка при загрузке экземпляра плагина '{pluginInfo.Name}': {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Критическая ошибка при загрузке конфигурации плагинов из '{configurationPath}': {ex.Message}", ex);
            }
        }

        // Инициализирует плагины и создает UI.
        public void InitializePluginsAndCreateUI(UIControlledApplication application)
        {
            if (application == null) throw new ArgumentNullException(nameof(application));
            if (_pluginConfiguration == null)
            {
                TaskDialog.Show("Plugin UI Error", "Конфигурация плагинов не загружена.");
                return;
            }

            foreach (var plugin in _loadedPlugins)
            {
                try
                {
                    plugin.Initialize();
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Plugin Initialization Error", $"Ошибка при внутренней инициализации плагина '{plugin.Info?.Name}': {ex.Message}");
                }
            }

            var uiElementsByPanel = _pluginConfiguration.PulldownButtonDefinitions
                .Where(pbd => pbd.Enabled) // Учитываем активность PulldownButton
                .Cast<object>()
                .Concat(_pluginConfiguration.Plugins.Cast<object>())
                .Where(item =>
                {
                    if (item is PluginInfo pi) return pi.Enabled && pi.LoadOnStartup;
                    return true;
                })
                .GroupBy(item =>
                {
                    if (item is PulldownButtonDefinitionInfo pbd) return new { Tab = pbd.RibbonTab, Panel = pbd.RibbonPanel };
                    if (item is PluginInfo pi) return new { Tab = pi.RibbonTab, Panel = pi.RibbonPanel };
                    return null;
                })
                .Where(g => g.Key != null);

            foreach (var panelGroup in uiElementsByPanel)
            {
                var tabName = panelGroup.Key.Tab;
                var panelName = panelGroup.Key.Panel;
                RibbonPanel ribbonPanel = GetOrCreateRibbonPanel(application, tabName, panelName);

                // Создаем только активные PulldownButton
                foreach (var pbdInfo in panelGroup.OfType<PulldownButtonDefinitionInfo>().Where(pbd => pbd.Enabled))
                {
                    CreateActualPulldownButton(ribbonPanel, pbdInfo);
                }

                foreach (var pluginInfo in panelGroup.OfType<PluginInfo>())
                {
                    string pluginAssemblyFullPath = Path.Combine(_krgpMagicBasePath, pluginInfo.AssemblyPath);
                    string pluginAssemblyDir = Path.GetDirectoryName(pluginAssemblyFullPath);

                    if (!File.Exists(pluginAssemblyFullPath))
                    {
                        TaskDialog.Show("Plugin UI Error", $"Сборка не найдена для плагина '{pluginInfo.Name}': {pluginAssemblyFullPath}");
                        continue;
                    }

                    if (!string.IsNullOrEmpty(pluginInfo.PulldownGroupName))
                    {
                        // Проверяем, что PulldownButton, к которому привязывается плагин, активен
                        var targetPulldownDef = _pluginConfiguration.PulldownButtonDefinitions
                            .FirstOrDefault(pbd => pbd.Name == pluginInfo.PulldownGroupName && pbd.RibbonTab == tabName && pbd.RibbonPanel == panelName);

                        if (targetPulldownDef != null && targetPulldownDef.Enabled)
                        {
                            string pulldownKey = GeneratePulldownKey(tabName, panelName, pluginInfo.PulldownGroupName);
                            if (_createdPulldownButtons.TryGetValue(pulldownKey, out PulldownButton pulldownButton))
                            {
                                AddItemToPulldownButton(pulldownButton, pluginInfo, pluginAssemblyFullPath, pluginAssemblyDir);
                            }
                            else
                            {
                                TaskDialog.Show("Plugin UI Warning", $"PulldownButton '{pluginInfo.PulldownGroupName}' определен, но не был создан (возможно, из-за ошибки). Плагин '{pluginInfo.Name}' не будет добавлен в него.");
                            }
                        }
                        else if (targetPulldownDef == null)
                        {
                            TaskDialog.Show("Plugin UI Warning", $"PulldownButton '{pluginInfo.PulldownGroupName}' не определен для панели '{panelName}'. Плагин '{pluginInfo.Name}' не будет добавлен.");
                        }
                        // Если targetPulldownDef.Enabled == false, плагин просто не добавляется в него, сообщение не обязательно
                    }
                    else
                    {
                        AddItemToPanel(ribbonPanel, pluginInfo, pluginAssemblyFullPath, pluginAssemblyDir);
                    }
                }
            }
        }

        // Завершение работы плагинов.
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
            _createdPulldownButtons.Clear();
            _pluginConfiguration = null;
        }

        #endregion

        #region Private UI Creation Methods

        // Генерирует ключ для словаря _createdPulldownButtons.
        private string GeneratePulldownKey(string tabName, string panelName, string pulldownName)
        {
            return $"{tabName}_{panelName}_{pulldownName}";
        }

        // Создает и регистрирует PulldownButton, если он активен.
        private void CreateActualPulldownButton(RibbonPanel ribbonPanel, PulldownButtonDefinitionInfo pbdInfo)
        {
            if (!pbdInfo.Enabled) return; // Не создаем, если отключен

            string pulldownKey = GeneratePulldownKey(pbdInfo.RibbonTab, pbdInfo.RibbonPanel, pbdInfo.Name);
            if (_createdPulldownButtons.ContainsKey(pulldownKey)) return;

            var pulldownButtonData = new PulldownButtonData(
                name: $"cmd_pulldown_{pbdInfo.Name.Replace(" ", "_")}_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                text: pbdInfo.DisplayName
            );

            if (!string.IsNullOrEmpty(pbdInfo.Description))
            {
                pulldownButtonData.ToolTip = pbdInfo.Description;
            }

            string largeIconPath = string.IsNullOrEmpty(pbdInfo.LargeIcon) ? null : Path.Combine(_krgpMagicBasePath, pbdInfo.LargeIcon);
            string smallIconPath = string.IsNullOrEmpty(pbdInfo.SmallIcon) ? null : Path.Combine(_krgpMagicBasePath, pbdInfo.SmallIcon);

            pulldownButtonData.LargeImage = LoadBitmapImage(largeIconPath);
            pulldownButtonData.Image = LoadBitmapImage(smallIconPath);

            var pulldownButton = ribbonPanel.AddItem(pulldownButtonData) as PulldownButton;
            if (pulldownButton != null)
            {
                _createdPulldownButtons[pulldownKey] = pulldownButton;
            }
            else
            {
                TaskDialog.Show("Plugin UI Error", $"Не удалось создать PulldownButton '{pbdInfo.DisplayName}'.");
            }
        }

        // Добавляет элемент (PushButton или SplitButton) на панель.
        private void AddItemToPanel(RibbonPanel ribbonPanel, PluginInfo pluginInfo, string pluginAssemblyFullPath, string pluginAssemblyDir)
        {
            if (pluginInfo.UIType == PluginInfo.ButtonUIType.SplitButton)
            {
                CreateSplitButtonOnPanel(ribbonPanel, pluginInfo, pluginAssemblyFullPath, pluginAssemblyDir);
            }
            else
            {
                CreatePushButtonOnPanel(ribbonPanel, pluginInfo, pluginAssemblyFullPath, pluginAssemblyDir);
            }
        }

        // Добавляет элемент (PushButton или SplitButton) в PulldownButton.
        private void AddItemToPulldownButton(PulldownButton pulldownButton, PluginInfo pluginInfo, string pluginAssemblyFullPath, string pluginAssemblyDir)
        {
            if (pluginInfo.UIType == PluginInfo.ButtonUIType.SplitButton)
            {
                if (!string.IsNullOrEmpty(pluginInfo.ClassName))
                {
                    var mainPushButtonData = PreparePushButtonData(pluginInfo, pluginAssemblyFullPath, pluginAssemblyDir);
                    pulldownButton.AddPushButton(mainPushButtonData);
                    if (pluginInfo.SubCommands.Any()) pulldownButton.AddSeparator();
                }

                foreach (var subCommandInfo in pluginInfo.SubCommands)
                {
                    var subPushButtonData = new PushButtonData(
                        name: $"cmd_sub_pd_{subCommandInfo.Name.Replace(" ", "_")}_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                        text: subCommandInfo.DisplayName,
                        assemblyName: pluginAssemblyFullPath,
                        className: subCommandInfo.ClassName
                    );
                    if (!string.IsNullOrEmpty(subCommandInfo.Description)) subPushButtonData.ToolTip = subCommandInfo.Description;

                    string subLargeIconPath = string.IsNullOrEmpty(subCommandInfo.LargeIcon) ? null : Path.Combine(pluginAssemblyDir, subCommandInfo.LargeIcon);
                    string subSmallIconPath = string.IsNullOrEmpty(subCommandInfo.SmallIcon) ? null : Path.Combine(pluginAssemblyDir, subCommandInfo.SmallIcon);
                    subPushButtonData.LargeImage = LoadBitmapImage(subLargeIconPath);
                    subPushButtonData.Image = LoadBitmapImage(subSmallIconPath);

                    pulldownButton.AddPushButton(subPushButtonData);
                }
            }
            else
            {
                var pushButtonData = PreparePushButtonData(pluginInfo, pluginAssemblyFullPath, pluginAssemblyDir);
                pulldownButton.AddPushButton(pushButtonData);
            }
        }

        // Подготавливает PushButtonData.
        private PushButtonData PreparePushButtonData(PluginInfo pluginInfo, string pluginAssemblyFullPath, string pluginAssemblyDir)
        {
            var pushButtonData = new PushButtonData(
                name: $"cmd_pb_{pluginInfo.Name.Replace(" ", "_")}_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                text: pluginInfo.DisplayName,
                assemblyName: pluginAssemblyFullPath,
                className: pluginInfo.ClassName
            );
            if (!string.IsNullOrEmpty(pluginInfo.Description)) pushButtonData.ToolTip = pluginInfo.Description;

            string largeIconPath = string.IsNullOrEmpty(pluginInfo.LargeIcon) ? null : Path.Combine(pluginAssemblyDir, pluginInfo.LargeIcon);
            string smallIconPath = string.IsNullOrEmpty(pluginInfo.SmallIcon) ? null : Path.Combine(pluginAssemblyDir, pluginInfo.SmallIcon);
            pushButtonData.LargeImage = LoadBitmapImage(largeIconPath);
            pushButtonData.Image = LoadBitmapImage(smallIconPath);
            return pushButtonData;
        }

        // Создает PushButton на панели.
        private void CreatePushButtonOnPanel(RibbonPanel ribbonPanel, PluginInfo pluginInfo, string pluginAssemblyFullPath, string pluginAssemblyDir)
        {
            var pushButtonData = PreparePushButtonData(pluginInfo, pluginAssemblyFullPath, pluginAssemblyDir);
            ribbonPanel.AddItem(pushButtonData);
        }

        // Подготавливает SplitButtonData.
        private SplitButtonData PrepareSplitButtonData(PluginInfo pluginInfo, string pluginAssemblyFullPath, string pluginAssemblyDir)
        {
            var splitButtonData = new SplitButtonData(
                 name: $"cmd_sb_{pluginInfo.Name.Replace(" ", "_")}_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                 text: pluginInfo.DisplayName
            );
            if (!string.IsNullOrEmpty(pluginInfo.Description)) splitButtonData.ToolTip = pluginInfo.Description;

            string largeIconPath = string.IsNullOrEmpty(pluginInfo.LargeIcon) ? null : Path.Combine(pluginAssemblyDir, pluginInfo.LargeIcon);
            splitButtonData.LargeImage = LoadBitmapImage(largeIconPath);

            return splitButtonData;
        }

        // Создает SplitButton на панели и наполняет его подкомандами.
        private void CreateSplitButtonOnPanel(RibbonPanel ribbonPanel, PluginInfo pluginInfo, string pluginAssemblyFullPath, string pluginAssemblyDir)
        {
            var splitButtonData = PrepareSplitButtonData(pluginInfo, pluginAssemblyFullPath, pluginAssemblyDir);
            SplitButton splitButton = ribbonPanel.AddItem(splitButtonData) as SplitButton;

            if (splitButton != null)
            {
                foreach (var subCommandInfo in pluginInfo.SubCommands)
                {
                    var subPushButtonData = new PushButtonData(
                        name: $"cmd_sub_{subCommandInfo.Name.Replace(" ", "_")}_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                        text: subCommandInfo.DisplayName,
                        assemblyName: pluginAssemblyFullPath,
                        className: subCommandInfo.ClassName
                    );
                    if (!string.IsNullOrEmpty(subCommandInfo.Description)) subPushButtonData.ToolTip = subCommandInfo.Description;

                    string subLargeIconPath = string.IsNullOrEmpty(subCommandInfo.LargeIcon) ? null : Path.Combine(pluginAssemblyDir, subCommandInfo.LargeIcon);
                    string subSmallIconPath = string.IsNullOrEmpty(subCommandInfo.SmallIcon) ? null : Path.Combine(pluginAssemblyDir, subCommandInfo.SmallIcon);
                    subPushButtonData.LargeImage = LoadBitmapImage(subLargeIconPath);
                    subPushButtonData.Image = LoadBitmapImage(subSmallIconPath);

                    splitButton.AddPushButton(subPushButtonData);
                }
            }
            else
            {
                TaskDialog.Show("Plugin UI Error", $"Не удалось создать SplitButton '{pluginInfo.DisplayName}' на панели.");
            }
        }

        // Загружает BitmapImage.
        private BitmapImage LoadBitmapImage(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath)) return null;
            try
            {
                return new BitmapImage(new Uri(fullPath, UriKind.Absolute));
            }
            catch { return null; }
        }

        // Получает или создает RibbonPanel.
        private RibbonPanel GetOrCreateRibbonPanel(UIControlledApplication application, string tabName, string panelName)
        {
            RibbonPanel panel = null;
            try { panel = application.GetRibbonPanels(tabName).FirstOrDefault(p => p.Name.Equals(panelName, StringComparison.OrdinalIgnoreCase)); }
            catch { /* Вкладка может не существовать */ }

            if (panel != null) return panel;

            try { application.CreateRibbonTab(tabName); }
            catch (Autodesk.Revit.Exceptions.ArgumentException ex) when (ex.Message.ToLower().Contains("already exist")) { /* Игнорируем */ }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Ошибка при создании вкладки '{tabName}': {ex.Message}"); }

            return application.CreateRibbonPanel(tabName, panelName);
        }

        #endregion
    }
}
