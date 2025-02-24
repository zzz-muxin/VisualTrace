using Eto.Forms;
using Eto.Drawing;

namespace VisualTrace;

public class PreferencesDialog : Dialog
{
    UserSettings _userSettings = new UserSettings();
    
    private DropDown _mapProvider;
    private TextArea _customDnsResolvers;
    private NumericStepper _gridSizePercentage;
    private NumericStepper _yellowSpeed;
    private NumericStepper _redSpeed;
    private CheckBox _combineGeoOrg;
    private CheckBox _timeRounding;
    private CheckBox _hideMapPopup;
    private CheckBox _hideAddIcmpFirewallRule;
    private TextBox _queries;
    private TextBox _port;
    private TextBox _parallelRequests;
    private TextBox _maxHops;
    private TextBox _first;
    private TextBox _sendTime;
    private TextBox _ttlTime;
    private TextBox _source;
    private TextBox _dev;
    private DropDown _rdnsMode;

    public PreferencesDialog()
    {
        Title = Resources.PREFERENCES;
        //Icon = new Icon("icon.png");
        MinimumSize = new Size(500, 350);
        Padding = 10;
        
        // 创建控件
        _mapProvider = new DropDown
        {
            Items =
            {
                new ListItem { Text = Resources.MAP_PROVIDER_BAIDU, Key = "baidu" },
                new ListItem { Text = Resources.MAP_PROVIDER_AMAP, Key = "amap" }
            },
            SelectedIndex = 0,
            ID = "MapProvider"
        };
        _customDnsResolvers = new TextArea { ID = "CustomDnsResolvers" };
        _gridSizePercentage = new NumericStepper { MinValue = 10, MaxValue = 80 , ID = "GridSizePercentage" };
        _yellowSpeed = new NumericStepper { MinValue = 1, MaxValue = 1000 , ID = "YellowSpeed" };
        _redSpeed = new NumericStepper { MinValue = 1, MaxValue = 2000 , ID = "RedSpeed" };
        _combineGeoOrg = new CheckBox { Text = Resources.COMBINE_GEO_ORG , ID = "CombineGeoOrg" };
        _timeRounding = new CheckBox { Text = Resources.TIME_ROUNDING , ID = "TimeRounding" };
        _hideMapPopup = new CheckBox { Text = Resources.HIDE_MAP_POPUP , ID = "HideMapPopup" };
        _hideAddIcmpFirewallRule = new CheckBox { Text = Resources.HIDE_ADD_FIREWALL_PROMPT , ID = "HideAddIcmpFirewallRule" };

        _queries = new TextBox { PlaceholderText = "3" , ID = "Queries" };
        _port = new TextBox { PlaceholderText = "80/tcp 53/udp 1/icmp" , ID = "Port" };
        _parallelRequests = new TextBox { PlaceholderText = "18" , ID = "ParallelRequests" };
        _maxHops = new TextBox { PlaceholderText = "30" , ID = "MaxHops" };
        _first = new TextBox { PlaceholderText = "1" , ID = "First" };
        _sendTime = new TextBox { PlaceholderText = "100" , ID = "SendTime" };
        _ttlTime = new TextBox { PlaceholderText = "500" , ID = "TtlTime" };
        _source = new TextBox { ID = "Source" };
        _dev = new TextBox{ ID = "Dev" };
        _rdnsMode = new DropDown
        {
            Items =
            {
                new ListItem { Text = Resources.RDNS_MODE_DEFAULT, Key = "default" },
                new ListItem { Text = Resources.RDNS_MODE_DISABLE, Key = "disable" },
                new ListItem { Text = Resources.RDNS_MODE_ALWAYS, Key = "always" }
            },
            SelectedIndex = 0,
            ID = "RdnsMode"
        };
        Content = CreateLayout();
        
        ApplyUserSettings();  // 确保 UI 完成后再应用设置
    }

    // 设置布局
    private Control CreateLayout()
    {
        var generalTab = new TabPage { Text = Resources.GENERAL, Content = CreateGeneralLayout() };
        var tracerouteTab = new TabPage { Text = Resources.TRACEROUTE, Content = CreateTracerouteLayout() };

        var tabControl = new TabControl { Pages = { generalTab, tracerouteTab } };

        var saveButton = new Button { Text = Resources.SAVE };
        saveButton.Click += SaveButton_Click;

        var cancelButton = new Button { Text = Resources.CANCEL };
        cancelButton.Click += CancelButton_Click;

        return new TableLayout
        {
            Spacing = new Size(5, 5),
            Rows =
            {
                new TableRow(tabControl) { ScaleHeight = true },
                new TableRow(new TableLayout
                {
                    Spacing = new Size(5, 5),
                    Rows = { new TableRow(new Label { Text = Resources.RESTART_TO_APPLY }, new TableCell { ScaleWidth = true }, saveButton, cancelButton) }
                })
            }
        };
    }
    
    // 常规设置页面
    private Control CreateGeneralLayout()
    {
        return new TableLayout
        {
            Padding = 10,
            Spacing = new Size(10, 10),
            Rows =
            {
                new TableRow(new Label { Text = Resources.MAP_PROVIDER }, _mapProvider),
                new TableRow(new Label { Text = Resources.CUSTOM_DNS_RESOLVERS }, _customDnsResolvers),
                new TableRow(new Label { Text = Resources.GRID_SIZE_RATIO }, _gridSizePercentage),
                new TableRow(
                    new Label { Text = Resources.DELAY_TIME },
                    new TableLayout
                    {
                        Spacing = new Size(5, 5),
                        Rows =
                        {
                            new TableRow(
                                new Label { Text = Resources.YELLOW_SPEED },
                                _yellowSpeed,
                                new Label { Text = Resources.RED_SPEED },
                                _redSpeed
                                )
                        }
                    }
                ),
                new TableRow(_combineGeoOrg),
                new TableRow(_timeRounding),
                new TableRow(_hideMapPopup),
                new TableRow(_hideAddIcmpFirewallRule),
                new TableRow { ScaleHeight = true }
            }
        };
    }

    // 路由跟踪设置页面
    private Control CreateTracerouteLayout()
    {
        return new TableLayout
        {
            Padding = 10,
            Spacing = new Size(5, 5),
            Rows =
            {
                new TableRow(new Label { Text = Resources.QUERIES_SETTING }, _queries),
                new TableRow(new Label { Text = Resources.DST_PORT_INIT_SEQ }, _port),
                new TableRow(new Label { Text = Resources.PARALLEL_REQ }, _parallelRequests),
                new TableRow(new Label { Text = Resources.MAX_HOPS }, _maxHops),
                new TableRow(new Label { Text = Resources.FIRST_TTL_HOP }, _first),
                new TableRow(new Label { Text = Resources.PACKET_INTERVAL }, _sendTime),
                new TableRow(new Label { Text = Resources.PACKET_GROUP_INTERVAL }, _ttlTime),
                new TableRow(new Label { Text = Resources.SRC_ADDR_SETTING }, _source),
                new TableRow(new Label { Text = Resources.SRC_INTERFACE_SETTING }, _dev),
                new TableRow(new Label { Text = Resources.RDNS_MODE }, _rdnsMode),
                new TableRow { ScaleHeight = true }
            }
        };
    }
    
    // 将每个属性的值同步到对应的 UI 控件中
    private void ApplyUserSettings()
    {
        foreach (var setting in _userSettings.GetType().GetProperties())
        {
            var settingTextBox = FindChild<TextBox>(setting.Name);
            if (settingTextBox != null)
            {
                settingTextBox.Text = setting.GetValue(_userSettings, null) as string ?? "";
            }
            var settingCheckBox = FindChild<CheckBox>(setting.Name);
            if (settingCheckBox != null)
            {
                settingCheckBox.Checked = (setting.GetValue(_userSettings, null) as bool?) ?? false;
            }
            var settingDropDown = FindChild<DropDown>(setting.Name);
            if (settingDropDown != null)
            {
                settingDropDown.SelectedKey = setting.GetValue(_userSettings, null) as string ?? "";
            }
            var settingTextArea = FindChild<TextArea>(setting.Name);
            if (settingTextArea != null)
            {
                settingTextArea.Text = setting.GetValue(_userSettings, null) as string ?? "";
            }
        }
        FindChild<NumericStepper>("YellowSpeed").Value = UserSettings.YellowSpeed;
        FindChild<NumericStepper>("RedSpeed").Value = UserSettings.RedSpeed;
        // 根据设置配置grid高度
        FindChild<NumericStepper>("GridSizePercentage").Value = UserSettings.GridSizePercentage * 100;
    }
    
    // 点击保存按钮
    private void SaveButton_Click(object? sender, EventArgs e)
    {
        // 将每个 UI 控件中的值保存到设置中
        foreach (var setting in _userSettings.GetType().GetProperties())
        {
            TextBox settingTextBox = FindChild<TextBox>(setting.Name);
            if (settingTextBox != null)
            {
                setting.SetValue(_userSettings, settingTextBox.Text);
            }
            CheckBox settingCheckBox = FindChild<CheckBox>(setting.Name);
            if (settingCheckBox != null)
            {
                setting.SetValue(_userSettings, settingCheckBox.Checked);
            }
            DropDown settingDropDown = FindChild<DropDown>(setting.Name);
            if (settingDropDown != null)
            {
                setting.SetValue(_userSettings, settingDropDown.SelectedKey);
            }
            TextArea settingTextArea = FindChild<TextArea>(setting.Name);
            if (settingTextArea != null)
            {
                setting.SetValue(_userSettings, settingTextArea.Text);
            }
        }
        UserSettings.GridSizePercentage = FindChild<NumericStepper>("GridSizePercentage").Value / 100;
        UserSettings.YellowSpeed = FindChild<NumericStepper>("YellowSpeed").Value;
        UserSettings.RedSpeed = FindChild<NumericStepper>("RedSpeed").Value;
        if (UserSettings.RedSpeed < UserSettings.YellowSpeed)
        {
            UserSettings.RedSpeed = UserSettings.YellowSpeed;
        }
        UserSettings.SaveSettings();  // 保存设置
        
        //IPDBLoader.Load();  // 加载IP数据库
        Close();
    }

    // 点击取消按钮
    private void CancelButton_Click(object? sender, EventArgs e)
    {
        Close();
    }
    
}