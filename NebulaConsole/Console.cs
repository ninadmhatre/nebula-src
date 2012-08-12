using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Nebula.Common;
using NebulaConsole.Helper;

/* TODO
 * 1. Remove most of the debug statements to DEBUG for console application
 */
namespace NebulaConsole
{
    class NebulaConsole
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        static readonly string ConfigFile = Path.Combine(Environment.CurrentDirectory, "Config", "Favorite.txt");

        static void Main(string[] args)
        {
            if (CommandLineHelper.IfAskedForHelp(Environment.GetCommandLineArgs())) ShowHelp();

            // If valid args == 1 go ahead otherwise we have a problem
            if (!CommandLineHelper.ValidateArgs(Environment.GetCommandLineArgs()))
            {
                Logger.Error("Argument Validation failed..Exiting");
                ShowHelp();
                Environment.Exit(1);
            }

            Logger.Info("Summary : ");
            Logger.Info("Directory to be locked   : {0}", CommandArgs.WDirectory);
            Logger.Info("User Action              : {0}", CommandArgs.UAction);
            Logger.Info("Cache Directory          : {0}", CommandArgs.CacheEntry);
            Logger.Info("Use Threads              : {0}", CommandArgs.UseThreads);
            Logger.Info("Show Cache               : {0}", CommandArgs.ShowCache);
            Logger.Info("Open Directory           : {0}", CommandArgs.OpenDir);
            Logger.Info("Use directory from cache : {0}", CommandArgs.UseFromCache);

            // Generate Helper Object 
            var nHelper = CommandArgs.UseThreads == "true" ? new Nebula.Common.Helper(true) : new Nebula.Common.Helper(false);

            string[] lockExts = { "*.bmp","*.jpg"};
            string[] unlockExts = { "*.nylj", "*.nylb" };

            var imageList = new List<string>();

            switch (CommandArgs.UAction.ToLower())
            {
                case "lock" :
                    imageList = nHelper.GetImages(CommandArgs.WDirectory, lockExts);
                    break;
                case "unlock" :
                    imageList = nHelper.GetImages(CommandArgs.WDirectory, unlockExts);
                    break;
                default:
                    Logger.Error("Invalid Option....");
                    Environment.Exit(2);
                    break;
            }

            nHelper.SetFileCountList(imageList.Count);

            if ( imageList.Count > 0 )
            {
                var threadCnt = (int)nHelper.DoWork(imageList);
                var threadFinCnt = 0;

                while (threadCnt != threadFinCnt)
                {
                    Logger.Info("Threads created [{0}] Threads Finished [{1}]", threadCnt, threadFinCnt);
                    ProgressBarUpdater(nHelper.UpdateProgressBar());
                    threadFinCnt = nHelper.CheckCompletedThreadCnt();
                    Logger.Info("Sleeping for 1/5 sec before checking the status again");
                    Thread.Sleep(200);
                }

                var failedCnt = (Dictionary<string, string>)nHelper.GetFailedFiles();
                var retCode = failedCnt.Count > 0 ? 2 : 0;

                CheckRCode(retCode, imageList.Count, CommandArgs.UAction);
                var allowCache = CommandArgs.CacheEntry == "true" ? true : false;
                var config = new FavoritesFile(ConfigFile,allowCache ); 
                config.UpdateFavoriteCache(CommandArgs.WDirectory);

                if (CommandArgs.OpenDir == "true")
                    System.Diagnostics.Process.Start("explorer", string.Format("{0}", CommandArgs.WDirectory)); 
            } else
            {
                Logger.Info("There are NO files to be locked/unlocked in '{0}' directory!!",CommandArgs.WDirectory);
                Environment.Exit(0);
            }
        }

        private static void ProgressBarUpdater(int p)
        {
            Logger.Info("{0}% completed...",p);
        }

        private static void CheckRCode(int rc, int fcnt, string action)
        {
            Logger.Info("Check the return code of the {0} operation, rc : {1}, failed count {2}", rc, fcnt, action.ToUpper());
            var result = "";
            var failedCnt = 0;
            Logger.Info("Stopping the timer!!");
            //_sw.Stop();
            //TimeSpan ts = _sw.Elapsed;
            // var timeDuration = string.Format("Process took {0:00}:{1:00}:{2:00} (Min:Sec:MiliSec)", ts.Minutes, ts.Seconds, ts.Milliseconds);
            // Logger.Info(timeDuration);

            switch (rc)
            {
                case 0:
                    result = "Successful";
                    break;
                case 1:
                    result = "No_Files_To_Move";
                    break;
                case 2:
                    result = "Failed_To_Move";
                    break;
            }
            Logger.Info("Operation result : {0}", result);
            //var summary = new Hashtable { { "result", result }, { "failed_cnt", failedCnt.ToString(CultureInfo.InvariantCulture) }, { "time_duration", timeDuration } };
            //Logger.Info("Summary : [{0}]",summary);
        }

        public static void ShowHelp()
            {
            var nl = Environment.NewLine ; 
            var helpMessage = "Sample Application to access Nebula functionality via console" + nl
                            + "Usage   : NebulaConsole.exe /d:<DIR> /a:<ACTION> [/c] [/t] [/sc] [/uc:<CACHE_ID>] [/o]" + nl 
                            + "E.g     : NebulaConsole.exe /d:\"c:\\Test\\Image\\Folder\" /a:lock /sc /o" + nl
                            + "Desc    : " + nl
                            + nl
                            + " /d     : Directory, Directory with Images" + nl
                            + " /a     : Action, Action to perform , lock or unlock" + nl
                            + " /c     : Cache, Cache the file name " + nl
                            + " /t     : Threading, Use Threading " + nl
                            + " /sc    : Show Cache, Show cached file listing" + nl
                            + " /o     : Open the directory once operation is finished" + nl 
                            + " /uc    : Use Cache, Use cache id shown by /sc option as a folder with images" + nl ; 

            Console.WriteLine(helpMessage) ; 
            Environment.Exit(1) ; 
        }
    }
}
