using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;

namespace DataEngine
{
    static class Format
    {
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
