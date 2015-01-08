using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fileshare_utility
{
    public abstract class Writer : IWriter
    {
        internal const string TIMESTAMP_FORMAT = "MM/dd HH:mm:ss";
        internal string assemblyVersion;

        internal string username;       // Current Running User
        internal string computername;   // Current Running Computer

        internal string GetAssemblyVersion()
        {
            return typeof(FileWriter).Assembly.GetName().Version.ToString();
        }

        //required by interface
        public string Header()
        {
            return "Logger Version: v" + assemblyVersion;
        }

        public void Output(string message, bool OutputOverride)
        {
            var uname = username ?? "UnknownUser";
            var cname = computername ?? "UnknownComputer";

            if (OutputOverride)
            {
                Output(string.Format("{0,15}@{1,-15} | {2}", uname, cname, message));
            }
        }
        //required by interface
        public abstract void Output(string message);

        internal static string GetTimestamp(DateTime value)
        {
            return value.ToString(TIMESTAMP_FORMAT);
        }
    }
}
