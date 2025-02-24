using System.Collections.ObjectModel;


namespace VisualTrace;

internal class TracerouteHop
{
    public ObservableCollection<TracerouteResult> HopData { get; set; }

    public TracerouteHop(TracerouteResult hopData)
    {
        HopData = new ObservableCollection<TracerouteResult>();
        HopData.Add(hopData);
    }

    public string Hop => HopData[0].Hop;

    public string Ip
    {
        get
        {
            List<string> uniqueIPs = new List<string>();
            foreach (var hop in HopData)
                if (!uniqueIPs.Contains(hop.Ip) && hop.Ip != "*")
                    uniqueIPs.Add(hop.Ip);
            if (uniqueIPs.Count == 0) uniqueIPs.Add("*");
            return string.Join(Environment.NewLine, uniqueIPs);
        }
    }

    public string Time
    {
        get
        {
            if (UserSettings.TimeRounding)
                return string.Join(" / ",
                    HopData.Select(d => d.Time == "*" ? d.Time : Math.Round(Convert.ToDouble(d.Time)).ToString()));
            else
                return string.Join(" / ", HopData.Select(d => d.Time));
        }
    }

    public string Geolocation
    {
        get
        {
            List<string> uniqueGeo = new();
            foreach (var hop in HopData)
                if (!uniqueGeo.Contains(hop.Geolocation) && hop.Ip != "*")
                    uniqueGeo.Add(hop.Geolocation);
            return string.Join(Environment.NewLine, uniqueGeo);
        }
    }

    public string Organization
    {
        get
        {
            List<string> uniqueOrg = new();
            foreach (var hop in HopData)
                if (!uniqueOrg.Contains(hop.Organization) && hop.Ip != "*")
                    uniqueOrg.Add(hop.Organization);
            return string.Join(Environment.NewLine, uniqueOrg);
        }
    }

    public string GeolocationAndOrganization
    {
        get
        {
            List<string> uniqueGeoAndOrg = new();
            foreach (var hop in HopData)
                if (!uniqueGeoAndOrg.Contains(hop.Geolocation + " " + hop.Organization) && hop.Ip != "*")
                    uniqueGeoAndOrg.Add(hop.Geolocation + " " + hop.Organization);
            return string.Join(Environment.NewLine, uniqueGeoAndOrg);
        }
    }

    public string Hostname
    {
        get
        {
            List<string> uniqueHostname = new();
            foreach (var hop in HopData)
                if (!uniqueHostname.Contains(hop.Hostname) && hop.Hostname != "" && hop.Ip != "*")
                    uniqueHostname.Add(hop.Hostname);
            return string.Join(Environment.NewLine, uniqueHostname);
        }
    }

    public string As
    {
        get
        {
            List<string> uniqueAs = new();
            foreach (var hop in HopData)
                if (!uniqueAs.Contains(hop.As) && hop.As != "" && hop.Ip != "*")
                    uniqueAs.Add(hop.As);
            return string.Join(Environment.NewLine, uniqueAs);
        }
    }
    

    public int Loss
    {
        get
        {
            var count = 0;
            foreach (var hop in HopData)
                if (hop.Ip == "*")
                    count++;
            return (int)((float)count / HopData.Count * 100);
        }
    }

    public int Recv
    {
        get
        {
            var count = 0;
            foreach (var hop in HopData)
                if (hop.Ip != "*")
                    count++;
            return count;
        }
    }

    public int Sent => HopData.Count;

    public double Last
    {
        get
        {
            if (HopData.Count > 0 && HopData[HopData.Count - 1].Ip != "*")
                return double.Parse(HopData[HopData.Count - 1].Time);
            else
                return 0;
        }
    }

    public double Worst
    {
        get
        {
            double worst = 0;
            foreach (var hop in HopData)
                if (hop.Ip != "*" && double.Parse(hop.Time) > worst)
                    worst = double.Parse(hop.Time);
            return worst;
        }
    }

    public double Best
    {
        get
        {
            var best = double.MaxValue;
            foreach (var hop in HopData)
                if (hop.Ip != "*" && double.Parse(hop.Time) < best)
                    best = double.Parse(hop.Time);
            if (best == double.MaxValue) best = 0;
            return best;
        }
    }

    public double Average
    {
        get
        {
            double sum = 0;
            var count = 0;
            foreach (var hop in HopData)
                if (hop.Ip != "*")
                {
                    sum += double.Parse(hop.Time);
                    count++;
                }

            return sum / count;
        }
    }
    
}