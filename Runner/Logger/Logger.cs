using System;
using System.IO;

namespace Luca
{
    public class Logger
    {
        // Set a variable to the My Documents path.
        private string logPath =
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
       

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dirPath">Name of the path to Log from Roaming\</param>
        public Logger(string dirPath)
        {
            this.logPath += dirPath;
            CreateFolderIfDoesentExists();
        }

        private void CreateFolderIfDoesentExists()
        {
            Directory.CreateDirectory(logPath);
        }

        /// <summary>
        /// Log an error adding the current data by default
        /// </summary>
        /// <param name="txt"></param>
        public void WriteLog(string txt)
        {
            try
            {
                CreateFolderIfDoesentExists();
                // Tested, it can write on an already opened file.
                // Write the string array to a new file.
                using (StreamWriter outputFile = new StreamWriter(this.logPath + $@"\{DateTime.Today.ToString("dd_MM_yy) ")}Logs.txt", true))
                {
                    outputFile.WriteLine(DateTime.Now + ": " + txt);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error on Log Writing : " + ex.Message);
            }

        }


    }
}
