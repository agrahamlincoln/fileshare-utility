using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fileshare_utility
{

    /// <summary>
    /// The LogWriter class will write log entries to a text file. This log file is by default located in the user's AppData/Roaming Folder.
    /// </summary>
    public class FileWriter : Writer
    {

        // Class Variables
        public string filePath { get; set; }                // Path of the log file
        public string fileName { get; set; }                // Filename of the log file

        #region Constructors
        /// <summary>No-Arg Constructor
        /// </summary>
        /// <remarks>Will default file path to user's AppData/Roaming folder</remarks>
        public FileWriter() : this(getAppDataPath(), getProcessName() + "_log.txt") { }

        /// <summary>Single Argument Constructor
        /// </summary>
        /// <param name="filepath">Path of the log file</param>
        public FileWriter(string filepath) : this(filepath, getProcessName() + "_log.txt") { }

        /// <summary>Two-Argument Constructor
        /// </summary>
        /// <param name="filepath">Path of the log file</param>
        /// <param name="fileName">Name of the log file</param>
        public FileWriter(string filepath, string fileName)
        {
            this.filePath = filepath;
            this.fileName = fileName;
            this.assemblyVersion = GetAssemblyVersion();
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

        #endregion

        #region Log writing

        /// <summary>Public method to access the writelog method
        /// </summary>
        /// <param name="message">Message to write to log</param>
        /// <param name="print">Whether to write or not</param>
        public override void Output(string message)
        {
            _Output(message);
        }

        /// <summary>Appends a new message to the end of the log file.
        /// </summary>
        /// <remarks>If there is no log file available, this will create one. The log files are stored in the current user's Appdata/Roaming folder.</remarks>
        /// <param name="message"></param>
        private void _Output(string message)
        {
            string logLocation = filePath + "\\" + fileName;

            if (!System.IO.File.Exists(logLocation))
            {
                using (StreamWriter sw = System.IO.File.CreateText(logLocation))
                {
                    sw.WriteLine(GetTimestamp(DateTime.Now) + message);
                }
            }
            else
            {
                using (StreamWriter sw = new StreamWriter(logLocation, true))
                {
                    sw.WriteLine(GetTimestamp(DateTime.Now) + "\t" + message);

                    sw.Close();
                }
            }
        }
        #endregion
    }
}
