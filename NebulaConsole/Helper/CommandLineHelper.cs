using System.Collections.Generic;
using System.Linq;


namespace NebulaConsole.Helper
{
    class CommandLineHelper
    {
        // Convert Argument in dictionary as key : value 
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public static List<string> ValidArgs = new List<string> { "/d:", "/a:","/t:","/s:","/u:","/o:","/c:" };

        public static bool ValidateArgs(string[] cmdArgs)
        {
            var cmdList = cmdArgs.ToList();
            cmdList.RemoveAt(0);

            if ( cmdArgs.Count() > 5 || cmdArgs.Count() < 1 )
                 return false;
            
            int invalidArgCount = 0;

            //cmdList.Add(@"/d:H:\test");
            //cmdList.Add("/a:lock");
            //cmdList.Add("/c");
            //cmdList.Add("/o:");
            //cmdList.Add("/t:");

            foreach (var arg in cmdList)
            {
                var newArg = (arg.Length == 2) ? arg + ":" : arg; 

                var startArg = newArg.Substring(0, 3);

                Logger.Info("Start Argument : {0}",startArg);
                if (!ValidArgs.Contains(startArg))
                {
                    Logger.Error(string.Format("Invalid Argument : [{0}]", arg));
                    invalidArgCount++;
                }
            }

            if (invalidArgCount > 0)
                return false;

            var cmdArguments = new CommandArgs(cmdList);

            if ( string.IsNullOrEmpty(CommandArgs.WDirectory) && string.IsNullOrEmpty(CommandArgs.UseFromCache) )
            {
                Logger.Error(string.Format("Directory to lock/unlock or value from cache not specified"));
                return false; 
            }

            if ( string.IsNullOrEmpty(CommandArgs.UAction))
            {
                Logger.Error("User must specify proper action. Valid choices are [Lock/Unlock]");
                return false; 
            }
            
            if ( !string.IsNullOrEmpty(CommandArgs.WDirectory) && !string.IsNullOrEmpty(CommandArgs.UseFromCache))
            {
                Logger.Error(string.Format("Directory to be actioned and Use from cache both flags set"));
                return false; 
            }

            return true; 
        }

        public static bool IfAskedForHelp(string[] cmdArgs)
        {
            return cmdArgs.Any(arg => arg.Equals("/h") || arg.Equals("/help"));
        }
    }
}
