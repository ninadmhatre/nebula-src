using System.Collections.Generic;
using System.Linq;

namespace NebulaConsole.Helper
{
    public class CommandArgs
    {
        private static List<string> _cmdArgs = new List<string>(6);
        private static readonly Dictionary<string,string> CmdValMapping = new Dictionary<string, string>(5);
        // private Dictionary<string,string> _paramMapping = new Dictionary<string, string>(6);

        public CommandArgs(List<string> args)
        {
            _cmdArgs = args;
            SetAll();
        }

        public CommandArgs()
        {
            // SetAll();
        }

        private void SetAll()
        {
            WDirectory   = GetValueFromArgsFor("/d:",false);
            UAction      = GetValueFromArgsFor("/a:",false);
            CacheEntry   = GetValueFromArgsFor("/c",true);
            UseThreads   = GetValueFromArgsFor("/t",true);
            UseFromCache = GetValueFromArgsFor("/u:",false);
            ShowCache    = GetValueFromArgsFor("/s",true);
            OpenDir      = GetValueFromArgsFor("/o",true);
        }

        public static string WDirectory
        {
            get { return CmdValMapping["directory"]; } 
            set { CmdValMapping.Add("directory",value); }
        }

        public static string UAction
        {
            get { return CmdValMapping["action"]; }
            set { CmdValMapping.Add("action", value); }
        }

        public static string CacheEntry
        {
            get { return CmdValMapping["cache"]; }
            set { CmdValMapping.Add("cache", value); }
        }

        public static string UseThreads
        {
            get { return CmdValMapping["threads"]; }
            set { CmdValMapping.Add("threads", value); }
        }

        public static string UseFromCache
        {
            get { return CmdValMapping["usecache"]; }
            set { CmdValMapping.Add("usecache", value); }
        }

        public static string ShowCache
        {
            get { return CmdValMapping["showcache"]; }
            set { CmdValMapping.Add("showcache", value); }
        }

        public static string OpenDir
        {
            get { return CmdValMapping["opendir"]; }
            set { CmdValMapping.Add("opendir", value); }
        }

        private static string GetValueFromArgsFor(string param, bool isFlag)
        {
            if (!isFlag)
            {
                var match = _cmdArgs.FirstOrDefault(s => s.StartsWith(param));

                if (match != null)
                {
                    var value = match.Split(new[] {':'}, 2)[1];
                    return value;
                }
                return string.Empty;
            }
            var boolMatch = _cmdArgs.FirstOrDefault(s => s.StartsWith(param));
            return boolMatch != null ? "true" : "false";
        }
    }
}

