using Eto.Forms;
using Eto.Drawing;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Records;
using Newtonsoft.Json;

namespace VisualTrace;

public class MainForm : Form
{
    // traceroute返回的结果集
    private ObservableCollection<TracerouteHop> _tracerouteResultCollection = new();
    private static Tracer Tracer { get; set; } // Traceroute进程实例
    private ComboBox _hostInputBox; // IP或域名输入框
    private GridView _tracerouteGridView; // traceroute视图
    private CheckBox _mtrMode; // MTR模式
    private WebView _mapWebView; // 地图视图
    private DropDown _resolvedIpSelection; // 解析的IP选择框
    private DropDown _dataProviderSelection; // 数据提供方选择框
    private DropDown _protocolSelection; // 协议选择框
    private DropDown _dnsResolverSelection; // DNS方式选择框
    private Button _startTracerouteButton; // 开始按钮
    private TipBar _tipBar = new ();
    private bool _gridResizing; // 用于调整GridView尺寸
    private bool _appForceExiting; // app强制退出标志
    private bool _enterPressed; // 输入框按下enter键标志

    private ExceptionalOutputForm _exceptionalOutputForm = new(); // 异常输出

    public MainForm()
    {
        // IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());
        // foreach (var ip in ipEntry.AddressList)
        // {
        //     Console.WriteLine("IP Address: " + ip);
        // }
        // 初始化窗口
        Title = Resources.APPTITLE;
        //Icon = new Icon("icon.png");
        MinimumSize = new Size(900, 600);
        
        // 监听窗口 Shown 事件，在窗口显示时居中
        Shown += (sender, e) =>
        {
            var screen = Screen.PrimaryScreen.Bounds;  // 获取主屏幕尺寸
            Location = new Point(
                (int)((screen.Width - Width) / 2), 
                (int)((screen.Height - Height) / 2)
            );
        };

        // 创建菜单项
        // 退出
        var quitCommand = new Command
            { MenuText = Resources.QUIT };
        quitCommand.Executed += (sender, e) => Application.Instance.Quit();
        // git主页
        var gitHomePageCommand = new Command { MenuText = Resources.HOMEPAGE };
        gitHomePageCommand.Executed += (sender, e) => Process.Start(new ProcessStartInfo("https://github.com/zzz-muxin")
            { UseShellExecute = true });
        // 设置
        var preferenceCommand = new Command
            { MenuText = Resources.PREFERENCES };
        preferenceCommand.Executed += (sender, e) =>
        {
            // 新建设置对话框
            new PreferencesDialog().ShowModal(this);
            // 关闭设置后刷新 DNS 服务器列表
            LoadDnsResolvers();
            // 重新绘制
            _tipBar.Invalidate();
            // 重新设置地图来源
            SetMap();
            // 检查合并地理位置和组织
            MTRMode_CheckedChanged(null, null);
            // 刷新grid高度大小
            MainForm_SizeChanged(sender, e);
        };

        // 创建菜单栏
        Menu = new MenuBar
        {
            Items =
            {
                new SubMenuItem
                {
                    Text = Resources.MENU, Items =
                    {
                        preferenceCommand,
                        quitCommand
                    }
                },
                new SubMenuItem
                {
                    Text = Resources.ABOUT, Items =
                    {
                        gitHomePageCommand
                    }
                }
            }
        };

        // 创建控件
        // 输入框
        _hostInputBox = new ComboBox { Text = "" , ID = "HostInputBox"};
        _hostInputBox.KeyDown += HostInputBox_KeyDown;
        _hostInputBox.KeyUp += HostInputBox_KeyUp;
        _hostInputBox.TextChanged += ResolveParamChanged;
        // _hostInputBox.GotFocus += (sender, e) =>
        // {
        //     if (_hostInputBox.Text == "输入IP或域名")
        //     {
        //         _hostInputBox.Text = ""; // 清空占位符
        //     }
        // };
        //
        // _hostInputBox.LostFocus += (sender, e) =>
        // {
        //     if (string.IsNullOrEmpty(_hostInputBox.Text))
        //     {
        //         _hostInputBox.Text = "输入IP或域名"; // 重新显示占位符
        //     }
        // };
        if (!string.IsNullOrEmpty(UserSettings.TraceHistory))
            foreach (var item in UserSettings.TraceHistory.Split('\n'))
                if (item != "")
                    _hostInputBox.Items.Add(item);

        // MTR 模式
        _mtrMode = new CheckBox { Text = Resources.MTR_MODE };
        _mtrMode.CheckedChanged += MTRMode_CheckedChanged;

        // dns解析IP框设置不可见
        _resolvedIpSelection = new DropDown { Visible = false };

        // 开始按钮
        _startTracerouteButton = new Button
        {
            Text = Resources.START
        };
        _startTracerouteButton.Click += StartTracerouteButton_Click;

        // 协议选择
        _protocolSelection = new DropDown
        {
            Items =
            {
                new ListItem { Text = "ICMP", Key = "" },
                new ListItem { Text = "TCP", Key = "-T" },
                new ListItem { Text = "UDP", Key = "-U" }
            },
            SelectedIndex = 0,
            ToolTip = Resources.PROTOCOL_FOR_TRACEROUTING
        };
        _protocolSelection.SelectedKey = UserSettings.SelectedProtocol;
        _protocolSelection.SelectedKeyChanged += (sender, e) =>
        {
            UserSettings.SelectedProtocol = _protocolSelection.SelectedKey;
            UserSettings.SaveSettings();
        };

        // 数据源选择
        _dataProviderSelection = new DropDown
        {
            Items =
            {
                new ListItem { Text = "LeoMoeAPI", Key = "" },
                new ListItem { Text = Resources.DISABLE_IPGEO, Key = "--data-provider disable-geoip" }
            },
            SelectedIndex = 0,
            ToolTip = Resources.IP_GEO_DATA_PROVIDER
        };
        _dataProviderSelection.SelectedKey = UserSettings.SelectedDataProvider;
        _dataProviderSelection.SelectedKeyChanged += (sender, e) =>
        {
            UserSettings.SelectedDataProvider = _dataProviderSelection.SelectedKey;
            UserSettings.SaveSettings();
        };

        // DNS解析选择
        _dnsResolverSelection = new DropDown();
        _dnsResolverSelection.SelectedKeyChanged += ResolveParamChanged;
        LoadDnsResolvers();
        _dnsResolverSelection.SelectedKey = UserSettings.SelectedDnsResolver;
        _dnsResolverSelection.SelectedKeyChanged += (sender, e) =>
        {
            UserSettings.SelectedDnsResolver = _dnsResolverSelection.SelectedKey;
            UserSettings.SaveSettings();
        };

        // traceroute视图表格
        _tracerouteGridView = new GridView { DataStore = _tracerouteResultCollection };
        _tracerouteGridView.MouseUp += Dragging_MouseUp;
        _tracerouteGridView.SelectedRowsChanged += TracerouteGridView_SelectedRowsChanged;
        // 三个复制命令
        var copyIpCommand = new Command { MenuText = Resources.COPY + "IP" };
        var copyGeolocationCommand = new Command { MenuText = Resources.COPY + Resources.GEOLOCATION };
        var copyHostnameCommand = new Command { MenuText = Resources.COPY + Resources.HOSTNAME };
        _tracerouteGridView.ContextMenu = new ContextMenu
        {
            Items =
            {
                copyIpCommand,
                copyGeolocationCommand,
                copyHostnameCommand
            }
        };
        copyIpCommand.Executed += (sender, e) =>
        {
            Clipboard.Instance.Text = _tracerouteResultCollection[_tracerouteGridView.SelectedRow].Ip;
        };
        copyGeolocationCommand.Executed += (sender, e) =>
        {
            if (UserSettings.CombineGeoOrg)
                Clipboard.Instance.Text = _tracerouteResultCollection[_tracerouteGridView.SelectedRow]
                    .GeolocationAndOrganization;
            else
                Clipboard.Instance.Text = _tracerouteResultCollection[_tracerouteGridView.SelectedRow].Geolocation;
        };
        copyHostnameCommand.Executed += (sender, e) =>
        {
            Clipboard.Instance.Text = _tracerouteResultCollection[_tracerouteGridView.SelectedRow].Hostname;
        };

        // 添加Traceroute表格表头
        AddGridColumnsTraceroute();

        // 地图视图
        _mapWebView = new WebView();
        SetMap();

        // 绑定地图刷新事件
        _mapWebView.DocumentLoaded += (sender6, e6) => { ResetMap(); };

        // windows平台检查
        PlatformChecks();

        // 绑定表格视图大小调整事件
        SizeChanged += MainForm_SizeChanged;
        MouseDown += Dragging_MouseDown;
        MouseUp += Dragging_MouseUp;
        MouseMove += MainForm_MouseMove;

        // 使用 Table 布局添加控件
        var layout = new TableLayout
        {
            Padding = new Padding(10),
            Spacing = new Size(5, 5),
            Rows =
            {
                new TableRow
                {
                    Cells =
                    {
                        new TableLayout
                        {
                            Spacing = new Size(10, 10),
                            Rows =
                            {
                                new TableRow
                                {
                                    Cells =
                                    {
                                        new TableCell(_hostInputBox, true),
                                        _resolvedIpSelection,
                                        _mtrMode,
                                        _protocolSelection,
                                        _dnsResolverSelection,
                                        _dataProviderSelection,
                                        _tipBar,
                                        _startTracerouteButton
                                    }
                                }
                            }
                        }
                    }
                },
                new TableRow
                {
                    Cells = { _tracerouteGridView }
                },
                new TableRow
                {
                    Cells = { _mapWebView }
                }
            }
        };
        Content = layout;

        _hostInputBox.Focus(); // 自动聚焦输入框
    }

    // 加载DNS服务
    private void LoadDnsResolvers()
    {
        // 清空下拉列表
        _dnsResolverSelection.Items.Clear();
        // 添加系统默认的 DNS 解析器
        _dnsResolverSelection.Items.Add(new ListItem { Text = Resources.SYSTEM_DNS_RESOLVER, Key = "system" });
        // 读取用户自定义的 DNS 解析器
        if (!string.IsNullOrEmpty(UserSettings.CustomDnsResolvers))
        {
            // 去除回车符
            var resolvers = UserSettings.CustomDnsResolvers.Replace("\r", "");
            // 按换行符分割成字符串数组
            foreach (var item in resolvers.Split('\n'))
            {
                // 再将 DNS 解析器地址和名称分开
                var resolver = item.Split('#');
                // 确保是一个有效的 DNS 解析器地址
                IPAddress resolverIp;
                if (resolver[0] != "" && (resolver[0].IndexOf("https://") == 0 ||
                                          IPAddress.TryParse(resolver[0], out resolverIp)))
                    // 地址合法才添加到下拉列表中
                    _dnsResolverSelection.Items.Add(new ListItem
                        { Text = resolver.Length == 2 ? resolver[1] : resolver[0], Key = resolver[0] });
            }
        }

        _dnsResolverSelection.SelectedIndex = 0;
    }

    // 初始化期间进行windows平台检查
    private void PlatformChecks()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && UserSettings.HideAddIcmpFirewallRule != true)
        {
            // 提示 Windows 用户添加防火墙规则放行 ICMP 
            if (MessageBox.Show(Resources.ASK_ADD_ICMP_FIREWALL_RULE, Resources.TIP, MessageBoxButtons.YesNo,
                    MessageBoxType.Question) ==
                DialogResult.Yes)
            {
                // 以管理员权限运行命令
                var allowIcmp = new Process(); // 创建一个新的进程对象 allowIcmp，用于执行 Windows 命令
                // 进程开始前的参数设置
                allowIcmp.StartInfo.FileName = "cmd.exe"; // 让 cmd.exe 执行 Windows 命令
                allowIcmp.StartInfo.UseShellExecute = true; // 允许使用外部 Shell 执行命令
                allowIcmp.StartInfo.Verb = "runas"; // 以管理员权限运行 cmd.exe
                // 使用 netsh 命令添加 Windows 防火墙规则
                // 规则名称：All ICMP v4 (Traceroute)
                // 方向：dir=in（入站流量）
                // 允许 ICMPv4和v6 任何类型的流量：protocol=[icmp]:any,any
                allowIcmp.StartInfo.Arguments =
                    "/c \"netsh advfirewall firewall add rule name=\"\"\"All ICMP v4 (Traceroute)\"\"\" dir=in action=allow protocol=icmpv4:any,any && netsh advfirewall firewall add rule name=\"\"\"All ICMP v6 (Traceroute)\"\"\" dir=in action=allow protocol=icmpv6:any,any\"";
                try
                {
                    allowIcmp.Start();
                    UserSettings.HideAddIcmpFirewallRule = true; // 隐藏添加规则提示
                    UserSettings.SaveSettings(); // 保存设置
                    MessageBox.Show( Resources.ICMP_RULE_ADD_SUCCESS , Resources.TIP );
                }
                catch (Win32Exception)
                {
                    MessageBox.Show(Resources.FAILED_TO_ADD_RULES, MessageBoxType.Error);
                }
            }
            else
            {
                UserSettings.HideAddIcmpFirewallRule = true;
                UserSettings.SaveSettings();
            }
        }
    }

    // 如果文本框被修改，则隐藏 DNS 解析选择框
    private void ResolveParamChanged(object? sender, EventArgs e)
    {
        if (_resolvedIpSelection.Visible)
        {
            _resolvedIpSelection.Items.Clear();
            _resolvedIpSelection.Visible = false;
        }
    }

    // MTR勾选改变时转换Traceroute视图
    private void MTRMode_CheckedChanged(object? sender, EventArgs? e)
    {
        if ((bool)_mtrMode.Checked)
            AddGridColumnsMtr();
        else
            AddGridColumnsTraceroute();
    }

    // 输入框按下enter键
    private void HostInputBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Keys.Enter && !_enterPressed) _enterPressed = true;
    }

    // 输入框弹起enter键
    private void HostInputBox_KeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Keys.Enter && _enterPressed)
        {
            _enterPressed = false;
            StartTracerouteButton_Click(sender, e);
        }
    }

    // 选中表格某行时地图跳转到其地理位置
    private void TracerouteGridView_SelectedRowsChanged(object? sender, EventArgs e)
    {
        FocusMapPoint(_tracerouteGridView.SelectedRow);
    }

    // 按下开始按钮
    private void StartTracerouteButton_Click(object? sender, EventArgs e)
    {
        // Windows只能选ICMP协议，暂不支持TCP/UDP
        if (_protocolSelection.SelectedValue.ToString() != "ICMP" &&
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            MessageBox.Show(Resources.WINDOWS_TCP_UDP_UNSUPPORTED , Resources.TIP);
            return;
        }

        // 当前Traceroute进程实例非空，先停止当前进程实例
        if (Tracer != null)
        {
            Console.WriteLine(Tracer);
            StopTraceroute();
            _appForceExiting = false;
            Tracer = null;
            return;
        }

        // 尝试新建Traceroute进程实例
        try
        {
            Tracer = new Tracer();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, MessageBoxType.Error);
        }

        // 检查输入框非空
        if (_hostInputBox.Text == "")
        {
            MessageBox.Show(Resources.EMPTY_HOSTNAME_MSGBOX , Resources.TIP);
            Tracer = null;
            return;
        }

        _tracerouteResultCollection.Clear(); // 清空原有Traceroute数据集
        ResetMap(); // 重置地图
        Title = Resources.APPTITLE; // 窗口标题更新
        // 预处理文本框输入
        string readyToUseIp;
        // 如果解析IP选择框可见且已选择一个解析的IP
        if (_resolvedIpSelection.Visible && _resolvedIpSelection.SelectedIndex != 0)
        {
            // 添加IP到窗口标题
            readyToUseIp = _resolvedIpSelection.SelectedKey;
            Title = Resources.APPTITLE + ": " + _hostInputBox.Text + " (" + readyToUseIp + ")";
        }
        // 如果解析IP选择框可见但没选择IP
        else if (_resolvedIpSelection.Visible && _resolvedIpSelection.SelectedIndex == 0)
        {
            MessageBox.Show(Resources.SELECT_IP_MSGBOX , Resources.TIP);
            Tracer = null; // Traceroute进程实例设为空
            return;
        }
        // 解析IP选择框不可见，进行域名解析
        else
        {
            _resolvedIpSelection.Visible = false; // 隐藏 IP 选择框
            IPAddress userInputAddress;
            _hostInputBox.Text = _hostInputBox.Text.Trim(); // 去除输入框两侧的空格
            Uri uri;
            if (Uri.TryCreate(_hostInputBox.Text, UriKind.Absolute, out uri) && uri.Host != "")
                // 是合法的 URL
                _hostInputBox.Text = uri.Host;

            // 如果有冒号而且有点(IPv4)，去除冒号后面的内容
            if (_hostInputBox.Text.IndexOf(":") != -1 && _hostInputBox.Text.IndexOf(".") != -1)
                _hostInputBox.Text = _hostInputBox.Text.Split(':')[0];

            if (IPAddress.TryParse(_hostInputBox.Text, out userInputAddress))
            {
                // 是合法的 IPv4 / IPv6，把程序处理后的IP放回文本框
                //_hostInputBox.Text = userInputAddress.ToString();
                readyToUseIp = userInputAddress.ToString();
                Title = Resources.APPTITLE + ": " + readyToUseIp;
            }
            else
            {
                try
                {
                    // 尝试域名解析
                    Title = Resources.APPTITLE + ": " + _hostInputBox.Text;
                    var resolvedAddresses = ResolveHost(_hostInputBox.Text);
                    // 域名解析成功时
                    if (resolvedAddresses.Length > 1)
                    {
                        _resolvedIpSelection.Items.Clear();
                        _resolvedIpSelection.Items.Add(Resources.SELECT_IP_DROPDOWN);
                        foreach (var resolvedAddress in resolvedAddresses)
                            _resolvedIpSelection.Items.Add(resolvedAddress.ToString());

                        _resolvedIpSelection.SelectedIndex = 0;
                        _resolvedIpSelection.Visible = true;
                        Tracer = null;
                        return;
                    }
                    else
                    {
                        readyToUseIp = resolvedAddresses[0].ToString();
                        Title = Resources.APPTITLE + ": " + _hostInputBox.Text + " (" + readyToUseIp + ")";
                    }
                }
                catch (SocketException)
                {
                    // 域名解析套接字异常
                    MessageBox.Show(string.Format(Resources.NAME_NOT_RESOLVED, _hostInputBox.Text),
                        MessageBoxType.Warning);
                    Title = Resources.APPTITLE;
                    Tracer = null;
                    return;
                }
                catch (Exception exception)
                {
                    // 其他异常
                    MessageBox.Show(exception.Message, MessageBoxType.Error);
                    Title = Resources.APPTITLE;
                    Tracer = null;
                    return;
                }
            }
        }

        var newText = _hostInputBox.Text;
        IList<IListItem> clone = _hostInputBox.Items.ToList(); // 清理重复记录
        foreach (var toRemove in
                 clone.Where(s => s.Text == newText))
            _hostInputBox.Items.Remove(toRemove); // 不知道为什么清理掉 ComboBox 的 Item 会把同名文本框的内容一起清掉

        _hostInputBox.Text = newText; // 所以得在这里重新放回去
        _hostInputBox.Items.Insert(0, new ListItem { Text = newText });
        while (_hostInputBox.Items.Count > 20) // 清理20条以上记录
            _hostInputBox.Items.RemoveAt(_hostInputBox.Items.Count - 1);

        // 添加到历史记录
        UserSettings.TraceHistory = string.Join("\n", _hostInputBox.Items.Select(item => item.Text));
        UserSettings.SaveSettings();

        _startTracerouteButton.Text = Resources.STOP;

        // 处理Traceroute进程实例发回的结果
        
        // CurrentInstance.ExceptionalOutput += Instance_ExceptionalOutput; // 异常输出
        //CurrentInstance.AppQuit += Instance_AppQuit; // 进程退出
        // 启动Traceroute进程
        // todo
        Tracer.Output.CollectionChanged += Instance_OutputCollectionChanged; // 结果集改变时
        Tracer.Run(readyToUseIp, (bool)_mtrMode.Checked, _dataProviderSelection.SelectedKey,
            _protocolSelection.SelectedKey);
    }

    // dns解析
    private IPAddress[] ResolveHost(string host)
    {
        var resolver = _dnsResolverSelection.SelectedKey;
        if (resolver == "system")
        {
            // 使用系统解析
            return Dns.GetHostAddresses(host);
        }
        else
        {
            // todo 使用自定义 DNS 服务器
            IDnsClient dnsClient = new DnsUdpClient(IPAddress.Parse(resolver));
            var result = Task.Run(() => dnsClient.Query(DnsQueryFactory.CreateQuery(host))).Result;

            if (result.Answers.Count == 0)
            {
                throw new SocketException();
            }
            else
            {
                List<IPAddress> addressList = new();
                foreach (var answer in result.Answers)
                    if (answer.Type == Ae.Dns.Protocol.Enums.DnsQueryType.A ||
                        answer.Type == Ae.Dns.Protocol.Enums.DnsQueryType.AAAA)
                        addressList.Add(((DnsIpAddressResource)answer.Resource).IPAddress);

                return addressList.ToArray();
            }
        }
    }

    // Traceroute进程实例退出检查
    private void Instance_AppQuit(object? sender, AppQuitEventArgs e)
    {
        Application.Instance.InvokeAsync(() =>
        {
            Tracer = null;
            _startTracerouteButton.Text = Resources.START;
            if (_appForceExiting != true && e.ExitCode != 0)
                // 主动结束，退出代码不为 0 则证明有异常
                MessageBox.Show(Resources.EXCEPTIONAL_EXIT_MSG + e.ExitCode, MessageBoxType.Warning);

            // 强制结束一般退出代码不为 0，不提示异常。
            _appForceExiting = false;
        });
    }

    // Traceroute进程实例异常输出
    private void Instance_ExceptionalOutput(object? sender, ExceptionalOutputEventArgs e)
    {
        Application.Instance.InvokeAsync(() =>
        {
            _exceptionalOutputForm.Show();
            if (!_exceptionalOutputForm.Visible) _exceptionalOutputForm.Visible = true;

            _exceptionalOutputForm.AppendOutput(e.Output);
        });
    }

    // Traceroute进程实例输出结果改变
    private void Instance_OutputCollectionChanged(object? sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            Application.Instance.InvokeAsync(() =>
            {
                try
                {
                    var result = (TracerouteResult)e.NewItems[0];
                    // result = IpdbLoader.Rewrite(result);
                    var hopNo = int.Parse(result.Hop);
                    if (hopNo > _tracerouteResultCollection.Count)
                    {
                        // 正常添加新的跳
                        _tracerouteResultCollection.Add(new TracerouteHop(result));
                        UpdateMap(result);
                        _tracerouteGridView.ScrollToRow(_tracerouteResultCollection.Count - 1);
                    }
                    else
                    {
                        // 修改现有的跳
                        _tracerouteResultCollection[hopNo - 1].HopData.Add(result);
                        _tracerouteGridView.ReloadData(hopNo - 1);
                    }
                }
                catch (Exception exception)
                {
                    MessageBox.Show(
                        $"Message: ${exception.Message} \nSource: ${exception.Source} \nStackTrace: ${exception.StackTrace}",
                        "Exception Occurred");
                }
            });
    }

    // 停止Traceroute进程实例
    private void StopTraceroute()
    {
        if (Tracer != null && !_appForceExiting)
        {
            Console.WriteLine("Stopping traceroute");
            _appForceExiting = true;
            Tracer.Stop();
            _startTracerouteButton.Text = Resources.START;
        }
    }

    // 处理拖拽调整 GridView 大小
    private void Dragging_MouseDown(object? sender, MouseEventArgs e)
    {
        // 鼠标按下
        if (e.Location.Y >= _tracerouteGridView.Bounds.Bottom + 15 &&
            e.Location.Y <= _tracerouteGridView.Bounds.Bottom + 20)
        {
            _gridResizing = true;
            _mapWebView.Enabled = false;
        }
    }

    private void Dragging_MouseUp(object? sender, MouseEventArgs e)
    {
        // 鼠标弹起
        _gridResizing = false;
        _mapWebView.Enabled = true;
        // 保存GridView尺寸到设置中
        UserSettings.SaveSettings();
    }

    // 鼠标移动改变gridview大小
    private void MainForm_MouseMove(object? sender, MouseEventArgs e)
    {
        // 设置鼠标指针
        if (e.Location.Y >= _tracerouteGridView.Bounds.Bottom + 15 &&
            e.Location.Y <= _tracerouteGridView.Bounds.Bottom + 20)
            Cursor = Cursors.SizeBottom;
        else
            Cursor = Cursors.Default;

        if (e.Buttons == MouseButtons.Primary && _gridResizing)
            if ((int)e.Location.Y > _tracerouteGridView.Bounds.Top + 100) // 最小调整为100px
            {
                _tracerouteGridView.Height = (int)e.Location.Y - _tracerouteGridView.Bounds.Top - 15;
                UserSettings.GridSizePercentage = (double)_tracerouteGridView.Height / (Height - 75); // 保存比例
            }
    }

    // gridview尺寸改变
    private void MainForm_SizeChanged(object? sender, EventArgs e)
    {
        int gridHeight;
        var totalHeight = Height - 75; // 减去边距和上面的文本框的75px
        gridHeight = (int)(totalHeight * UserSettings.GridSizePercentage);
        _tracerouteGridView.Height = gridHeight; // 按比例还原高度
    }

    // 更新地图
    private void UpdateMap(TracerouteResult result)
    {
        try
        {
            // 把 Result 转换为 JSON
            var resultJson = JsonConvert.SerializeObject(result);
            // 通过 ExecuteScript 把结果传进去
            _mapWebView.ExecuteScriptAsync(@"window.traceroute.addHop(`" + resultJson + "`);");
        }
        catch (Exception e)
        {
            MessageBox.Show($"Message: ${e.Message} \nSource: ${e.Source} \nStackTrace: ${e.StackTrace}",
                "Exception Occurred");
        }
    }

    // 聚焦地图选点
    private void FocusMapPoint(int hopNo)
    {
        try
        {
            _mapWebView.ExecuteScriptAsync(@"window.traceroute.focusHop(" + hopNo + ");");
        }
        catch (Exception e)
        {
            MessageBox.Show($"Message: ${e.Message} \nSource: ${e.Source} \nStackTrace: ${e.StackTrace}",
                "Exception Occurred");
        }
    }

    // 重置地图
    private void ResetMap()
    {
        try
        {
            // 判断 WebView 的当前 URL，选择不同的地图 API
            switch (_mapWebView.Url.Host)
            {
                // todo
                // case "geo-devrel-javascript-samples.web.app":
                //     _mapWebView.ExecuteScriptAsync(Resources.googleMap);
                //     break;
                case "lbs.baidu.com":
                    _mapWebView.ExecuteScriptAsync(Resources.baiduMap);
                    break;
            }
            // todo
            _mapWebView.ExecuteScriptAsync("window.traceroute.reset(" + UserSettings.HideMapPopup.ToString().ToLower() +
                                           ")");
        }
        catch (Exception e)
        {
            MessageBox.Show($"Message: ${e.Message} \nSource: ${e.Source} \nStackTrace: ${e.StackTrace}",
                "Exception Occurred");
        }
    }

    // 常规表格数据项
    private void AddGridColumnsTraceroute()
    {
        _tracerouteGridView.Columns.Clear();
        // 指定栏位数据源
        _tracerouteGridView.Columns.Add(new GridColumn
        {
            DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteHop, string>(r => r.Hop) },
            HeaderText = Resources.HOP
        });
        _tracerouteGridView.Columns.Add(new GridColumn
        {
            DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteHop, string>(r => r.Ip) },
            HeaderText = "IP"
        });
        _tracerouteGridView.Columns.Add(new GridColumn
        {
            DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteHop, string>(r => r.Time) },
            HeaderText = Resources.TIME_MS
        });
        // 是否合并位置和运营商
        if (UserSettings.CombineGeoOrg)
        {
            _tracerouteGridView.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell
                    { Binding = Binding.Property<TracerouteHop, string>(r => r.GeolocationAndOrganization) },
                HeaderText = Resources.GEOLOCATION
            });
        }
        else
        {
            _tracerouteGridView.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteHop, string>(r => r.Geolocation) },
                HeaderText = Resources.GEOLOCATION
            });
            _tracerouteGridView.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteHop, string>(r => r.Organization) },
                HeaderText = Resources.ORGANIZATION
            });
        }

        _tracerouteGridView.Columns.Add(new GridColumn
        {
            DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteHop, string>(r => r.As) },
            HeaderText = "AS"
        });
        _tracerouteGridView.Columns.Add(new GridColumn
        {
            DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteHop, string>(r => r.Hostname) },
            HeaderText = Resources.HOSTNAME
        });
        DrawTipColor();
    }

    // 开启MTR后的表格数据项
    private void AddGridColumnsMtr()
    {
        _tracerouteGridView.Columns.Clear();
        // 指定栏位数据源
        _tracerouteGridView.Columns.Add(new GridColumn
        {
            DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteHop, string>(r => r.Hop) },
            HeaderText = Resources.HOP
        });
        _tracerouteGridView.Columns.Add(new GridColumn
        {
            DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteHop, string>(r => r.Ip) },
            HeaderText = "IP"
        });
        // 合并位置和运营商
        if (UserSettings.CombineGeoOrg)
        {
            _tracerouteGridView.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell
                    { Binding = Binding.Property<TracerouteHop, string>(r => r.GeolocationAndOrganization) },
                HeaderText = Resources.GEOLOCATION
            });
        }
        else
        {
            _tracerouteGridView.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteHop, string>(r => r.Geolocation) },
                HeaderText = Resources.GEOLOCATION
            });
            _tracerouteGridView.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteHop, string>(r => r.Organization) },
                HeaderText = Resources.ORGANIZATION
            });
        }

        _tracerouteGridView.Columns.Add(new GridColumn
        {
            DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteHop, string>(r => r.Loss.ToString()) },
            HeaderText = Resources.LOSS
        });

        _tracerouteGridView.Columns.Add(new GridColumn
        {
            DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteHop, string>(r => r.Sent.ToString()) },
            HeaderText = Resources.SENT
        });

        _tracerouteGridView.Columns.Add(new GridColumn
        {
            DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteHop, string>(r => r.Recv.ToString()) },
            HeaderText = Resources.RECV
        });
        _tracerouteGridView.Columns.Add(new GridColumn
        {
            DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteHop, string>(r => r.Last.ToString()) },
            HeaderText = Resources.LAST
        });

        _tracerouteGridView.Columns.Add(new GridColumn
        {
            DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteHop, string>(r => r.Worst.ToString()) },
            HeaderText = Resources.WORST
        });

        _tracerouteGridView.Columns.Add(new GridColumn
        {
            DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteHop, string>(r => r.Best.ToString()) },
            HeaderText = Resources.BEST
        });

        _tracerouteGridView.Columns.Add(new GridColumn
        {
            DataCell = new TextBoxCell
                { Binding = Binding.Property<TracerouteHop, string>(r => r.Average.ToString("0.##")) },
            HeaderText = Resources.AVRG
        });

        /* TODO
        tracerouteGridView.Columns.Add(new GridColumn
        {
            DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteHop, string>(r => "TODO") },
            HeaderText = Resources.HISTORY
        }); */
        _tracerouteGridView.Columns.Add(new GridColumn
        {
            DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteHop, string>(r => r.As) },
            HeaderText = "AS"
        });
        _tracerouteGridView.Columns.Add(new GridColumn
        {
            DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteHop, string>(r => r.Hostname) },
            HeaderText = Resources.HOSTNAME
        });
        DrawTipColor();
    }
    
    // 设置延迟颜色提示
    private void DrawTipColor()
    {
        _tracerouteGridView.CellFormatting += (sender, e) =>
        {
            if (_tracerouteResultCollection.Count > 0 && e.Row >= 0 && e.Row < _tracerouteResultCollection.Count)
            {
                var tracerouteHop = _tracerouteResultCollection[e.Row];  // 获取当前行的数据对象
                // 获取当前行的最后一跳的延迟
                double lastValue = tracerouteHop.Last;
                // 根据最后一跳的延迟值决定背景颜色
                if (e.Column.HeaderText == Resources.HOP)
                {
                    if (lastValue > 0 && lastValue <= UserSettings.YellowSpeed)
                    {
                        e.BackgroundColor = Color.Parse("#a5d486");
                    }
                    else if (lastValue > UserSettings.YellowSpeed && lastValue <= UserSettings.RedSpeed)
                    {
                        e.BackgroundColor = Color.Parse("#ffcc63");
                    }
                    else
                    {
                        e.BackgroundColor = Color.Parse("#fb6351");
                    }
                }
            }
        };
    }

    // 设置地图来源
    private void SetMap()
    {
        switch (UserSettings.MapProvider)
        {
            // 百度地图
            case "baidu":
                _mapWebView.Url = new Uri($"file://{Path.GetFullPath("baidumap.html")}");
                break;
            // 高德地图
            case "amap":
                _mapWebView.Url = new Uri($"file://{Path.GetFullPath("amap.html")}");
                break;
        }
    }
    
}