using Advexp;

namespace VisualTrace;

internal class UserSettings : Settings<UserSettings>
{
    /* 主界面设置 */
    // 历史traceroute记录
    [Setting(Name = "TraceHistory", Default = "")]
    public static string TraceHistory { get; set; } = "";
    
    // 协议选择
    [Setting(Name = "SelectedProtocol", Default = "")]
    public static string SelectedProtocol { get; set; } = "";
    
    // 数据提供方选择
    [Setting(Name = "SelectedDataProvider", Default = "")]
    public static string SelectedDataProvider { get; set; } = "";
    
    // dns方式选择
    [Setting(Name = "SelectedDnsResolver", Default = "system")]
    public static string SelectedDnsResolver { get; set; } = "";
    
    
    /* 常规设置 */
    // 地图提供方
    [Setting(Name = "MapProvider", Default = "amap")]
    public static string MapProvider { get; set; } = "";
    
    // 自定义dns解析
    [Setting(Name = "CustomDnsResolvers", Default = "8.8.8.8#Google DNS\n")]
    public static string CustomDnsResolvers { get; set; } = "";
    
    // gridview尺寸表格高度占比
    [Setting(Name = "GridSizePercentage", Default = 0.4)]
    public static double GridSizePercentage { get; set; }
    
    // 黄色延迟时间
    [Setting(Name = "YellowSpeed", Default = 100)]
    public static double YellowSpeed { get; set; }
    
    // 红色延迟时间
    [Setting(Name = "RedSpeed", Default = 200)]
    public static double RedSpeed { get; set; }
    
    // 合并地理组织
    [Setting(Name = "CombineGeoOrg", Default = false)]
    public static bool CombineGeoOrg { get; set; }
    
    // 保留延迟显示到整数位
    [Setting(Name = "TimeRounding", Default = false)]
    public static bool TimeRounding { get; set; }
    
    // 隐藏地图上的信息窗口
    [Setting(Name = "HideMapPopup", Default = false)]
    public static bool HideMapPopup { get; set; }
    
    //  添加ICMP防火墙放行规则 选择框
    [Setting(Name = "HideAddIcmpFirewallRule", Default = false)]
    public static bool HideAddIcmpFirewallRule { get; set; }
    
    
    /* 路由跟踪设置 */
    // 每跃点探测次数
    [Setting(Name = "Queries", Default = "")]
    public static string Queries { get; set; } = "";

    // 目标端口
    [Setting(Name = "Port", Default = "")]
    public static string Port { get; set; } = "";

    // 并行请求数
    [Setting(Name = "ParallelRequest", Default = "")]
    public static string ParallelRequest { get; set; } = "";

    // 最大跃点
    [Setting(Name = "MaxHops", Default = "")]
    public static string MaxHops { get; set; } = "";

    // 首个跃点
    [Setting(Name = "First", Default = "")]
    public static string First { get; set; } = "";

    // 请求间隔
    [Setting(Name = "SendTime", Default = "")]
    public static string SendTime { get; set; } = "";

    // 分组请求间隔
    [Setting(Name = "TtlTime", Default = "")]
    public static string TtlTime { get; set; } = "";

    // 源地址
    [Setting(Name = "Source", Default = "")]
    public static string Source { get; set; } = "";

    // 源接口
    [Setting(Name = "Dev", Default = "")]
    public static string Dev { get; set; } = "";
    
    // rdns模式
    [Setting(Name = "RdnsMode", Default = "default")]
    public static string RdnsMode { get; set; } = "";
}