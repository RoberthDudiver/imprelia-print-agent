namespace Imprelia.PrintAgent;

partial class SettingsForm
{
    private System.ComponentModel.IContainer components = null;

    private PictureBox logoPictureBox;
    private Label appNameLabel;
    private Label subtitleLabel;
    private TabControl mainTabs;
    private TabPage dashboardTab;
    private TabPage printersTab;
    private TabPage routesTab;
    private TabPage settingsTab;
    private TabPage logsTab;
    private Label dashboardTitleLabel;
    private Label statusLabel;
    private Label urlLabel;
    private Button refreshDashboardButton;
    private Button dashboardTestButton;
    private Label eventsTitleLabel;
    private ListBox eventsList;
    private Label printersTitleLabel;
    private ListView printersList;
    private ColumnHeader printerNameColumn;
    private ColumnHeader printerDefaultColumn;
    private ColumnHeader printerStatusColumn;
    private ColumnHeader printerTypeColumn;
    private ColumnHeader printerPdfColumn;
    private ColumnHeader printerRawColumn;
    private ColumnHeader printerZplColumn;
    private Label defaultPrinterLabel;
    private ComboBox defaultPrinterCombo;
    private ComboBox testTypeCombo;
    private Button refreshPrintersButton;
    private Button useSelectedPrinterButton;
    private Button printerTestButton;
    private Label routesTitleLabel;
    private DataGridView routesGrid;
    private DataGridViewTextBoxColumn purposeColumn;
    private DataGridViewTextBoxColumn routePrinterColumn;
    private DataGridViewTextBoxColumn routeContentTypeColumn;
    private Button saveRoutesButton;
    private Label routesHintLabel;
    private Label settingsTitleLabel;
    private Label portLabel;
    private NumericUpDown portInput;
    private CheckBox corsCheck;
    private Label originsLabel;
    private TextBox originsText;
    private CheckBox autoStartCheck;
    private Button saveSettingsButton;
    private Button restoreDefaultsButton;
    private Label originsHintLabel;
    private Label portHintLabel;
    private Label logsTitleLabel;
    private Label logsHintLabel;
    private Button clearEventsButton;
    private Button minimizeButton;
    private Button exitButton;
    private Button apiGuideButton;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            logoPictureBox?.Image?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        logoPictureBox = new PictureBox();
        appNameLabel = new Label();
        subtitleLabel = new Label();
        mainTabs = new TabControl();
        dashboardTab = new TabPage();
        dashboardTitleLabel = new Label();
        statusLabel = new Label();
        urlLabel = new Label();
        refreshDashboardButton = new Button();
        dashboardTestButton = new Button();
        eventsTitleLabel = new Label();
        eventsList = new ListBox();
        printersTab = new TabPage();
        printersTitleLabel = new Label();
        printersList = new ListView();
        printerNameColumn = new ColumnHeader();
        printerDefaultColumn = new ColumnHeader();
        printerStatusColumn = new ColumnHeader();
        printerTypeColumn = new ColumnHeader();
        printerPdfColumn = new ColumnHeader();
        printerRawColumn = new ColumnHeader();
        printerZplColumn = new ColumnHeader();
        defaultPrinterLabel = new Label();
        defaultPrinterCombo = new ComboBox();
        testTypeCombo = new ComboBox();
        refreshPrintersButton = new Button();
        useSelectedPrinterButton = new Button();
        printerTestButton = new Button();
        routesTab = new TabPage();
        routesTitleLabel = new Label();
        routesGrid = new DataGridView();
        purposeColumn = new DataGridViewTextBoxColumn();
        routePrinterColumn = new DataGridViewTextBoxColumn();
        routeContentTypeColumn = new DataGridViewTextBoxColumn();
        saveRoutesButton = new Button();
        routesHintLabel = new Label();
        settingsTab = new TabPage();
        settingsTitleLabel = new Label();
        portLabel = new Label();
        portInput = new NumericUpDown();
        corsCheck = new CheckBox();
        originsLabel = new Label();
        originsText = new TextBox();
        autoStartCheck = new CheckBox();
        saveSettingsButton = new Button();
        restoreDefaultsButton = new Button();
        originsHintLabel = new Label();
        portHintLabel = new Label();
        logsTab = new TabPage();
        logsTitleLabel = new Label();
        logsHintLabel = new Label();
        clearEventsButton = new Button();
        minimizeButton = new Button();
        exitButton = new Button();
        apiGuideButton = new Button();
        ((System.ComponentModel.ISupportInitialize)logoPictureBox).BeginInit();
        mainTabs.SuspendLayout();
        dashboardTab.SuspendLayout();
        printersTab.SuspendLayout();
        routesTab.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)routesGrid).BeginInit();
        settingsTab.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)portInput).BeginInit();
        logsTab.SuspendLayout();
        SuspendLayout();
        // 
        // logoPictureBox
        // 
        logoPictureBox.BackColor = Color.White;
        logoPictureBox.Location = new Point(18, 14);
        logoPictureBox.Name = "logoPictureBox";
        logoPictureBox.Size = new Size(76, 76);
        logoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        logoPictureBox.TabIndex = 0;
        logoPictureBox.TabStop = false;
        // 
        // appNameLabel
        // 
        appNameLabel.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
        appNameLabel.ForeColor = Color.FromArgb(31, 49, 61);
        appNameLabel.Location = new Point(104, 22);
        appNameLabel.Name = "appNameLabel";
        appNameLabel.Size = new Size(380, 44);
        appNameLabel.TabIndex = 1;
        appNameLabel.Text = "Imprelia Print Agent";
        // 
        // subtitleLabel
        // 
        subtitleLabel.ForeColor = Color.DimGray;
        subtitleLabel.Location = new Point(100, 66);
        subtitleLabel.Name = "subtitleLabel";
        subtitleLabel.Size = new Size(480, 24);
        subtitleLabel.TabIndex = 2;
        subtitleLabel.Text = "Agente local de impresion para aplicaciones web";
        // 
        // mainTabs
        // 
        mainTabs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        mainTabs.Controls.Add(dashboardTab);
        mainTabs.Controls.Add(printersTab);
        mainTabs.Controls.Add(routesTab);
        mainTabs.Controls.Add(settingsTab);
        mainTabs.Controls.Add(logsTab);
        mainTabs.Location = new Point(14, 104);
        mainTabs.Name = "mainTabs";
        mainTabs.SelectedIndex = 0;
        mainTabs.Size = new Size(821, 625);
        mainTabs.TabIndex = 3;
        // 
        // dashboardTab
        // 
        dashboardTab.BackColor = Color.White;
        dashboardTab.Controls.Add(dashboardTitleLabel);
        dashboardTab.Controls.Add(statusLabel);
        dashboardTab.Controls.Add(urlLabel);
        dashboardTab.Controls.Add(refreshDashboardButton);
        dashboardTab.Controls.Add(dashboardTestButton);
        dashboardTab.Controls.Add(eventsTitleLabel);
        dashboardTab.Controls.Add(eventsList);
        dashboardTab.Location = new Point(4, 34);
        dashboardTab.Name = "dashboardTab";
        dashboardTab.Padding = new Padding(3);
        dashboardTab.Size = new Size(813, 587);
        dashboardTab.TabIndex = 0;
        dashboardTab.Text = "Dashboard";
        // 
        // dashboardTitleLabel
        // 
        dashboardTitleLabel.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
        dashboardTitleLabel.Location = new Point(18, 18);
        dashboardTitleLabel.Name = "dashboardTitleLabel";
        dashboardTitleLabel.Size = new Size(720, 34);
        dashboardTitleLabel.TabIndex = 0;
        dashboardTitleLabel.Text = "Estado del agente";
        // 
        // statusLabel
        // 
        statusLabel.ForeColor = Color.FromArgb(16, 122, 87);
        statusLabel.Location = new Point(22, 58);
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(730, 28);
        statusLabel.TabIndex = 1;
        // 
        // urlLabel
        // 
        urlLabel.ForeColor = Color.DimGray;
        urlLabel.Location = new Point(22, 90);
        urlLabel.Name = "urlLabel";
        urlLabel.Size = new Size(730, 24);
        urlLabel.TabIndex = 2;
        // 
        // refreshDashboardButton
        // 
        refreshDashboardButton.Location = new Point(22, 132);
        refreshDashboardButton.Name = "refreshDashboardButton";
        refreshDashboardButton.Size = new Size(160, 34);
        refreshDashboardButton.TabIndex = 3;
        refreshDashboardButton.Text = "Refrescar estado";
        refreshDashboardButton.UseVisualStyleBackColor = true;
        refreshDashboardButton.Click += refreshDashboardButton_Click;
        // 
        // dashboardTestButton
        // 
        dashboardTestButton.Location = new Point(196, 132);
        dashboardTestButton.Name = "dashboardTestButton";
        dashboardTestButton.Size = new Size(160, 34);
        dashboardTestButton.TabIndex = 4;
        dashboardTestButton.Text = "Imprimir prueba";
        dashboardTestButton.UseVisualStyleBackColor = true;
        dashboardTestButton.Click += dashboardTestButton_Click;
        // 
        // eventsTitleLabel
        // 
        eventsTitleLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        eventsTitleLabel.Location = new Point(22, 188);
        eventsTitleLabel.Name = "eventsTitleLabel";
        eventsTitleLabel.Size = new Size(300, 24);
        eventsTitleLabel.TabIndex = 5;
        eventsTitleLabel.Text = "Ultimos eventos";
        // 
        // eventsList
        // 
        eventsList.FormattingEnabled = true;
        eventsList.ItemHeight = 25;
        eventsList.Location = new Point(22, 218);
        eventsList.Name = "eventsList";
        eventsList.Size = new Size(730, 154);
        eventsList.TabIndex = 6;
        // 
        // printersTab
        // 
        printersTab.BackColor = Color.White;
        printersTab.Controls.Add(printersTitleLabel);
        printersTab.Controls.Add(printersList);
        printersTab.Controls.Add(defaultPrinterLabel);
        printersTab.Controls.Add(defaultPrinterCombo);
        printersTab.Controls.Add(testTypeCombo);
        printersTab.Controls.Add(refreshPrintersButton);
        printersTab.Controls.Add(useSelectedPrinterButton);
        printersTab.Controls.Add(printerTestButton);
        printersTab.Location = new Point(4, 34);
        printersTab.Name = "printersTab";
        printersTab.Padding = new Padding(3);
        printersTab.Size = new Size(813, 587);
        printersTab.TabIndex = 1;
        printersTab.Text = "Impresoras";
        // 
        // printersTitleLabel
        // 
        printersTitleLabel.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
        printersTitleLabel.Location = new Point(18, 18);
        printersTitleLabel.Name = "printersTitleLabel";
        printersTitleLabel.Size = new Size(720, 34);
        printersTitleLabel.TabIndex = 0;
        printersTitleLabel.Text = "Impresoras instaladas";
        // 
        // printersList
        // 
        printersList.Columns.AddRange(new ColumnHeader[] { printerNameColumn, printerDefaultColumn, printerStatusColumn, printerTypeColumn, printerPdfColumn, printerRawColumn, printerZplColumn });
        printersList.FullRowSelect = true;
        printersList.GridLines = true;
        printersList.Location = new Point(18, 58);
        printersList.Name = "printersList";
        printersList.Size = new Size(742, 320);
        printersList.TabIndex = 1;
        printersList.UseCompatibleStateImageBehavior = false;
        printersList.View = View.Details;
        // 
        // printerNameColumn
        // 
        printerNameColumn.Text = "Nombre";
        printerNameColumn.Width = 260;
        // 
        // printerDefaultColumn
        // 
        printerDefaultColumn.Text = "Default";
        printerDefaultColumn.Width = 70;
        // 
        // printerStatusColumn
        // 
        printerStatusColumn.Text = "Estado";
        printerStatusColumn.Width = 90;
        // 
        // printerTypeColumn
        // 
        printerTypeColumn.Text = "Tipo";
        printerTypeColumn.Width = 140;
        // 
        // printerPdfColumn
        // 
        printerPdfColumn.Text = "PDF";
        printerPdfColumn.Width = 50;
        // 
        // printerRawColumn
        // 
        printerRawColumn.Text = "RAW";
        printerRawColumn.Width = 50;
        // 
        // printerZplColumn
        // 
        printerZplColumn.Text = "ZPL";
        printerZplColumn.Width = 50;
        // 
        // defaultPrinterLabel
        // 
        defaultPrinterLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        defaultPrinterLabel.Location = new Point(18, 400);
        defaultPrinterLabel.Name = "defaultPrinterLabel";
        defaultPrinterLabel.Size = new Size(220, 24);
        defaultPrinterLabel.TabIndex = 2;
        defaultPrinterLabel.Text = "Predeterminada del agente";
        // 
        // defaultPrinterCombo
        // 
        defaultPrinterCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        defaultPrinterCombo.FormattingEnabled = true;
        defaultPrinterCombo.Location = new Point(18, 430);
        defaultPrinterCombo.Name = "defaultPrinterCombo";
        defaultPrinterCombo.Size = new Size(410, 33);
        defaultPrinterCombo.TabIndex = 3;
        defaultPrinterCombo.SelectedIndexChanged += defaultPrinterCombo_SelectedIndexChanged;
        // 
        // testTypeCombo
        // 
        testTypeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        testTypeCombo.FormattingEnabled = true;
        testTypeCombo.Items.AddRange(new object[] { "epos", "text", "zpl", "pdf", "fiscal" });
        testTypeCombo.Location = new Point(446, 430);
        testTypeCombo.Name = "testTypeCombo";
        testTypeCombo.Size = new Size(150, 33);
        testTypeCombo.TabIndex = 4;
        // 
        // refreshPrintersButton
        // 
        refreshPrintersButton.Location = new Point(18, 474);
        refreshPrintersButton.Name = "refreshPrintersButton";
        refreshPrintersButton.Size = new Size(120, 34);
        refreshPrintersButton.TabIndex = 5;
        refreshPrintersButton.Text = "Actualizar";
        refreshPrintersButton.UseVisualStyleBackColor = true;
        refreshPrintersButton.Click += refreshPrintersButton_Click;
        // 
        // useSelectedPrinterButton
        // 
        useSelectedPrinterButton.Location = new Point(150, 474);
        useSelectedPrinterButton.Name = "useSelectedPrinterButton";
        useSelectedPrinterButton.Size = new Size(160, 34);
        useSelectedPrinterButton.TabIndex = 6;
        useSelectedPrinterButton.Text = "Usar seleccionada";
        useSelectedPrinterButton.UseVisualStyleBackColor = true;
        useSelectedPrinterButton.Click += useSelectedPrinterButton_Click;
        // 
        // printerTestButton
        // 
        printerTestButton.Location = new Point(322, 474);
        printerTestButton.Name = "printerTestButton";
        printerTestButton.Size = new Size(150, 34);
        printerTestButton.TabIndex = 7;
        printerTestButton.Text = "Imprimir prueba";
        printerTestButton.UseVisualStyleBackColor = true;
        printerTestButton.Click += printerTestButton_Click;
        // 
        // routesTab
        // 
        routesTab.BackColor = Color.White;
        routesTab.Controls.Add(routesTitleLabel);
        routesTab.Controls.Add(routesGrid);
        routesTab.Controls.Add(saveRoutesButton);
        routesTab.Controls.Add(routesHintLabel);
        routesTab.Location = new Point(4, 34);
        routesTab.Name = "routesTab";
        routesTab.Padding = new Padding(3);
        routesTab.Size = new Size(813, 587);
        routesTab.TabIndex = 2;
        routesTab.Text = "Rutas";
        // 
        // routesTitleLabel
        // 
        routesTitleLabel.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
        routesTitleLabel.Location = new Point(18, 18);
        routesTitleLabel.Name = "routesTitleLabel";
        routesTitleLabel.Size = new Size(720, 34);
        routesTitleLabel.TabIndex = 0;
        routesTitleLabel.Text = "Rutas por proposito";
        // 
        // routesGrid
        // 
        routesGrid.AllowUserToDeleteRows = false;
        routesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        routesGrid.BackgroundColor = Color.White;
        routesGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        routesGrid.Columns.AddRange(new DataGridViewColumn[] { purposeColumn, routePrinterColumn, routeContentTypeColumn });
        routesGrid.Location = new Point(18, 58);
        routesGrid.Name = "routesGrid";
        routesGrid.RowHeadersWidth = 62;
        routesGrid.Size = new Size(742, 360);
        routesGrid.TabIndex = 1;
        // 
        // purposeColumn
        // 
        purposeColumn.HeaderText = "Proposito";
        purposeColumn.MinimumWidth = 8;
        purposeColumn.Name = "purposeColumn";
        // 
        // routePrinterColumn
        // 
        routePrinterColumn.HeaderText = "Impresora";
        routePrinterColumn.MinimumWidth = 8;
        routePrinterColumn.Name = "routePrinterColumn";
        // 
        // routeContentTypeColumn
        // 
        routeContentTypeColumn.HeaderText = "Tipo contenido";
        routeContentTypeColumn.MinimumWidth = 8;
        routeContentTypeColumn.Name = "routeContentTypeColumn";
        // 
        // saveRoutesButton
        // 
        saveRoutesButton.Location = new Point(18, 440);
        saveRoutesButton.Name = "saveRoutesButton";
        saveRoutesButton.Size = new Size(140, 34);
        saveRoutesButton.TabIndex = 2;
        saveRoutesButton.Text = "Guardar rutas";
        saveRoutesButton.UseVisualStyleBackColor = true;
        saveRoutesButton.Click += saveRoutesButton_Click;
        // 
        // routesHintLabel
        // 
        routesHintLabel.ForeColor = Color.DimGray;
        routesHintLabel.Location = new Point(174, 446);
        routesHintLabel.Name = "routesHintLabel";
        routesHintLabel.Size = new Size(520, 24);
        routesHintLabel.TabIndex = 3;
        routesHintLabel.Text = "Ejemplos: ticket, kitchen_order, report, label, fiscal.";
        // 
        // settingsTab
        // 
        settingsTab.BackColor = Color.White;
        settingsTab.Controls.Add(settingsTitleLabel);
        settingsTab.Controls.Add(portLabel);
        settingsTab.Controls.Add(portInput);
        settingsTab.Controls.Add(corsCheck);
        settingsTab.Controls.Add(originsLabel);
        settingsTab.Controls.Add(originsText);
        settingsTab.Controls.Add(autoStartCheck);
        settingsTab.Controls.Add(saveSettingsButton);
        settingsTab.Controls.Add(restoreDefaultsButton);
        settingsTab.Controls.Add(originsHintLabel);
        settingsTab.Controls.Add(portHintLabel);
        settingsTab.Location = new Point(4, 34);
        settingsTab.Name = "settingsTab";
        settingsTab.Padding = new Padding(3);
        settingsTab.Size = new Size(813, 587);
        settingsTab.TabIndex = 3;
        settingsTab.Text = "Configuracion";
        // 
        // settingsTitleLabel
        // 
        settingsTitleLabel.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
        settingsTitleLabel.Location = new Point(18, 18);
        settingsTitleLabel.Name = "settingsTitleLabel";
        settingsTitleLabel.Size = new Size(720, 34);
        settingsTitleLabel.TabIndex = 0;
        settingsTitleLabel.Text = "Configuracion del agente";
        // 
        // portLabel
        // 
        portLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        portLabel.Location = new Point(18, 70);
        portLabel.Name = "portLabel";
        portLabel.Size = new Size(160, 24);
        portLabel.TabIndex = 1;
        portLabel.Text = "Puerto HTTP";
        // 
        // portInput
        // 
        portInput.Location = new Point(190, 68);
        portInput.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
        portInput.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        portInput.Name = "portInput";
        portInput.Size = new Size(120, 33);
        portInput.TabIndex = 2;
        portInput.Value = new decimal(new int[] { 9100, 0, 0, 0 });
        // 
        // corsCheck
        // 
        corsCheck.Location = new Point(18, 112);
        corsCheck.Name = "corsCheck";
        corsCheck.Size = new Size(180, 28);
        corsCheck.TabIndex = 3;
        corsCheck.Text = "Permitir CORS";
        // 
        // originsLabel
        // 
        originsLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        originsLabel.Location = new Point(18, 158);
        originsLabel.Name = "originsLabel";
        originsLabel.Size = new Size(320, 24);
        originsLabel.TabIndex = 4;
        originsLabel.Text = "Origenes permitidos (uno por linea)";
        // 
        // originsText
        // 
        originsText.Location = new Point(18, 188);
        originsText.Multiline = true;
        originsText.Name = "originsText";
        originsText.ScrollBars = ScrollBars.Vertical;
        originsText.Size = new Size(520, 110);
        originsText.TabIndex = 5;
        // 
        // autoStartCheck
        // 
        autoStartCheck.Location = new Point(18, 320);
        autoStartCheck.Name = "autoStartCheck";
        autoStartCheck.Size = new Size(360, 28);
        autoStartCheck.TabIndex = 6;
        autoStartCheck.Text = "Abrir automaticamente al iniciar Windows";
        autoStartCheck.CheckedChanged += autoStartCheck_CheckedChanged;
        // 
        // saveSettingsButton
        // 
        saveSettingsButton.Location = new Point(18, 378);
        saveSettingsButton.Name = "saveSettingsButton";
        saveSettingsButton.Size = new Size(180, 34);
        saveSettingsButton.TabIndex = 7;
        saveSettingsButton.Text = "Guardar configuracion";
        saveSettingsButton.Click += saveSettingsButton_Click;
        // 
        // restoreDefaultsButton
        // 
        restoreDefaultsButton.Location = new Point(214, 378);
        restoreDefaultsButton.Name = "restoreDefaultsButton";
        restoreDefaultsButton.Size = new Size(160, 34);
        restoreDefaultsButton.TabIndex = 8;
        restoreDefaultsButton.Text = "Restaurar defaults";
        restoreDefaultsButton.Click += restoreDefaultsButton_Click;
        // 
        // originsHintLabel
        // 
        originsHintLabel.ForeColor = Color.DimGray;
        originsHintLabel.Location = new Point(18, 418);
        originsHintLabel.Name = "originsHintLabel";
        originsHintLabel.Size = new Size(700, 24);
        originsHintLabel.TabIndex = 9;
        originsHintLabel.Text = "Si la lista queda vacia, se permite cualquier origen local/web para mantener compatibilidad.";
        // 
        // portHintLabel
        // 
        portHintLabel.ForeColor = Color.DimGray;
        portHintLabel.Location = new Point(18, 444);
        portHintLabel.Name = "portHintLabel";
        portHintLabel.Size = new Size(680, 24);
        portHintLabel.TabIndex = 10;
        portHintLabel.Text = "El agente escucha en 127.0.0.1 por defecto. Si cambia el puerto, reinicie el agente.";
        // 
        // logsTab
        // 
        logsTab.BackColor = Color.White;
        logsTab.Controls.Add(logsTitleLabel);
        logsTab.Controls.Add(logsHintLabel);
        logsTab.Controls.Add(clearEventsButton);
        logsTab.Location = new Point(4, 34);
        logsTab.Name = "logsTab";
        logsTab.Padding = new Padding(3);
        logsTab.Size = new Size(813, 587);
        logsTab.TabIndex = 4;
        logsTab.Text = "Logs";
        // 
        // logsTitleLabel
        // 
        logsTitleLabel.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
        logsTitleLabel.Location = new Point(18, 18);
        logsTitleLabel.Name = "logsTitleLabel";
        logsTitleLabel.Size = new Size(720, 34);
        logsTitleLabel.TabIndex = 0;
        logsTitleLabel.Text = "Historial local";
        // 
        // logsHintLabel
        // 
        logsHintLabel.ForeColor = Color.DimGray;
        logsHintLabel.Location = new Point(18, 62);
        logsHintLabel.Name = "logsHintLabel";
        logsHintLabel.Size = new Size(660, 24);
        logsHintLabel.TabIndex = 1;
        logsHintLabel.Text = "Los trabajos recientes tambien estan disponibles en GET /api/jobs/recent.";
        // 
        // clearEventsButton
        // 
        clearEventsButton.Location = new Point(18, 104);
        clearEventsButton.Name = "clearEventsButton";
        clearEventsButton.Size = new Size(120, 34);
        clearEventsButton.TabIndex = 2;
        clearEventsButton.Text = "Limpiar vista";
        clearEventsButton.Click += clearEventsButton_Click;
        // 
        // minimizeButton
        // 
        minimizeButton.Location = new Point(10, 737);
        minimizeButton.Name = "minimizeButton";
        minimizeButton.Size = new Size(246, 36);
        minimizeButton.TabIndex = 4;
        minimizeButton.Text = "Minimizar a la bandeja";
        minimizeButton.UseVisualStyleBackColor = true;
        minimizeButton.Click += minimizeButton_Click;
        // 
        // exitButton
        // 
        exitButton.ForeColor = Color.FromArgb(190, 40, 40);
        exitButton.Location = new Point(643, 737);
        exitButton.Name = "exitButton";
        exitButton.Size = new Size(170, 36);
        exitButton.TabIndex = 5;
        exitButton.Text = "Salir del agente";
        exitButton.UseVisualStyleBackColor = true;
        exitButton.Click += exitButton_Click;
        // 
        // apiGuideButton
        // 
        apiGuideButton.Location = new Point(262, 737);
        apiGuideButton.Name = "apiGuideButton";
        apiGuideButton.Size = new Size(170, 36);
        apiGuideButton.TabIndex = 6;
        apiGuideButton.Text = "Abrir guia API";
        apiGuideButton.UseVisualStyleBackColor = true;
        apiGuideButton.Click += apiGuideButton_Click;
        // 
        // SettingsForm
        // 
        AutoScaleDimensions = new SizeF(11F, 25F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.White;
        ClientSize = new Size(849, 785);
        Controls.Add(apiGuideButton);
        Controls.Add(exitButton);
        Controls.Add(minimizeButton);
        Controls.Add(mainTabs);
        Controls.Add(subtitleLabel);
        Controls.Add(appNameLabel);
        Controls.Add(logoPictureBox);
        Font = new Font("Segoe UI", 9.5F);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Name = "SettingsForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Imprelia Print Agent";
        ((System.ComponentModel.ISupportInitialize)logoPictureBox).EndInit();
        mainTabs.ResumeLayout(false);
        dashboardTab.ResumeLayout(false);
        printersTab.ResumeLayout(false);
        routesTab.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)routesGrid).EndInit();
        settingsTab.ResumeLayout(false);
        settingsTab.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)portInput).EndInit();
        logsTab.ResumeLayout(false);
        ResumeLayout(false);
    }
}
