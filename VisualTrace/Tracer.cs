
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace VisualTrace;

// 存储异常输出信息
internal class ExceptionalOutputEventArgs : EventArgs
{
    public bool IsErrorOutput { get; set; }
    public string Output { get; set; }
    public ExceptionalOutputEventArgs(bool isErrorOutput, string output)
    {
        IsErrorOutput = isErrorOutput;
        Output = output;
    }
}

// 存储进程退出的返回码
internal class AppQuitEventArgs : EventArgs
{
    public int ExitCode { get; set; }
    public AppQuitEventArgs(int exitCode)
    {
        ExitCode = exitCode;
    }
}

internal class Tracer
{
    public string TargetHost { get; set; }  // 目标ip
    public int MaxHops { get; set; } = 30;  // 最大跳数
    public int Timeout { get; set; } = 1000;  // 超时时间ms
    public int PacketSize { get; set; } = 32;  // 包数据部分大小
    private List<TracerouteHop> hops = new ();
    private CancellationTokenSource cts = new ();
    public ObservableCollection<TracerouteResult> Output { get; } = new ();

    public void Run(string host, bool MTRMode, string dataProvider, string protocol)
    {
        TargetHost = host;

        // 创建目标IP地址
        IPAddress targetIp = IPAddress.Parse(host);
        if (targetIp == null)
        {
            Console.WriteLine("Unable to resolve target host.");
            return;
        }

        Task.Run(() =>
        {
            Console.WriteLine("Starting traceroute to: " + host);

            // 跟踪每一跳
            for (int ttl = 1; ttl <= MaxHops; ttl++)
            {
                var hopResult = TraceHop(ttl, targetIp);
                if (hopResult != null)
                {
                    // 如果是第一次遇到这个TTL，创建一个新的TracerouteHop实例
                    if (hops.Count < ttl)
                    {
                        hops.Add(new TracerouteHop(hopResult));
                    }
                    else
                    {
                        // 否则只更新已存在的Hop
                        hops[ttl - 1].HopData.Add(hopResult);
                    }
                    Output.Add(hopResult);
                }
                else
                {
                    Console.WriteLine($"{ttl}: Request timed out.");
                }

                // 如果目标地址到达，结束
                if (hopResult != null && hopResult.Ip.Equals(targetIp.ToString()))
                {
                    //Console.WriteLine("Target reached.");
                    break;
                }

                if (cts.Token.IsCancellationRequested)
                {
                    break;
                }
            }
        }, cts.Token);
    }
    
    private TracerouteResult TraceHop(int ttl, IPAddress targetIp)
    {
        try
        {
            string HOP = ttl.ToString();
            string IP = "*";
            string Time = "";
            string Geolocation = "";
            string AS = "";
            string Hostname = "";
            string Organization = "";
            string Latitude = "";
            string Longitude = "";
            
            
            DateTime sendTime = DateTime.MinValue;  // 记录发送的时间
            using (Ping pingSender = new Ping())
            {
                PingOptions options = new PingOptions(ttl, true);  // Ping参数
                string data = new string('a', PacketSize);  // 包数据部分
                byte[] buffer = Encoding.ASCII.GetBytes(data);  // 转码

                sendTime = DateTime.Now;
                PingReply reply = pingSender.Send(targetIp, Timeout, buffer, options);  // 发包
                // 到达目的地和ttl超时两种状态探测出目的ip和路径上节点ip
                if (reply.Status == IPStatus.Success || (reply.Status == IPStatus.TtlExpired && ttl > 0) )
                {
                    IP = reply.Address.ToString();
                    Time = (DateTime.Now - sendTime).TotalMilliseconds.ToString("F2");  // 计算往返时间
                    //Console.WriteLine(ttl + " ip: " + reply.Address + " time: " + reply.RoundtripTime);
                }
                if (reply.Status == IPStatus.TimedOut)
                {
                    
                }
                // 匹配特定网络地址
                if (new Regex(@"^((127\.)|(192\.168\.)|(10\.)|(172\.1[6-9]\.)|(172\.2[0-9]\.)|(172\.3[0-1]\.)|(::1$)|([fF][cCdD]))").IsMatch(IP))
                {
                    Geolocation = Resources.PRIVATE_ADDR;  // 私有地址（局域网）
                }
                if (new Regex(@"^((100\.6[4-9]\.)|(100\.[7-9][0-9]\.)|(100\.1[0-1][0-9]\.)|(100\.12[0-7]\.))").IsMatch(IP))
                {
                    Geolocation = Resources.SHARED_ADDR;  // 共享地址
                }
                if (new Regex(@"^169\.254\.").IsMatch(IP))
                {
                    Geolocation = Resources.LINKLOCAL_ADDR;  // 链路本地地址
                }
                if (new Regex(@"^127\.").IsMatch(IP))
                {
                    Geolocation = Resources.LOOPBACK_ADDR;  // 本地回环地址
                }
                
                return new TracerouteResult(HOP, IP, Time, Geolocation, AS, Hostname, Organization, Latitude, Longitude);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during ping: {ex.Message}");
            return null;
        }
    }
    
    
    public void Stop()
    {
        cts.Cancel();
    }
    // private Process _process;  // Traceroute进程
    //
    // public AppStatus Status { get; set; } = AppStatus.Init;  // 当前应用状态
    // public event EventHandler AppStart;  // 应用启动事件
    // public event EventHandler<AppQuitEventArgs> AppQuit;  // 应用退出事件，携带退出码信息
    // public event EventHandler<ExceptionalOutputEventArgs> ExceptionalOutput;  // 异常输出事件
    // private int errorOutputCount = 0;  // 跟踪错误输出的数量，超过一定数量时终止进程
    // // 存储traceroute结果
    // public ObservableCollection<TracerouteResult> Output { get; } = new ObservableCollection<TracerouteResult>();
    //
    // public void Run(string host, bool MTRMode, params string[] extraArgs)
    // {
    //     
    // }
    //
    // public void Kill()
    // {
    //     try
    //     {
    //         if (_process != null && !_process.HasExited)
    //             _process.Kill();
    //     }
    //     catch (Exception ex)
    //     {
    //         Debug.Print(ex.Message);
    //     }
    // }

}

