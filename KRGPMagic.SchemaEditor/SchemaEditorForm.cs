using KRGPMagic.Core.Models;
using KRGPMagic.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace KRGPMagic.SchemaEditor
{
    /// <summary>
    /// Форма для визуального редактирования файла конфигурации KRGPMagic_Schema.xml.
    /// Позволяет управлять определениями PulldownButton, плагинами, их свойствами и взаимным расположением.
    /// </summary>
    public partial class SchemaEditorForm : Form
    {
        #region Fields

        private string _schemaFilePath;
        private PluginConfiguration _currentConfiguration;
        private BindingList<PulldownButtonDefinitionInfo> _pulldownDefinitionsBindingList;
        private BindingList<PluginInfo> _pluginsBindingList;

        private readonly Color _enabledColor = Color.LightGreen;
        private readonly Color _disabledColor = Color.LightCoral;
        private readonly Color _defaultRowColor = SystemColors.Window;

        private bool _isDirty = false; // Флаг наличия несохраненных изменений

        #endregion

        #region Constructor

        /// <summary>
        /// Инициализирует новый экземпляр формы SchemaEditorForm.
        /// </summary>
        /// <param name="schemaFilePath">Путь к файлу схемы конфигурации XML.</param>
        public SchemaEditorForm(string schemaFilePath)
        {
            _schemaFilePath = schemaFilePath ?? throw new ArgumentNullException(nameof(schemaFilePath));
            InitializeComponent();
            LoadConfigurationFromFile(_schemaFilePath);
            UpdateFormTitle();
            this.FormClosing += SchemaEditorForm_FormClosing; // Подписка на событие закрытия формы
        }

        #endregion

        #region Private Methods - Load & Save Configuration

        /// <summary>
        /// Обновляет заголовок формы, отображая имя текущего файла схемы.
        /// </summary>
        private void UpdateFormTitle()
        {
            this.Text = $"Редактор схемы KRGPMagic - [{Path.GetFileName(_schemaFilePath)}]";
        }

        /// <summary>
        /// Загружает конфигурацию плагинов из указанного XML-файла.
        /// </summary>
        /// <param name="filePath">Путь к XML-файлу конфигурации.</param>
        private void LoadConfigurationFromFile(string filePath)
        {
            try
            {
                var reader = new XmlConfigurationReader();
                _currentConfiguration = reader.ReadConfiguration(filePath);
                _schemaFilePath = filePath;
                UpdateFormTitle();
                _isDirty = false; // Сбрасываем флаг после успешной загрузки
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки файла схемы '{filePath}': {ex.Message}", "Ошибка Загрузки", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _currentConfiguration = new PluginConfiguration();
                _isDirty = false; // Даже если создана пустая конфигурация, считаем ее "чистой"
            }
            SetupDataBindings();
        }

        /// <summary>
        /// Сохраняет текущую конфигурацию плагинов в XML-файл.
        /// </summary>
        private void SaveConfiguration()
        {
            try
            {
                // Обновляем основные коллекции из BindingList перед сериализацией
                _currentConfiguration.PulldownButtonDefinitions = _pulldownDefinitionsBindingList.ToList();
                _currentConfiguration.Plugins = _pluginsBindingList.ToList();

                var serializer = new XmlSerializer(typeof(PluginConfiguration));
                using (var writer = new StreamWriter(_schemaFilePath, false, System.Text.Encoding.UTF8))
                {
                    var ns = new XmlSerializerNamespaces();
                    ns.Add("", "");
                    serializer.Serialize(writer, _currentConfiguration, ns);
                }
                MessageBox.Show("Схема успешно сохранена.", "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _isDirty = false; // Сбрасываем флаг после успешного сохранения

                RefreshDataGridViewsFormatting();
                UpdatePulldownGroupComboBox();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения файла схемы: {ex.Message}", "Ошибка Сохранения", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Private Methods - UI Setup & Data Binding

        /// <summary>
        /// Настраивает привязки данных для элементов управления формы (DataGridView, ComboBox).
        /// </summary>
        private void SetupDataBindings()
        {
            // Отписка от событий перед новой привязкой во избежание дублирования
            if (dgvPulldownDefinitions != null)
            {
                dgvPulldownDefinitions.SelectionChanged -= DgvPulldownDefinitions_SelectionChanged;
                dgvPulldownDefinitions.RowPrePaint -= DgvPulldownDefinitions_RowPrePaint;
            }
            if (dgvPlugins != null)
            {
                dgvPlugins.SelectionChanged -= DgvPlugins_SelectionChanged;
                dgvPlugins.RowPrePaint -= DgvPlugins_RowPrePaint;
            }
            if (dgvSubCommands != null) dgvSubCommands.SelectionChanged -= DgvSubCommands_SelectionChanged;

            // Привязка PulldownButtonDefinitions
            _pulldownDefinitionsBindingList = new BindingList<PulldownButtonDefinitionInfo>(_currentConfiguration.PulldownButtonDefinitions ?? new List<PulldownButtonDefinitionInfo>());
            dgvPulldownDefinitions.AutoGenerateColumns = false;
            dgvPulldownDefinitions.DataSource = _pulldownDefinitionsBindingList;
            dgvPulldownDefinitions.SelectionChanged += DgvPulldownDefinitions_SelectionChanged;
            dgvPulldownDefinitions.RowPrePaint += DgvPulldownDefinitions_RowPrePaint;

            // Привязка Plugins
            _pluginsBindingList = new BindingList<PluginInfo>(_currentConfiguration.Plugins ?? new List<PluginInfo>());
            dgvPlugins.AutoGenerateColumns = false;
            dgvPlugins.DataSource = _pluginsBindingList;
            dgvPlugins.SelectionChanged += DgvPlugins_SelectionChanged;
            dgvPlugins.RowPrePaint += DgvPlugins_RowPrePaint;

            // Привязка SubCommands (косвенно через выбор плагина)
            dgvSubCommands.AutoGenerateColumns = false;
            dgvSubCommands.SelectionChanged += DgvSubCommands_SelectionChanged;

            UpdatePulldownGroupComboBox();
            propertyGrid.SelectedObject = null;
            ClearSubCommandsDisplay();
            RefreshDataGridViewsFormatting();

            // Инициализация состояния кнопок Вкл/Выкл
            btnTogglePulldownEnable.Enabled = false;
            btnTogglePulldownEnable.Text = "Вкл/Выкл";
            btnTogglePluginEnable.Enabled = false;
            btnTogglePluginEnable.Text = "Вкл/Выкл";
        }

        /// <summary>
        /// Обрабатывает изменение выбора в таблице определений PulldownButton.
        /// Обновляет PropertyGrid и состояние кнопки Вкл/Выкл.
        /// </summary>
        private void DgvPulldownDefinitions_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvPulldownDefinitions.CurrentRow != null && dgvPulldownDefinitions.CurrentRow.DataBoundItem is PulldownButtonDefinitionInfo selectedPulldown)
            {
                propertyGrid.SelectedObject = selectedPulldown;
                btnTogglePulldownEnable.Enabled = true;
                btnTogglePulldownEnable.Text = selectedPulldown.Enabled ? "Выключить" : "Включить";
            }
            else
            {
                propertyGrid.SelectedObject = null;
                btnTogglePulldownEnable.Enabled = false;
                btnTogglePulldownEnable.Text = "Вкл/Выкл";
            }
        }

        /// <summary>
        /// Обрабатывает изменение выбора в таблице плагинов.
        /// Обновляет PropertyGrid, отображение подкоманд и состояние кнопок.
        /// </summary>
        private void DgvPlugins_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvPlugins.CurrentRow != null && dgvPlugins.CurrentRow.DataBoundItem is PluginInfo selectedPlugin)
            {
                propertyGrid.SelectedObject = selectedPlugin;
                DisplaySubCommands(selectedPlugin);
                UpdatePulldownGroupAssignmentControls(selectedPlugin);
                btnTogglePluginEnable.Enabled = true;
                btnTogglePluginEnable.Text = selectedPlugin.Enabled ? "Выключить" : "Включить";
            }
            else
            {
                propertyGrid.SelectedObject = null;
                ClearSubCommandsDisplay();
                UpdatePulldownGroupAssignmentControls(null);
                btnTogglePluginEnable.Enabled = false;
                btnTogglePluginEnable.Text = "Вкл/Выкл";
            }
        }

        /// <summary>
        /// Обрабатывает изменение выбора в таблице подкоманд.
        /// Обновляет PropertyGrid.
        /// </summary>
        private void DgvSubCommands_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvSubCommands.CurrentRow != null && dgvSubCommands.CurrentRow.DataBoundItem is SubCommandInfo selectedSubCommand)
            {
                propertyGrid.SelectedObject = selectedSubCommand;
            }
            // Если нет выбора или DataBoundItem не SubCommandInfo, PropertyGrid не меняется или очищается предыдущим выбором
        }

        /// <summary>
        /// Отображает подкоманды для выбранного плагина, если он является SplitButton.
        /// </summary>
        /// <param name="plugin">Выбранный плагин.</param>
        private void DisplaySubCommands(PluginInfo plugin)
        {
            if (plugin != null && plugin.UIType == PluginInfo.ButtonUIType.SplitButton)
            {
                if (plugin.SubCommands == null) plugin.SubCommands = new List<SubCommandInfo>();
                var subCommandsBindingList = new BindingList<SubCommandInfo>(plugin.SubCommands);
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

        /// <summary>
        /// Очищает отображение подкоманд и деактивирует связанные элементы управления.
        /// </summary>
        private void ClearSubCommandsDisplay()
        {
            dgvSubCommands.DataSource = null;
            dgvSubCommands.Enabled = false;
            btnAddSubCommand.Enabled = false;
            btnRemoveSubCommand.Enabled = false;
        }

        /// <summary>
        /// Обновляет список доступных групп PulldownButton в ComboBox.
        /// </summary>
        private void UpdatePulldownGroupComboBox()
        {
            string previouslySelected = cmbPulldownGroups.SelectedItem as string;
            cmbPulldownGroups.Items.Clear();
            cmbPulldownGroups.Items.Add("");
            if (_pulldownDefinitionsBindingList != null)
            {
                foreach (var pbd in _pulldownDefinitionsBindingList.Where(p => p.Enabled && !string.IsNullOrEmpty(p.Name)))
                {
                    cmbPulldownGroups.Items.Add(pbd.Name);
                }
            }

            bool selectionRestored = false;
            if (previouslySelected != null && cmbPulldownGroups.Items.Contains(previouslySelected))
            {
                cmbPulldownGroups.SelectedItem = previouslySelected;
                selectionRestored = true;
            }

            if (!selectionRestored && dgvPlugins.CurrentRow?.DataBoundItem is PluginInfo currentPlugin)
            {
                string groupToSelect = currentPlugin.PulldownGroupName ?? "";
                if (cmbPulldownGroups.Items.Contains(groupToSelect))
                {
                    cmbPulldownGroups.SelectedItem = groupToSelect;
                    selectionRestored = true;
                }
            }

            if (!selectionRestored)
            {
                if (cmbPulldownGroups.Items.Contains("")) cmbPulldownGroups.SelectedItem = "";
                else if (cmbPulldownGroups.Items.Count > 0) cmbPulldownGroups.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Обновляет состояние элементов управления для назначения плагина группе PulldownButton.
        /// </summary>
        /// <param name="selectedPlugin">Выбранный плагин.</param>
        private void UpdatePulldownGroupAssignmentControls(PluginInfo selectedPlugin)
        {
            if (selectedPlugin != null)
            {
                cmbPulldownGroups.Enabled = true;
                btnAssignPulldownGroup.Enabled = true;
                cmbPulldownGroups.SelectedItem = selectedPlugin.PulldownGroupName ?? "";
            }
            else
            {
                cmbPulldownGroups.Enabled = false;
                btnAssignPulldownGroup.Enabled = false;
                cmbPulldownGroups.SelectedItem = null;
            }
        }

        /// <summary>
        /// Принудительно обновляет форматирование (включая подсветку) строк в DataGridViews.
        /// </summary>
        private void RefreshDataGridViewsFormatting()
        {
            dgvPulldownDefinitions.Refresh();
            dgvPlugins.Refresh();
        }

        #endregion

        #region Event Handlers

        #region Form Event Handlers
        /// <summary>
        /// Обрабатывает событие закрытия формы, проверяя наличие несохраненных изменений.
        /// </summary>
        private void SchemaEditorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isDirty)
            {
                var result = MessageBox.Show("Есть несохраненные изменения. Сохранить их перед закрытием?",
                                             "Несохраненные изменения",
                                             MessageBoxButtons.YesNoCancel,
                                             MessageBoxIcon.Warning);
                if (result == DialogResult.Yes)
                {
                    SaveConfiguration();
                    // Если SaveConfiguration вызвала ошибку, _isDirty останется true,
                    // но пользователь уже видел сообщение об ошибке.
                    // Если сохранение успешно, _isDirty станет false.
                    if (_isDirty) // Проверяем, если сохранение не удалось (например, из-за ошибки доступа)
                    {
                        e.Cancel = true; // Предотвращаем закрытие, если сохранение не удалось и изменения остались
                    }
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
                // Если DialogResult.No, форма закроется без сохранения.
            }
        }
        #endregion

        #region PropertyGrid Event Handlers
        /// <summary>
        /// Обрабатывает изменение значения свойства в PropertyGrid.
        /// Устанавливает флаг несохраненных изменений и обновляет UI при необходимости.
        /// </summary>
        private void propertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            _isDirty = true;
            if (e.ChangedItem.PropertyDescriptor.Name == "Enabled")
            {
                if (propertyGrid.SelectedObject is PulldownButtonDefinitionInfo pbd)
                {
                    btnTogglePulldownEnable.Text = pbd.Enabled ? "Выключить" : "Включить";
                    RefreshDataGridViewsFormatting();
                    UpdatePulldownGroupComboBox(); // Активность Pulldown влияет на список групп
                }
                else if (propertyGrid.SelectedObject is PluginInfo plugin)
                {
                    btnTogglePluginEnable.Text = plugin.Enabled ? "Выключить" : "Включить";
                    RefreshDataGridViewsFormatting();
                }
            }
            else if (e.ChangedItem.PropertyDescriptor.Name == "Name" && propertyGrid.SelectedObject is PulldownButtonDefinitionInfo)
            {
                // Имя PulldownButtonDefinition изменилось, нужно обновить ComboBox
                // Это будет сделано при сохранении через UpdatePulldownGroupComboBox в SaveConfiguration
                // или немедленно, если пользователь этого ожидает:
                // UpdatePulldownGroupComboBox(); // Раскомментировать для немедленного обновления
            }
        }
        #endregion

        #region Button Click Handlers

        /// <summary>
        /// Обрабатывает нажатие кнопки "Открыть файл...".
        /// </summary>
        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            if (_isDirty)
            {
                var result = MessageBox.Show("Есть несохраненные изменения. Сохранить их перед открытием нового файла?",
                                             "Несохраненные изменения",
                                             MessageBoxButtons.YesNoCancel,
                                             MessageBoxIcon.Warning);
                if (result == DialogResult.Yes) SaveConfiguration();
                else if (result == DialogResult.Cancel) return;
            }

            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = Path.GetDirectoryName(_schemaFilePath);
                openFileDialog.FileName = Path.GetFileName(_schemaFilePath);
                openFileDialog.Filter = "KRGPMagic Schema Files (*.xml)|*.xml|All files (*.*)|*.*";
                openFileDialog.Title = "Выберите файл схемы KRGPMagic";
                openFileDialog.CheckFileExists = true;

                if (openFileDialog.ShowDialog(this) == DialogResult.OK)
                {
                    LoadConfigurationFromFile(openFileDialog.FileName);
                }
            }
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Сохранить".
        /// </summary>
        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveConfiguration();
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Закрыть".
        /// </summary>
        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Добавить" для определений PulldownButton.
        /// </summary>
        private void btnAddPulldown_Click(object sender, EventArgs e)
        {
            var newPulldown = new PulldownButtonDefinitionInfo { Name = "NewPulldown", DisplayName = "Новый Pulldown", RibbonTab = "KRGPMagic", RibbonPanel = "Панель", Enabled = true };
            _pulldownDefinitionsBindingList.Add(newPulldown);
            _isDirty = true;
            if (dgvPulldownDefinitions.Rows.Count > 0)
            {
                dgvPulldownDefinitions.ClearSelection();
                dgvPulldownDefinitions.Rows[dgvPulldownDefinitions.Rows.Count - 1].Selected = true;
            }
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Удалить" для определений PulldownButton.
        /// </summary>
        private void btnRemovePulldown_Click(object sender, EventArgs e)
        {
            if (dgvPulldownDefinitions.CurrentRow != null && dgvPulldownDefinitions.CurrentRow.DataBoundItem is PulldownButtonDefinitionInfo selected)
            {
                if (MessageBox.Show($"Удалить определение Pulldown '{selected.DisplayName}'?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _pulldownDefinitionsBindingList.Remove(selected);
                    _isDirty = true;
                }
            }
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Включить/Выключить" для определений PulldownButton.
        /// </summary>
        private void btnTogglePulldownEnable_Click(object sender, EventArgs e)
        {
            if (dgvPulldownDefinitions.CurrentRow != null && dgvPulldownDefinitions.CurrentRow.DataBoundItem is PulldownButtonDefinitionInfo selectedPulldown)
            {
                selectedPulldown.Enabled = !selectedPulldown.Enabled;
                _isDirty = true;
                btnTogglePulldownEnable.Text = selectedPulldown.Enabled ? "Выключить" : "Включить";
                _pulldownDefinitionsBindingList.ResetItem(_pulldownDefinitionsBindingList.IndexOf(selectedPulldown));
                RefreshDataGridViewsFormatting();
                UpdatePulldownGroupComboBox();
                propertyGrid.Refresh();
            }
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Добавить" для плагинов.
        /// </summary>
        private void btnAddPlugin_Click(object sender, EventArgs e)
        {
            using (var typeDialog = new Form())
            {
                typeDialog.Text = "Выберите тип плагина";
                typeDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                typeDialog.StartPosition = FormStartPosition.CenterParent;
                typeDialog.ClientSize = new System.Drawing.Size(200, 100);

                var rbPush = new RadioButton { Text = "PushButton", Location = new System.Drawing.Point(10, 10), Checked = true, AutoSize = true };
                var rbSplit = new RadioButton { Text = "SplitButton", Location = new System.Drawing.Point(10, 35), AutoSize = true };
                var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new System.Drawing.Point(110, 65) };

                typeDialog.Controls.AddRange(new Control[] { rbPush, rbSplit, btnOk });
                typeDialog.AcceptButton = btnOk;

                if (typeDialog.ShowDialog(this) == DialogResult.OK)
                {
                    var newPlugin = new PluginInfo
                    {
                        Name = "NewPlugin",
                        DisplayName = "Новый Плагин",
                        AssemblyPath = "Plugins\\NewPlugin\\NewPlugin.dll",
                        ClassName = "Namespace.NewPluginCommand",
                        RibbonTab = "KRGPMagic",
                        RibbonPanel = "Панель",
                        UIType = rbSplit.Checked ? PluginInfo.ButtonUIType.SplitButton : PluginInfo.ButtonUIType.PushButton,
                        Enabled = true
                    };
                    if (newPlugin.UIType == PluginInfo.ButtonUIType.SplitButton)
                    {
                        newPlugin.SubCommands = new List<SubCommandInfo>();
                    }
                    _pluginsBindingList.Add(newPlugin);
                    _isDirty = true;
                    if (dgvPlugins.Rows.Count > 0)
                    {
                        dgvPlugins.ClearSelection();
                        dgvPlugins.Rows[dgvPlugins.Rows.Count - 1].Selected = true;
                    }
                }
            }
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Удалить" для плагинов.
        /// </summary>
        private void btnRemovePlugin_Click(object sender, EventArgs e)
        {
            if (dgvPlugins.CurrentRow != null && dgvPlugins.CurrentRow.DataBoundItem is PluginInfo selected)
            {
                if (MessageBox.Show($"Удалить плагин '{selected.DisplayName}'?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _pluginsBindingList.Remove(selected);
                    _isDirty = true;
                }
            }
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Включить/Выключить" для плагинов.
        /// </summary>
        private void btnTogglePluginEnable_Click(object sender, EventArgs e)
        {
            if (dgvPlugins.CurrentRow != null && dgvPlugins.CurrentRow.DataBoundItem is PluginInfo selectedPlugin)
            {
                selectedPlugin.Enabled = !selectedPlugin.Enabled;
                _isDirty = true;
                btnTogglePluginEnable.Text = selectedPlugin.Enabled ? "Выключить" : "Включить";
                _pluginsBindingList.ResetItem(_pluginsBindingList.IndexOf(selectedPlugin));
                RefreshDataGridViewsFormatting();
                propertyGrid.Refresh();
            }
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Назначить" для привязки плагина к группе PulldownButton.
        /// </summary>
        private void btnAssignPulldownGroup_Click(object sender, EventArgs e)
        {
            if (dgvPlugins.CurrentRow != null && dgvPlugins.CurrentRow.DataBoundItem is PluginInfo selectedPlugin)
            {
                string selectedGroup = cmbPulldownGroups.SelectedItem as string ?? "";
                if (selectedPlugin.PulldownGroupName != selectedGroup)
                {
                    selectedPlugin.PulldownGroupName = selectedGroup;
                    _isDirty = true;
                    _pluginsBindingList.ResetItem(_pluginsBindingList.IndexOf(selectedPlugin));
                    propertyGrid.Refresh();
                }
            }
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Добавить" для подкоманд (SplitButton).
        /// </summary>
        private void btnAddSubCommand_Click(object sender, EventArgs e)
        {
            if (dgvPlugins.CurrentRow != null && dgvPlugins.CurrentRow.DataBoundItem is PluginInfo selectedPlugin)
            {
                if (selectedPlugin.UIType == PluginInfo.ButtonUIType.SplitButton)
                {
                    var newSubCommand = new SubCommandInfo { Name = "NewSubCmd", DisplayName = "Новая Подкоманда", ClassName = "Namespace.NewSubCommand" };
                    if (selectedPlugin.SubCommands == null) selectedPlugin.SubCommands = new List<SubCommandInfo>();

                    selectedPlugin.SubCommands.Add(newSubCommand);
                    _isDirty = true;
                    DisplaySubCommands(selectedPlugin);

                    if (dgvSubCommands.Rows.Count > 0)
                    {
                        dgvSubCommands.ClearSelection();
                        dgvSubCommands.Rows[dgvSubCommands.Rows.Count - 1].Selected = true;
                    }
                }
            }
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Удалить" для подкоманд (SplitButton).
        /// </summary>
        private void btnRemoveSubCommand_Click(object sender, EventArgs e)
        {
            if (dgvPlugins.CurrentRow != null && dgvPlugins.CurrentRow.DataBoundItem is PluginInfo selectedPlugin &&
                dgvSubCommands.CurrentRow != null && dgvSubCommands.CurrentRow.DataBoundItem is SubCommandInfo selectedSubCommand)
            {
                if (MessageBox.Show($"Удалить подкоманду '{selectedSubCommand.DisplayName}' из плагина '{selectedPlugin.DisplayName}'?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (selectedPlugin.SubCommands != null)
                    {
                        selectedPlugin.SubCommands.Remove(selectedSubCommand);
                        _isDirty = true;
                        DisplaySubCommands(selectedPlugin);
                    }
                }
            }
        }

        #endregion

        #region DataGridView Event Handlers

        /// <summary>
        /// Осуществляет кастомную отрисовку строк в таблице определений PulldownButton для подсветки.
        /// </summary>
        private void DgvPulldownDefinitions_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvPulldownDefinitions.Rows.Count) return;
            var row = dgvPulldownDefinitions.Rows[e.RowIndex];
            if (row.DataBoundItem is PulldownButtonDefinitionInfo pbd)
            {
                row.DefaultCellStyle.BackColor = pbd.Enabled ? _enabledColor : _disabledColor;
                row.DefaultCellStyle.SelectionBackColor = pbd.Enabled ? Color.DarkSeaGreen : Color.IndianRed; // Цвет выделения
            }
            else
            {
                row.DefaultCellStyle.BackColor = _defaultRowColor;
                row.DefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
            }
        }

        /// <summary>
        /// Осуществляет кастомную отрисовку строк в таблице плагинов для подсветки.
        /// </summary>
        private void DgvPlugins_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvPlugins.Rows.Count) return;
            var row = dgvPlugins.Rows[e.RowIndex];
            if (row.DataBoundItem is PluginInfo plugin)
            {
                row.DefaultCellStyle.BackColor = plugin.Enabled ? _enabledColor : _disabledColor;
                row.DefaultCellStyle.SelectionBackColor = plugin.Enabled ? Color.DarkSeaGreen : Color.IndianRed;
            }
            else
            {
                row.DefaultCellStyle.BackColor = _defaultRowColor;
                row.DefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
            }
        }

        #endregion

        #region Designer Code (SchemaEditorForm.Designer.cs) - Partial

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
            this.colPulldownEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.panelPulldownButtons = new System.Windows.Forms.Panel();
            this.btnTogglePulldownEnable = new System.Windows.Forms.Button(); // Новая кнопка
            this.btnRemovePulldown = new System.Windows.Forms.Button();
            this.btnAddPulldown = new System.Windows.Forms.Button();
            this.tabPagePlugins = new System.Windows.Forms.TabPage();
            this.splitContainerPlugins = new System.Windows.Forms.SplitContainer();
            this.dgvPlugins = new System.Windows.Forms.DataGridView();
            this.colPluginName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPluginDisplayName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPluginUIType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPluginPulldownGroup = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panelPluginActions = new System.Windows.Forms.Panel();
            this.btnTogglePluginEnable = new System.Windows.Forms.Button(); // Новая кнопка
            this.btnAssignPulldownGroup = new System.Windows.Forms.Button();
            this.cmbPulldownGroups = new System.Windows.Forms.ComboBox();
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
            this.btnOpenFile = new System.Windows.Forms.Button();
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
            this.panelPluginActions.SuspendLayout();
            this.groupBoxSubCommands.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSubCommands)).BeginInit();
            this.panelSubCommandButtons.SuspendLayout();
            this.panelBottom.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControlMain
            // 
            this.tabControlMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControlMain.Controls.Add(this.tabPagePulldowns);
            this.tabControlMain.Controls.Add(this.tabPagePlugins);
            this.tabControlMain.Location = new System.Drawing.Point(12, 12);
            this.tabControlMain.Name = "tabControlMain";
            this.tabControlMain.SelectedIndex = 0;
            this.tabControlMain.Size = new System.Drawing.Size(700, 550);
            this.tabControlMain.TabIndex = 0;
            // 
            // tabPagePulldowns
            // 
            this.tabPagePulldowns.Controls.Add(this.dgvPulldownDefinitions);
            this.tabPagePulldowns.Controls.Add(this.panelPulldownButtons);
            this.tabPagePulldowns.Location = new System.Drawing.Point(4, 22);
            this.tabPagePulldowns.Name = "tabPagePulldowns";
            this.tabPagePulldowns.Padding = new System.Windows.Forms.Padding(3);
            this.tabPagePulldowns.Size = new System.Drawing.Size(692, 524);
            this.tabPagePulldowns.TabIndex = 0;
            this.tabPagePulldowns.Text = "Pulldown Buttons";
            this.tabPagePulldowns.UseVisualStyleBackColor = true;
            // 
            // dgvPulldownDefinitions
            // 
            this.dgvPulldownDefinitions.AllowUserToAddRows = false;
            this.dgvPulldownDefinitions.AllowUserToDeleteRows = false;
            this.dgvPulldownDefinitions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvPulldownDefinitions.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colPulldownName,
            this.colPulldownDisplayName,
            this.colPulldownTab,
            this.colPulldownPanel,
            this.colPulldownEnabled});
            this.dgvPulldownDefinitions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvPulldownDefinitions.Location = new System.Drawing.Point(3, 33);
            this.dgvPulldownDefinitions.MultiSelect = false;
            this.dgvPulldownDefinitions.Name = "dgvPulldownDefinitions";
            this.dgvPulldownDefinitions.ReadOnly = true;
            this.dgvPulldownDefinitions.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPulldownDefinitions.Size = new System.Drawing.Size(686, 488);
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
            this.colPulldownDisplayName.Width = 150;
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
            // colPulldownEnabled
            // 
            this.colPulldownEnabled.DataPropertyName = "Enabled";
            this.colPulldownEnabled.HeaderText = "Активен";
            this.colPulldownEnabled.Name = "colPulldownEnabled";
            this.colPulldownEnabled.ReadOnly = true;
            this.colPulldownEnabled.Width = 60;
            // 
            // panelPulldownButtons
            // 
            this.panelPulldownButtons.Controls.Add(this.btnTogglePulldownEnable);
            this.panelPulldownButtons.Controls.Add(this.btnRemovePulldown);
            this.panelPulldownButtons.Controls.Add(this.btnAddPulldown);
            this.panelPulldownButtons.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelPulldownButtons.Location = new System.Drawing.Point(3, 3);
            this.panelPulldownButtons.Name = "panelPulldownButtons";
            this.panelPulldownButtons.Size = new System.Drawing.Size(686, 30);
            this.panelPulldownButtons.TabIndex = 0;
            // 
            // btnTogglePulldownEnable
            // 
            this.btnTogglePulldownEnable.Location = new System.Drawing.Point(165, 3);
            this.btnTogglePulldownEnable.Name = "btnTogglePulldownEnable";
            this.btnTogglePulldownEnable.Size = new System.Drawing.Size(120, 23);
            this.btnTogglePulldownEnable.TabIndex = 2;
            this.btnTogglePulldownEnable.Text = "Вкл/Выкл";
            this.btnTogglePulldownEnable.UseVisualStyleBackColor = true;
            this.btnTogglePulldownEnable.Click += new System.EventHandler(this.btnTogglePulldownEnable_Click);
            this.btnTogglePulldownEnable.Enabled = false;
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
            this.tabPagePlugins.Size = new System.Drawing.Size(692, 524);
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
            this.splitContainerPlugins.Panel1.Controls.Add(this.panelPluginActions);
            // 
            // splitContainerPlugins.Panel2
            // 
            this.splitContainerPlugins.Panel2.Controls.Add(this.groupBoxSubCommands);
            this.splitContainerPlugins.Size = new System.Drawing.Size(686, 518);
            this.splitContainerPlugins.SplitterDistance = 250;
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
            this.dgvPlugins.Size = new System.Drawing.Size(686, 220);
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
            this.colPluginDisplayName.Width = 150;
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
            this.colPluginPulldownGroup.Width = 120;
            // 
            // panelPluginActions
            // 
            this.panelPluginActions.Controls.Add(this.btnTogglePluginEnable);
            this.panelPluginActions.Controls.Add(this.btnAssignPulldownGroup);
            this.panelPluginActions.Controls.Add(this.cmbPulldownGroups);
            this.panelPluginActions.Controls.Add(this.btnRemovePlugin);
            this.panelPluginActions.Controls.Add(this.btnAddPlugin);
            this.panelPluginActions.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelPluginActions.Location = new System.Drawing.Point(0, 0);
            this.panelPluginActions.Name = "panelPluginActions";
            this.panelPluginActions.Size = new System.Drawing.Size(686, 30);
            this.panelPluginActions.TabIndex = 0;
            // 
            // btnTogglePluginEnable
            // 
            this.btnTogglePluginEnable.Location = new System.Drawing.Point(165, 3); // Левее cmbPulldownGroups
            this.btnTogglePluginEnable.Name = "btnTogglePluginEnable";
            this.btnTogglePluginEnable.Size = new System.Drawing.Size(120, 23);
            this.btnTogglePluginEnable.TabIndex = 4; // Обновленный TabIndex
            this.btnTogglePluginEnable.Text = "Вкл/Выкл";
            this.btnTogglePluginEnable.UseVisualStyleBackColor = true;
            this.btnTogglePluginEnable.Click += new System.EventHandler(this.btnTogglePluginEnable_Click);
            this.btnTogglePluginEnable.Enabled = false;
            // 
            // btnAssignPulldownGroup
            // 
            this.btnAssignPulldownGroup.Enabled = false;
            this.btnAssignPulldownGroup.Location = new System.Drawing.Point(456, 3); // Сдвинуто правее
            this.btnAssignPulldownGroup.Name = "btnAssignPulldownGroup";
            this.btnAssignPulldownGroup.Size = new System.Drawing.Size(85, 23);
            this.btnAssignPulldownGroup.TabIndex = 3;
            this.btnAssignPulldownGroup.Text = "Назначить";
            this.btnAssignPulldownGroup.UseVisualStyleBackColor = true;
            this.btnAssignPulldownGroup.Click += new System.EventHandler(this.btnAssignPulldownGroup_Click);
            // 
            // cmbPulldownGroups
            // 
            this.cmbPulldownGroups.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPulldownGroups.Enabled = false;
            this.cmbPulldownGroups.FormattingEnabled = true;
            this.cmbPulldownGroups.Location = new System.Drawing.Point(291, 4); // Сдвинуто правее
            this.cmbPulldownGroups.Name = "cmbPulldownGroups";
            this.cmbPulldownGroups.Size = new System.Drawing.Size(159, 21);
            this.cmbPulldownGroups.TabIndex = 2;
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
            this.groupBoxSubCommands.Size = new System.Drawing.Size(686, 264);
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
            this.dgvSubCommands.Size = new System.Drawing.Size(680, 215);
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
            this.panelSubCommandButtons.Size = new System.Drawing.Size(680, 30);
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
            this.propertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.propertyGrid.Location = new System.Drawing.Point(718, 34);
            this.propertyGrid.Name = "propertyGrid";
            this.propertyGrid.Size = new System.Drawing.Size(354, 524);
            this.propertyGrid.TabIndex = 1;
            this.propertyGrid.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.propertyGrid_PropertyValueChanged);
            // 
            // panelBottom
            // 
            this.panelBottom.Controls.Add(this.btnOpenFile);
            this.panelBottom.Controls.Add(this.btnClose);
            this.panelBottom.Controls.Add(this.btnSave);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Location = new System.Drawing.Point(0, 568);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Size = new System.Drawing.Size(1084, 43);
            this.panelBottom.TabIndex = 2;
            // 
            // btnOpenFile
            // 
            this.btnOpenFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnOpenFile.Location = new System.Drawing.Point(12, 10);
            this.btnOpenFile.Name = "btnOpenFile";
            this.btnOpenFile.Size = new System.Drawing.Size(120, 23);
            this.btnOpenFile.TabIndex = 2;
            this.btnOpenFile.Text = "Открыть файл...";
            this.btnOpenFile.UseVisualStyleBackColor = true;
            this.btnOpenFile.Click += new System.EventHandler(this.btnOpenFile_Click);
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(997, 10);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "Закрыть";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.Location = new System.Drawing.Point(916, 10);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
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
            this.ClientSize = new System.Drawing.Size(1084, 611);
            this.Controls.Add(this.panelBottom);
            this.Controls.Add(this.propertyGrid);
            this.Controls.Add(this.tabControlMain);
            this.MinimumSize = new System.Drawing.Size(900, 500);
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
            this.panelPluginActions.ResumeLayout(false);
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
        private System.Windows.Forms.Panel panelPluginActions;
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
        private System.Windows.Forms.DataGridViewCheckBoxColumn colPulldownEnabled;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPluginName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPluginDisplayName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPluginUIType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPluginPulldownGroup;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSubCmdName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSubCmdDisplayName;
        private System.Windows.Forms.Button btnOpenFile;
        private System.Windows.Forms.ComboBox cmbPulldownGroups;
        private System.Windows.Forms.Button btnAssignPulldownGroup;
        private System.Windows.Forms.Button btnTogglePulldownEnable; // Объявление
        private System.Windows.Forms.Button btnTogglePluginEnable;   // Объявление

        #endregion
        #endregion

    }
}
