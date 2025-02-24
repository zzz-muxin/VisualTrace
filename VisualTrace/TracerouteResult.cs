namespace VisualTrace;

internal class TracerouteResult
{
    public TracerouteResult(string hop, string ip, string time, string geolocation, string asNumber, string hostname, string organization, string latitude, string longitude)
    {
        Hop = hop;
        Ip = ip;
        Time = time;
        Geolocation = geolocation;
        As = asNumber;
        Hostname = hostname;
        Organization = organization;
        Latitude = latitude;
        Longitude = longitude;
    }
    public string Hop { get; set; }
    public string Ip { get; set; }
    public string Time { get; set; }
    public string Geolocation { get; set; }
    public string As { get; set; }
    public string Hostname { get; set; }
    public string Organization { get; set; }
    public string Latitude { get; set; }  // 纬度
    public string Longitude { get; set; }  // 经度
}