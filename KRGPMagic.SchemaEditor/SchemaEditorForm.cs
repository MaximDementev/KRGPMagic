// Требуется добавить ссылки на:
// - System.Windows.Forms
// - System.Drawing
// - KRGPMagic.Core.dll (для доступа к моделям PluginConfiguration и др.)
// - System.Xml (для XmlSerializer, хотя он используется в XmlConfigurationReader/Writer)

using KRGPMagic.Core.Models;
using KRGPMagic.Services; // Для XmlConfigurationReader (и потенциального XmlConfigurationWriter)
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;


namespace KRGPMagic.SchemaEditor
{
    // Форма для визуального редактирования файла KRGPMagic_Schema.xml.
    public partial class SchemaEditorForm : Form
    {
        #region Fields
        private readonly string _schemaFilePath;
        private PluginConfiguration _currentConfiguration;
        private BindingList<PulldownButtonDefinitionInfo> _pulldownDefinitionsBindingList;
        private BindingList<PluginInfo> _pluginsBindingList;
        #endregion

        #region Constructor
        // Инициализирует форму и загружает данные из указанного файла схемы.
        public SchemaEditorForm(string schemaFilePath)
        {
            _schemaFilePath = schemaFilePath ?? throw new ArgumentNullException(nameof(schemaFilePath));
            InitializeComponent();
            LoadConfiguration();
            SetupDataBindings();
            this.Text = $"Редактор схемы KRGPMagic - {_schemaFilePath}";
        }
        #endregion

        #region Private Methods - Load & Save Configuration

        // Загружает конфигурацию из XML-файла.
        private void LoadConfiguration()
        {
            try
            {
                var reader = new XmlConfigurationReader(); // Используем существующий reader
                _currentConfiguration = reader.ReadConfiguration(_schemaFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки файла схемы: {ex.Message}", "Ошибка Загрузки", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _currentConfiguration = new PluginConfiguration(); // Создаем пустую конфигурацию в случае ошибки
            }
        }

        // Сохраняет текущую конфигурацию в XML-файл.
        private void SaveConfiguration()
        {
            try
            {
                // Обновляем _currentConfiguration из BindingList перед сохранением
                _currentConfiguration.PulldownButtonDefinitions = _pulldownDefinitionsBindingList.ToList();
                _currentConfiguration.Plugins = _pluginsBindingList.ToList();

                var serializer = new XmlSerializer(typeof(PluginConfiguration));
                using (var writer = new StreamWriter(_schemaFilePath, false, System.Text.Encoding.UTF8)) // UTF-8 и без BOM
                {
                    var ns = new XmlSerializerNamespaces();
                    ns.Add("", ""); // Убираем стандартные пространства имен xsi и xsd
                    serializer.Serialize(writer, _currentConfiguration, ns);
                }
                MessageBox.Show("Схема успешно сохранена.", "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения файла схемы: {ex.Message}", "Ошибка Сохранения", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region Private Methods - UI Setup & Data Binding

        // Настраивает привязки данных для элементов управления.
        private void SetupDataBindings()
        {
            // PulldownButtonDefinitions
            _pulldownDefinitionsBindingList = new BindingList<PulldownButtonDefinitionInfo>(_currentConfiguration.PulldownButtonDefinitions ?? new List<PulldownButtonDefinitionInfo>());
            dgvPulldownDefinitions.AutoGenerateColumns = false; // Настраиваем колонки вручную
            dgvPulldownDefinitions.DataSource = _pulldownDefinitionsBindingList;
            // Привязка PropertyGrid к выбранному элементу в dgvPulldownDefinitions
            dgvPulldownDefinitions.SelectionChanged += (s, e) =>
            {
                if (dgvPulldownDefinitions.CurrentRow != null && dgvPulldownDefinitions.CurrentRow.DataBoundItem is PulldownButtonDefinitionInfo selectedPulldown)
                {
                    propertyGrid.SelectedObject = selectedPulldown;
                }
                else
                {
                    propertyGrid.SelectedObject = null;
                }
            };


            // Plugins
            _pluginsBindingList = new BindingList<PluginInfo>(_currentConfiguration.Plugins ?? new List<PluginInfo>());
            dgvPlugins.AutoGenerateColumns = false; // Настраиваем колонки вручную
            dgvPlugins.DataSource = _pluginsBindingList;
            // Привязка PropertyGrid к выбранному элементу в dgvPlugins
            dgvPlugins.SelectionChanged += (s, e) =>
            {
                if (dgvPlugins.CurrentRow != null && dgvPlugins.CurrentRow.DataBoundItem is PluginInfo selectedPlugin)
                {
                    propertyGrid.SelectedObject = selectedPlugin;
                    // Дополнительно можно отобразить SubCommands для выбранного плагина, если он SplitButton
                    DisplaySubCommands(selectedPlugin);
                }
                else
                {
                    propertyGrid.SelectedObject = null;
                    ClearSubCommandsDisplay();
                }
            };

            // SubCommands (для выбранного плагина)
            dgvSubCommands.AutoGenerateColumns = false;
            dgvSubCommands.SelectionChanged += (s, e) =>
            {
                if (dgvSubCommands.CurrentRow != null && dgvSubCommands.CurrentRow.DataBoundItem is SubCommandInfo selectedSubCommand)
                {
                    propertyGrid.SelectedObject = selectedSubCommand;
                }
                // Не сбрасываем propertyGrid, если ничего не выбрано в SubCommands,
                // чтобы оставить выбранным родительский PluginInfo
            };
        }

        // Отображает подкоманды для выбранного плагина.
        private void DisplaySubCommands(PluginInfo plugin)
        {
            if (plugin != null && plugin.UIType == PluginInfo.ButtonUIType.SplitButton)
            {
                var subCommandsBindingList = new BindingList<SubCommandInfo>(plugin.SubCommands ?? new List<SubCommandInfo>());
                // Важно: если plugin.SubCommands это List, то изменения в subCommandsBindingList (добавление/удаление)
                // не отразятся в plugin.SubCommands автоматически, если не переприсвоить.
                // PropertyGrid будет редактировать объекты в subCommandsBindingList, которые являются ссылками на объекты в plugin.SubCommands.
                dgvSubCommands.DataSource = subCommandsBindingList;
                dgvSubCommands.Enabled = true;
                btnAddSubCommand.Enabled = true;
                btnRemoveSubCommand.Enabled = true;
            }
            else
            {
                ClearSubCommandsDisplay();
            }
        }

        // Очищает отображение подкоманд.
        private void ClearSubCommandsDisplay()
        {
            dgvSubCommands.DataSource = null;
            dgvSubCommands.Enabled = false;
            btnAddSubCommand.Enabled = false;
            btnRemoveSubCommand.Enabled = false;
        }

        #endregion

        #region Event Handlers

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveConfiguration();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close(); // DialogResult будет Cancel по умолчанию, если не OK
        }

        // --- Обработчики для PulldownDefinitions ---
        private void btnAddPulldown_Click(object sender, EventArgs e)
        {
            var newPulldown = new PulldownButtonDefinitionInfo { Name = "NewPulldown", DisplayName = "Новый Pulldown", RibbonTab = "KRGPMagic", RibbonPanel = "Панель" };
            _pulldownDefinitionsBindingList.Add(newPulldown);
            dgvPulldownDefinitions.ClearSelection();
            dgvPulldownDefinitions.Rows[dgvPulldownDefinitions.Rows.Count - 1].Selected = true;
        }

        private void btnRemovePulldown_Click(object sender, EventArgs e)
        {
            if (dgvPulldownDefinitions.CurrentRow != null && dgvPulldownDefinitions.CurrentRow.DataBoundItem is PulldownButtonDefinitionInfo selected)
            {
                if (MessageBox.Show($"Удалить определение Pulldown '{selected.DisplayName}'?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _pulldownDefinitionsBindingList.Remove(selected);
                }
            }
        }

        // --- Обработчики для Plugins ---
        private void btnAddPlugin_Click(object sender, EventArgs e)
        {
            var newPlugin = new PluginInfo { Name = "NewPlugin", DisplayName = "Новый Плагин", AssemblyPath = "Plugins\\NewPlugin\\NewPlugin.dll", ClassName = "Namespace.NewPluginCommand", RibbonTab = "KRGPMagic", RibbonPanel = "Панель" };
            _pluginsBindingList.Add(newPlugin);
            dgvPlugins.ClearSelection();
            dgvPlugins.Rows[dgvPlugins.Rows.Count - 1].Selected = true;
        }

        private void btnRemovePlugin_Click(object sender, EventArgs e)
        {
            if (dgvPlugins.CurrentRow != null && dgvPlugins.CurrentRow.DataBoundItem is PluginInfo selected)
            {
                if (MessageBox.Show($"Удалить плагин '{selected.DisplayName}'?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _pluginsBindingList.Remove(selected);
                }
            }
        }

        // --- Обработчики для SubCommands ---
        private void btnAddSubCommand_Click(object sender, EventArgs e)
        {
            if (dgvPlugins.CurrentRow != null && dgvPlugins.CurrentRow.DataBoundItem is PluginInfo selectedPlugin)
            {
                if (selectedPlugin.UIType == PluginInfo.ButtonUIType.SplitButton)
                {
                    var newSubCommand = new SubCommandInfo { Name = "NewSubCmd", DisplayName = "Новая Подкоманда", ClassName = "Namespace.NewSubCommand" };
                    // Убедимся, что список SubCommands инициализирован
                    if (selectedPlugin.SubCommands == null) selectedPlugin.SubCommands = new List<SubCommandInfo>();

                    // Если dgvSubCommands.DataSource привязан к BindingList<SubCommandInfo>, который является оберткой над selectedPlugin.SubCommands
                    var subCommandsBindingList = dgvSubCommands.DataSource as BindingList<SubCommandInfo>;
                    if (subCommandsBindingList != null)
                    {
                        subCommandsBindingList.Add(newSubCommand);
                        // Если subCommandsBindingList НЕ является прямым отображением selectedPlugin.SubCommands, нужно добавить и туда:
                        if (!ReferenceEquals(subCommandsBindingList.ToList(), selectedPlugin.SubCommands))
                        {
                            selectedPlugin.SubCommands.Add(newSubCommand); // Это может быть избыточно, если BindingList создан из selectedPlugin.SubCommands
                        }
                    }
                    else // Если DataSource был null или не BindingList, создаем новый
                    {
                        selectedPlugin.SubCommands.Add(newSubCommand);
                        DisplaySubCommands(selectedPlugin); // Перепривязываем
                    }

                    if (dgvSubCommands.Rows.Count > 0)
                    {
                        dgvSubCommands.ClearSelection();
                        dgvSubCommands.Rows[dgvSubCommands.Rows.Count - 1].Selected = true;
                    }
                }
            }
        }

        private void btnRemoveSubCommand_Click(object sender, EventArgs e)
        {
            if (dgvPlugins.CurrentRow != null && dgvPlugins.CurrentRow.DataBoundItem is PluginInfo selectedPlugin &&
                dgvSubCommands.CurrentRow != null && dgvSubCommands.CurrentRow.DataBoundItem is SubCommandInfo selectedSubCommand)
            {
                if (MessageBox.Show($"Удалить подкоманду '{selectedSubCommand.DisplayName}' из плагина '{selectedPlugin.DisplayName}'?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    var subCommandsBindingList = dgvSubCommands.DataSource as BindingList<SubCommandInfo>;
                    if (subCommandsBindingList != null)
                    {
                        subCommandsBindingList.Remove(selectedSubCommand);
                        // Также удаляем из оригинальной коллекции в PluginInfo, если BindingList был оберткой
                        if (selectedPlugin.SubCommands != null && selectedPlugin.SubCommands.Contains(selectedSubCommand))
                        {
                            selectedPlugin.SubCommands.Remove(selectedSubCommand);
                        }
                    }
                }
            }
        }

        #endregion

        #region Designer Code (SchemaEditorForm.Designer.cs) - Partial
        // Этот код обычно генерируется дизайнером. Привожу основную структуру.
        // Вам нужно будет создать форму в дизайнере Visual Studio и разместить эти контролы.

        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.tabControlMain = new System.Windows.Forms.TabControl();
            this.tabPagePulldowns = new System.Windows.Forms.TabPage();
            this.dgvPulldownDefinitions = new System.Windows.Forms.DataGridView();
            this.colPulldownName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPulldownDisplayName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPulldownTab = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPulldownPanel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panelPulldownButtons = new System.Windows.Forms.Panel();
            this.btnRemovePulldown = new System.Windows.Forms.Button();
            this.btnAddPulldown = new System.Windows.Forms.Button();
            this.tabPagePlugins = new System.Windows.Forms.TabPage();
            this.splitContainerPlugins = new System.Windows.Forms.SplitContainer();
            this.dgvPlugins = new System.Windows.Forms.DataGridView();
            this.colPluginName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPluginDisplayName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPluginUIType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPluginPulldownGroup = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panelPluginButtons = new System.Windows.Forms.Panel();
            this.btnRemovePlugin = new System.Windows.Forms.Button();
            this.btnAddPlugin = new System.Windows.Forms.Button();
            this.groupBoxSubCommands = new System.Windows.Forms.GroupBox();
            this.dgvSubCommands = new System.Windows.Forms.DataGridView();
            this.colSubCmdName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSubCmdDisplayName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panelSubCommandButtons = new System.Windows.Forms.Panel();
            this.btnRemoveSubCommand = new System.Windows.Forms.Button();
            this.btnAddSubCommand = new System.Windows.Forms.Button();
            this.propertyGrid = new System.Windows.Forms.PropertyGrid();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.tabControlMain.SuspendLayout();
            this.tabPagePulldowns.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPulldownDefinitions)).BeginInit();
            this.panelPulldownButtons.SuspendLayout();
            this.tabPagePlugins.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerPlugins)).BeginInit();
            this.splitContainerPlugins.Panel1.SuspendLayout();
            this.splitContainerPlugins.Panel2.SuspendLayout();
            this.splitContainerPlugins.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPlugins)).BeginInit();
            this.panelPluginButtons.SuspendLayout();
            this.groupBoxSubCommands.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSubCommands)).BeginInit();
            this.panelSubCommandButtons.SuspendLayout();
            this.panelBottom.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControlMain
            // 
            this.tabControlMain.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.tabControlMain.Controls.Add(this.tabPagePulldowns);
            this.tabControlMain.Controls.Add(this.tabPagePlugins);
            this.tabControlMain.Location = new System.Drawing.Point(3, 2);
            this.tabControlMain.MinimumSize = new System.Drawing.Size(726, 611);
            this.tabControlMain.Name = "tabControlMain";
            this.tabControlMain.SelectedIndex = 0;
            this.tabControlMain.Size = new System.Drawing.Size(726, 611);
            this.tabControlMain.TabIndex = 0;
            // 
            // tabPagePulldowns
            // 
            this.tabPagePulldowns.Controls.Add(this.dgvPulldownDefinitions);
            this.tabPagePulldowns.Controls.Add(this.panelPulldownButtons);
            this.tabPagePulldowns.Location = new System.Drawing.Point(4, 22);
            this.tabPagePulldowns.Name = "tabPagePulldowns";
            this.tabPagePulldowns.Padding = new System.Windows.Forms.Padding(3);
            this.tabPagePulldowns.Size = new System.Drawing.Size(718, 585);
            this.tabPagePulldowns.TabIndex = 0;
            this.tabPagePulldowns.Text = "Pulldown Buttons";
            this.tabPagePulldowns.UseVisualStyleBackColor = true;
            // 
            // dgvPulldownDefinitions
            // 
            this.dgvPulldownDefinitions.AllowUserToAddRows = false;
            this.dgvPulldownDefinitions.AllowUserToDeleteRows = false;
            this.dgvPulldownDefinitions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvPulldownDefinitions.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvPulldownDefinitions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvPulldownDefinitions.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colPulldownName,
            this.colPulldownDisplayName,
            this.colPulldownTab,
            this.colPulldownPanel});
            this.dgvPulldownDefinitions.Location = new System.Drawing.Point(3, 33);
            this.dgvPulldownDefinitions.MultiSelect = false;
            this.dgvPulldownDefinitions.Name = "dgvPulldownDefinitions";
            this.dgvPulldownDefinitions.ReadOnly = true;
            this.dgvPulldownDefinitions.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPulldownDefinitions.Size = new System.Drawing.Size(709, 549);
            this.dgvPulldownDefinitions.TabIndex = 1;
            // 
            // colPulldownName
            // 
            this.colPulldownName.DataPropertyName = "Name";
            this.colPulldownName.HeaderText = "ID";
            this.colPulldownName.Name = "colPulldownName";
            this.colPulldownName.ReadOnly = true;
            // 
            // colPulldownDisplayName
            // 
            this.colPulldownDisplayName.DataPropertyName = "DisplayName";
            this.colPulldownDisplayName.HeaderText = "Отображаемое имя";
            this.colPulldownDisplayName.Name = "colPulldownDisplayName";
            this.colPulldownDisplayName.ReadOnly = true;
            // 
            // colPulldownTab
            // 
            this.colPulldownTab.DataPropertyName = "RibbonTab";
            this.colPulldownTab.HeaderText = "Вкладка";
            this.colPulldownTab.Name = "colPulldownTab";
            this.colPulldownTab.ReadOnly = true;
            // 
            // colPulldownPanel
            // 
            this.colPulldownPanel.DataPropertyName = "RibbonPanel";
            this.colPulldownPanel.HeaderText = "Панель";
            this.colPulldownPanel.Name = "colPulldownPanel";
            this.colPulldownPanel.ReadOnly = true;
            // 
            // panelPulldownButtons
            // 
            this.panelPulldownButtons.Controls.Add(this.btnRemovePulldown);
            this.panelPulldownButtons.Controls.Add(this.btnAddPulldown);
            this.panelPulldownButtons.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelPulldownButtons.Location = new System.Drawing.Point(3, 3);
            this.panelPulldownButtons.Name = "panelPulldownButtons";
            this.panelPulldownButtons.Size = new System.Drawing.Size(712, 30);
            this.panelPulldownButtons.TabIndex = 0;
            // 
            // btnRemovePulldown
            // 
            this.btnRemovePulldown.Location = new System.Drawing.Point(84, 3);
            this.btnRemovePulldown.Name = "btnRemovePulldown";
            this.btnRemovePulldown.Size = new System.Drawing.Size(75, 23);
            this.btnRemovePulldown.TabIndex = 1;
            this.btnRemovePulldown.Text = "Удалить";
            this.btnRemovePulldown.UseVisualStyleBackColor = true;
            this.btnRemovePulldown.Click += new System.EventHandler(this.btnRemovePulldown_Click);
            // 
            // btnAddPulldown
            // 
            this.btnAddPulldown.Location = new System.Drawing.Point(3, 3);
            this.btnAddPulldown.Name = "btnAddPulldown";
            this.btnAddPulldown.Size = new System.Drawing.Size(75, 23);
            this.btnAddPulldown.TabIndex = 0;
            this.btnAddPulldown.Text = "Добавить";
            this.btnAddPulldown.UseVisualStyleBackColor = true;
            this.btnAddPulldown.Click += new System.EventHandler(this.btnAddPulldown_Click);
            // 
            // tabPagePlugins
            // 
            this.tabPagePlugins.Controls.Add(this.splitContainerPlugins);
            this.tabPagePlugins.Location = new System.Drawing.Point(4, 22);
            this.tabPagePlugins.Name = "tabPagePlugins";
            this.tabPagePlugins.Padding = new System.Windows.Forms.Padding(3);
            this.tabPagePlugins.Size = new System.Drawing.Size(718, 585);
            this.tabPagePlugins.TabIndex = 1;
            this.tabPagePlugins.Text = "Плагины";
            this.tabPagePlugins.UseVisualStyleBackColor = true;
            // 
            // splitContainerPlugins
            // 
            this.splitContainerPlugins.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerPlugins.Location = new System.Drawing.Point(3, 3);
            this.splitContainerPlugins.Name = "splitContainerPlugins";
            this.splitContainerPlugins.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerPlugins.Panel1
            // 
            this.splitContainerPlugins.Panel1.Controls.Add(this.dgvPlugins);
            this.splitContainerPlugins.Panel1.Controls.Add(this.panelPluginButtons);
            // 
            // splitContainerPlugins.Panel2
            // 
            this.splitContainerPlugins.Panel2.Controls.Add(this.groupBoxSubCommands);
            this.splitContainerPlugins.Size = new System.Drawing.Size(712, 579);
            this.splitContainerPlugins.SplitterDistance = 281;
            this.splitContainerPlugins.TabIndex = 0;
            // 
            // dgvPlugins
            // 
            this.dgvPlugins.AllowUserToAddRows = false;
            this.dgvPlugins.AllowUserToDeleteRows = false;
            this.dgvPlugins.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvPlugins.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colPluginName,
            this.colPluginDisplayName,
            this.colPluginUIType,
            this.colPluginPulldownGroup});
            this.dgvPlugins.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvPlugins.Location = new System.Drawing.Point(0, 30);
            this.dgvPlugins.MultiSelect = false;
            this.dgvPlugins.Name = "dgvPlugins";
            this.dgvPlugins.ReadOnly = true;
            this.dgvPlugins.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPlugins.Size = new System.Drawing.Size(712, 251);
            this.dgvPlugins.TabIndex = 1;
            // 
            // colPluginName
            // 
            this.colPluginName.DataPropertyName = "Name";
            this.colPluginName.HeaderText = "ID";
            this.colPluginName.Name = "colPluginName";
            this.colPluginName.ReadOnly = true;
            // 
            // colPluginDisplayName
            // 
            this.colPluginDisplayName.DataPropertyName = "DisplayName";
            this.colPluginDisplayName.HeaderText = "Отображаемое имя";
            this.colPluginDisplayName.Name = "colPluginDisplayName";
            this.colPluginDisplayName.ReadOnly = true;
            this.colPluginDisplayName.Width = 200;
            // 
            // colPluginUIType
            // 
            this.colPluginUIType.DataPropertyName = "UIType";
            this.colPluginUIType.HeaderText = "Тип UI";
            this.colPluginUIType.Name = "colPluginUIType";
            this.colPluginUIType.ReadOnly = true;
            // 
            // colPluginPulldownGroup
            // 
            this.colPluginPulldownGroup.DataPropertyName = "PulldownGroupName";
            this.colPluginPulldownGroup.HeaderText = "Группа Pulldown";
            this.colPluginPulldownGroup.Name = "colPluginPulldownGroup";
            this.colPluginPulldownGroup.ReadOnly = true;
            this.colPluginPulldownGroup.Width = 150;
            // 
            // panelPluginButtons
            // 
            this.panelPluginButtons.Controls.Add(this.btnRemovePlugin);
            this.panelPluginButtons.Controls.Add(this.btnAddPlugin);
            this.panelPluginButtons.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelPluginButtons.Location = new System.Drawing.Point(0, 0);
            this.panelPluginButtons.Name = "panelPluginButtons";
            this.panelPluginButtons.Size = new System.Drawing.Size(712, 30);
            this.panelPluginButtons.TabIndex = 0;
            // 
            // btnRemovePlugin
            // 
            this.btnRemovePlugin.Location = new System.Drawing.Point(84, 3);
            this.btnRemovePlugin.Name = "btnRemovePlugin";
            this.btnRemovePlugin.Size = new System.Drawing.Size(75, 23);
            this.btnRemovePlugin.TabIndex = 1;
            this.btnRemovePlugin.Text = "Удалить";
            this.btnRemovePlugin.UseVisualStyleBackColor = true;
            this.btnRemovePlugin.Click += new System.EventHandler(this.btnRemovePlugin_Click);
            // 
            // btnAddPlugin
            // 
            this.btnAddPlugin.Location = new System.Drawing.Point(3, 3);
            this.btnAddPlugin.Name = "btnAddPlugin";
            this.btnAddPlugin.Size = new System.Drawing.Size(75, 23);
            this.btnAddPlugin.TabIndex = 0;
            this.btnAddPlugin.Text = "Добавить";
            this.btnAddPlugin.UseVisualStyleBackColor = true;
            this.btnAddPlugin.Click += new System.EventHandler(this.btnAddPlugin_Click);
            // 
            // groupBoxSubCommands
            // 
            this.groupBoxSubCommands.Controls.Add(this.dgvSubCommands);
            this.groupBoxSubCommands.Controls.Add(this.panelSubCommandButtons);
            this.groupBoxSubCommands.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxSubCommands.Location = new System.Drawing.Point(0, 0);
            this.groupBoxSubCommands.Name = "groupBoxSubCommands";
            this.groupBoxSubCommands.Size = new System.Drawing.Size(712, 294);
            this.groupBoxSubCommands.TabIndex = 0;
            this.groupBoxSubCommands.TabStop = false;
            this.groupBoxSubCommands.Text = "Подкоманды (для SplitButton)";
            // 
            // dgvSubCommands
            // 
            this.dgvSubCommands.AllowUserToAddRows = false;
            this.dgvSubCommands.AllowUserToDeleteRows = false;
            this.dgvSubCommands.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSubCommands.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colSubCmdName,
            this.colSubCmdDisplayName});
            this.dgvSubCommands.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvSubCommands.Enabled = false;
            this.dgvSubCommands.Location = new System.Drawing.Point(3, 46);
            this.dgvSubCommands.MultiSelect = false;
            this.dgvSubCommands.Name = "dgvSubCommands";
            this.dgvSubCommands.ReadOnly = true;
            this.dgvSubCommands.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvSubCommands.Size = new System.Drawing.Size(706, 245);
            this.dgvSubCommands.TabIndex = 1;
            // 
            // colSubCmdName
            // 
            this.colSubCmdName.DataPropertyName = "Name";
            this.colSubCmdName.HeaderText = "ID";
            this.colSubCmdName.Name = "colSubCmdName";
            this.colSubCmdName.ReadOnly = true;
            // 
            // colSubCmdDisplayName
            // 
            this.colSubCmdDisplayName.DataPropertyName = "DisplayName";
            this.colSubCmdDisplayName.HeaderText = "Отображаемое имя";
            this.colSubCmdDisplayName.Name = "colSubCmdDisplayName";
            this.colSubCmdDisplayName.ReadOnly = true;
            this.colSubCmdDisplayName.Width = 200;
            // 
            // panelSubCommandButtons
            // 
            this.panelSubCommandButtons.Controls.Add(this.btnRemoveSubCommand);
            this.panelSubCommandButtons.Controls.Add(this.btnAddSubCommand);
            this.panelSubCommandButtons.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelSubCommandButtons.Location = new System.Drawing.Point(3, 16);
            this.panelSubCommandButtons.Name = "panelSubCommandButtons";
            this.panelSubCommandButtons.Size = new System.Drawing.Size(706, 30);
            this.panelSubCommandButtons.TabIndex = 0;
            // 
            // btnRemoveSubCommand
            // 
            this.btnRemoveSubCommand.Enabled = false;
            this.btnRemoveSubCommand.Location = new System.Drawing.Point(84, 3);
            this.btnRemoveSubCommand.Name = "btnRemoveSubCommand";
            this.btnRemoveSubCommand.Size = new System.Drawing.Size(75, 23);
            this.btnRemoveSubCommand.TabIndex = 1;
            this.btnRemoveSubCommand.Text = "Удалить";
            this.btnRemoveSubCommand.UseVisualStyleBackColor = true;
            this.btnRemoveSubCommand.Click += new System.EventHandler(this.btnRemoveSubCommand_Click);
            // 
            // btnAddSubCommand
            // 
            this.btnAddSubCommand.Enabled = false;
            this.btnAddSubCommand.Location = new System.Drawing.Point(3, 3);
            this.btnAddSubCommand.Name = "btnAddSubCommand";
            this.btnAddSubCommand.Size = new System.Drawing.Size(75, 23);
            this.btnAddSubCommand.TabIndex = 0;
            this.btnAddSubCommand.Text = "Добавить";
            this.btnAddSubCommand.UseVisualStyleBackColor = true;
            this.btnAddSubCommand.Click += new System.EventHandler(this.btnAddSubCommand_Click);
            // 
            // propertyGrid
            // 
            this.propertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.propertyGrid.Location = new System.Drawing.Point(735, 24);
            this.propertyGrid.Name = "propertyGrid";
            this.propertyGrid.Size = new System.Drawing.Size(567, 589);
            this.propertyGrid.TabIndex = 1;
            // 
            // panelBottom
            // 
            this.panelBottom.Controls.Add(this.btnClose);
            this.panelBottom.Controls.Add(this.btnSave);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Location = new System.Drawing.Point(0, 621);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Size = new System.Drawing.Size(1314, 40);
            this.panelBottom.TabIndex = 2;
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(1071, 8);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(206, 23);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "Закрыть";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.Location = new System.Drawing.Point(878, 8);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(187, 23);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "Сохранить";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // SchemaEditorForm
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(1314, 661);
            this.Controls.Add(this.panelBottom);
            this.Controls.Add(this.tabControlMain);
            this.Controls.Add(this.propertyGrid);
            this.MinimumSize = new System.Drawing.Size(1330, 700);
            this.Name = "SchemaEditorForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Редактор схемы KRGPMagic";
            this.tabControlMain.ResumeLayout(false);
            this.tabPagePulldowns.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvPulldownDefinitions)).EndInit();
            this.panelPulldownButtons.ResumeLayout(false);
            this.tabPagePlugins.ResumeLayout(false);
            this.splitContainerPlugins.Panel1.ResumeLayout(false);
            this.splitContainerPlugins.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerPlugins)).EndInit();
            this.splitContainerPlugins.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvPlugins)).EndInit();
            this.panelPluginButtons.ResumeLayout(false);
            this.groupBoxSubCommands.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvSubCommands)).EndInit();
            this.panelSubCommandButtons.ResumeLayout(false);
            this.panelBottom.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.TabControl tabControlMain;
        private System.Windows.Forms.TabPage tabPagePulldowns;
        private System.Windows.Forms.DataGridView dgvPulldownDefinitions;
        private System.Windows.Forms.Panel panelPulldownButtons;
        private System.Windows.Forms.Button btnRemovePulldown;
        private System.Windows.Forms.Button btnAddPulldown;
        private System.Windows.Forms.TabPage tabPagePlugins;
        private System.Windows.Forms.PropertyGrid propertyGrid;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.SplitContainer splitContainerPlugins;
        private System.Windows.Forms.DataGridView dgvPlugins;
        private System.Windows.Forms.Panel panelPluginButtons;
        private System.Windows.Forms.Button btnRemovePlugin;
        private System.Windows.Forms.Button btnAddPlugin;
        private System.Windows.Forms.GroupBox groupBoxSubCommands;
        private System.Windows.Forms.DataGridView dgvSubCommands;
        private System.Windows.Forms.Panel panelSubCommandButtons;
        private System.Windows.Forms.Button btnRemoveSubCommand;
        private System.Windows.Forms.Button btnAddSubCommand;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPulldownName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPulldownDisplayName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPulldownTab;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPulldownPanel;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPluginName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPluginDisplayName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPluginUIType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPluginPulldownGroup;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSubCmdName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSubCmdDisplayName;

        #endregion
    }
}
