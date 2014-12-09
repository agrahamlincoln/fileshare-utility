using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fileshare_utility
{
    /// <summary>The LogWriter class will write to a log file. This log file is by default located in the user's AppData/Roaming Folder.
    /// </summary>
    public class LogWriter
    {
        // Class Variables
        public string logPath { get; set; }             // Path of the log file
        public string fileName { get; set; }            // Filename of the log file
        public string assemblyVersion { get; set; }     // Assembly Version of the current running executable

        #region Constructors
        /// <summary>No-Arg Constructor
        /// </summary>
        /// <remarks>Will default file path to user's AppData/Roaming folder</remarks>
        public LogWriter() : this(getAppDataPath()) { }

        /// <summary>Single Argument Constructor
        /// </summary>
        /// <param name="filepath">Path of the log file</param>
        public LogWriter(string filepath)
        {
            this.logPath = filepath;
            this.fileName = getProcessName() + "_log.txt";
            this.assemblyVersion = getVersion();
        }

        /// <summary>Two-Argument Constructor
        /// </summary>
        /// <param name="filepath">Path of the log file</param>
        /// <param name="fileName">Name of the log file</param>
        public LogWriter(string filepath, string fileName)
        {
            this.logPath = filepath;
            this.fileName = fileName;
            this.assemblyVersion = getVersion();
        }
        #endregion

        #region Information Gathering
        /// <summary>Gets the file path in the current user's AppData/Roaming folder. The filename will be the current process name by default.
        /// </summary>
        /// <returns>string file path in user's AppData/Roaming folder</returns>
        private static string getAppDataPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        /// <summary>Gets the current process name
        /// </summary>
        /// <returns>Process Name for this application</returns>
        private static string getProcessName()
        {
            return System.Diagnostics.Process.GetCurrentProcess().ProcessName;
        }

        /// <summary>Returns the current assembly version
        /// </summary>
        /// <returns>String version number of the current assembly</returns>
        private string getVersion()
        {
            return typeof(LogWriter).Assembly.GetName().Version.ToString();
        }

        #endregion

        #region Log writing
        const string TIMESTAMP_FORMAT = "MM/dd HH:mm:ss";

        /// <summary>Returns a timestamp in string format.
        /// </summary>
        /// <remarks>Uses the Timestamp Format defined in the Constants of this class.</remarks>
        /// <param name="value">DateTime to change into string</param>
        /// <returns>Timestamp in string format.</returns>
        private static string getTimestamp(DateTime value)
        {
            return value.ToString(TIMESTAMP_FORMAT);
        }

        /// <summary>Public method to access the writelog method
        /// </summary>
        /// <param name="message">Message to write to log</param>
        /// <param name="print">Whether to write or not</param>
        public void Write(string message, bool print)
        {
            if (print)
            {
                oWrite(message);
            }
        }

        /// <summary>Public method to access the writelog method
        /// </summary>
        /// <param name="message">Message to write to log</param>
        /// <param name="print">Whether to write or not</param>
        public void Write(string message)
        {
            oWrite(message);
        }

        /// <summary>Appends a new message to the end of the log file.
        /// </summary>
        /// <remarks>If there is no log file available, this will create one. The log files are stored in the current user's Appdata/Roaming folder.</remarks>
        /// <param name="message"></param>
        private void oWrite(string message)
        {
            string logLocation = logPath + "\\" + fileName;

            if (!System.IO.File.Exists(logLocation))
            {
                using (StreamWriter sw = System.IO.File.CreateText(logLocation))
                {
                    sw.WriteLine(getTimestamp(DateTime.Now) + message);
                }
            }
            else
            {
                StreamWriter sWriter = new StreamWriter(logLocation, true);
                sWriter.WriteLine(getTimestamp(DateTime.Now) + "\t" + message);

                sWriter.Close();
            }
        }

        /// <summary> Creates a string header with program name and version. Typically used at the beginning of the program.
        /// </summary>
        /// <returns>string program header for log file.</returns>
        public string header()
        {
            return "Logger Version: v" + assemblyVersion;
        }
        #endregion
    }
}
