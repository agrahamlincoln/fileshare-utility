using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fileshare_utility
{
    class FileLocations
    {
        private const string DATA_SUBDIRECTORY = "data";            // Stores Data
        private const string LOG_SUBDIRECTORY = "data";            // Stores Logs
        private const string DB_FILENAME = "fileshares.s3db";       // Database Filename

        private string _RunningPath;
        public string runningPath 
        {
            get { return _RunningPath; }
            private set { _RunningPath = value; }
        }             // Path of the Executed Application

        private string _LogDir;
        public string logDir
        {
            get { return _LogDir; }
            private set {
                _LogDir = value;
                CreateSubdirectoryIfNoExists(LOG_SUBDIRECTORY); 
            }
        }                          // Directory that stores the log

        private string _DBDir;
        public string dbDir
        {
            get { return _DBDir; }
            private set {
                _DBDir = value;
                CreateSubdirectoryIfNoExists(DATA_SUBDIRECTORY); 
            }
        }                           // Directory that stores the database
        public string dbPath 
        { 
            get; 
            private set; 
        }                          // File Path of the database file

        public FileLocations()
        {
            this.runningPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            this.logDir = runningPath + "\\" + DATA_SUBDIRECTORY;
            this.dbDir = runningPath + "\\" + DATA_SUBDIRECTORY;
            this.dbPath = dbDir + "\\" + DB_FILENAME;
        }

        public void initializeAppDomain()
        {
            AppDomain.CurrentDomain.SetData("DataDirectory", runningPath);
        }

        /// <summary>
        /// Creates a Subdirectory under the running program if none exists
        /// </summary>
        /// <param name="name">name of the subdirectory to create</param>
        public void CreateSubdirectoryIfNoExists(string name)
        {
            if (!Directory.Exists(runningPath + "\\" + name))
                Directory.CreateDirectory(runningPath + "\\" + name);
        }
    }
}
