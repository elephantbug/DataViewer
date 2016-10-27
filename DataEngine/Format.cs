using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;

namespace DataEngine
{
    static class Format
    {
        //this would allow things like 999.999.999 which is not a valid (the format is valid, 
        //but not the values) IP address
        public static readonly Regex IpPortRegex = new Regex(@"^(?<Ip>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\:(?<Port>\d+)$");

        public static int Ip2Num(System.Net.IPAddress ip)
        {
            return System.BitConverter.ToInt32(ip.GetAddressBytes(), 0);
        }

        public static string Ip2String(int ip)
        {
            return new IPAddress(BitConverter.GetBytes(ip)).ToString();
        }

        public static string Port2String(short port)
        {
            return ((ushort)port).ToString();
        }

        public static string EndPoint2String(int ip, short port)
        {
            return String.Format("{0}:{1}", Format.Ip2String(ip), Format.Port2String(port));
        }

        public static int TryString2Ip(string str_val)
        {
            System.Net.IPAddress ip;

            if (IPAddress.TryParse(str_val, out ip))
            {
                return System.BitConverter.ToInt32(ip.GetAddressBytes(), 0);
            }

            return 0;
        }

        public static int String2NumIp(string str_val)
        {
            System.Net.IPAddress ip = IPAddress.Parse(str_val);

            return Ip2Num(ip);
        }

        public static void ParseIpPort(string val, out int num_ip, out short port)
        {
            Match m = Format.IpPortRegex.Match(val);

            if (m.Success)
            {
                num_ip = String2NumIp(m.Groups["Ip"].Value);
                
                port = (short)UInt16.Parse(m.Groups["Port"].Value);

                return;
            }

            num_ip = 0;

            port = 0;
        }

        public static string GetExceptionMessage(Exception x)
        {
            return GetExceptionMessage("The following error has occurred:\n{0}", x);
        }

        public static string GetExceptionMessage(string format, Exception x)
        {
            Exception inner = x;

            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            while (inner != null)
            {
                builder.AppendLine(String.Format("  Type: {0}, Message: {1}", inner.GetType().Name, inner.Message));

                inner = inner.InnerException;
            }

            return String.Format(format, builder.ToString());
        }

        public static string FormatTimeSpan(TimeSpan span)
        {
            StringBuilder buf = new StringBuilder();

            if (span.Days != 0)
            {
                buf.Append(span.Days.ToString("00"));
                buf.Append("d:");
            }

            if (span.Hours != 0)
            {
                buf.Append(span.Hours.ToString("00"));
                buf.Append("h:");
            }
            
            if (span.Minutes != 0)
            {
                buf.Append(span.Minutes.ToString("00"));
                buf.Append("m:");
            }
            
            buf.Append(span.Seconds.ToString("00"));
            buf.Append("s");

            return buf.ToString();
        }
    }
}
