using System.Collections.Generic;
using System.IO;

namespace Nebula.Common
{
    class Checkers
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        /// <summary>
        ///    Check if folder exist
        /// </summary>
        /// <param name="fname">folder name</param>
        /// <returns>yes / no</returns>
        public bool IsFolderExists(string fname)
        {
            Logger.Info("Folder name : [{0}]",fname);
            if (fname.Length <= 0)
            {
                return false;
            }
            else
            {
                // Folder is selected, lets check if exists.. i dont know why but just check!! 
                return Directory.Exists(fname) != false;
            }
        }

        /// <summary>
        ///    check if file exists!
        /// </summary>
        /// <param name="fileName">filename</param>
        /// <returns>yes / no</returns>
        public bool IsFileExists(string fileName)
        {
            Logger.Info("Check if File exist : [{0}]", fileName);
            var fInfo = new FileInfo(fileName);
            return fInfo.Exists;
        }

        /// <summary>
        ///    check if list is empty .. can be avoided list.count > 0 ** REDUNDANT **
        /// </summary>
        /// <param name="list">list</param>
        /// <returns>yes / no</returns>
        public bool IsEmpty(List<string> list)
        {
            Logger.Info("Check list to be empty? Size [{0}]",list.Count);
            return list.Count <= 0;
        }
    }
}
