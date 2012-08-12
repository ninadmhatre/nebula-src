using System.Collections.Generic;
using System.IO;

namespace Nebula.Common
{
    public class FavoritesFile
    {
        private readonly string _configFile;
        private readonly bool _allowCache;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        /// <summary>
        ///   Constructor for class
        /// </summary>
        /// <param name="configFile"></param>
        /// <param name="superAllowCache">Used decides if to add folder to cache</param>
        public FavoritesFile(string configFile,bool superAllowCache)
        {
            Logger.Info(string.Format("Initializing logger with {0} and cache enabled {1}",configFile,superAllowCache));    
            _configFile = configFile;
            _allowCache = superAllowCache; 
        }

        /// <summary>
        ///   Read the favorite cache, currently reads the .txt file 
        /// </summary>
        /// <returns>returns valid entries from the cache</returns>
        public List<string> ReadFavoritesCache()
        {
            var fcheck = new Checkers();

            var helper = new Helper();
            if (!fcheck.IsFileExists(_configFile))
            {
                // helper.ThrowError(string.Format("{0} file does not exist!!! Read Failed.", _configFile));
                Logger.Info(string.Format("{0} file does not exist!!! creating", _configFile));
                File.Create(_configFile); 
                var _temp = new List<string>() ; 
                return _temp;
            }
            Logger.Info("Going to read config file...");
            return CacheCleanup();
        }

        /// <summary>
        ///    Clens up the cache, verifies all directories exists! and there are no null entries
        /// </summary>
        /// <returns>returns list of favorite list</returns>
        private List<string> CacheCleanup()
        {
            var fReader = File.ReadAllLines(_configFile);
            var cleanedUp = new List<string>();
            var fcheck = new Checkers();

            Logger.Info("Cleaning up the cache before reading, to save loading stale entries");
            foreach (var folder in fReader)
            {
                if (folder != null && fcheck.IsFolderExists(folder)) cleanedUp.Add(folder);
            }
            Logger.Info("Validated cache entries, writing back to cache file");
            if (SaveCache(cleanedUp))
            {
                // Cache update successfully .... 
                Logger.Info("Cache cleaned up successfully!!");
            }
            return cleanedUp;
        }

        /// <summary>
        ///   Update the entire file at once and take backup of older file as _backup 
        /// </summary>
        /// <param name="cache">List of new files to be saves in cache</param>
        /// <returns>success or failure</returns>
        private bool SaveCache(List<string> cache)
        {
            var helper = new Helper();
            if (cache.Count > 0)
            {
                try
                {
                    Logger.Info("Backing up old cache file as '_back'");
                    File.Delete(_configFile + "_back");
                    File.Move(_configFile, _configFile + "_back");
                    File.WriteAllLines(_configFile, cache);
                    return true;
                }
                catch (IOException ex)
                {
                    helper.ThrowError(string.Format("Failed to Save Cache file!! Error : {0}", ex.Message));
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///    Update the cache with given entry 
        /// </summary>
        /// <param name="entry">directory name to be added to cache</param>
        /// <returns>success or failure</returns>
        public bool? UpdateFavoriteCache(string entry)
        {
            // Check if entry exist
            // if not then only add at the top 
            // delete the last entry 
            // return 1 on Success or existing entry 

            var fcheck = new Checkers();
            var helper = new Helper();

            if (!fcheck.IsFileExists(_configFile))
            {
                helper.ThrowError(string.Format("{0} file does not exist!!! Update Failed", _configFile));
                return null;
            }

            
            Logger.Info("Cleaning cache before writing new entries...");
            var cacheContents = ReadFavoritesCache();

            if (cacheContents.Contains(entry))
            {
                Logger.Info("'{0}' entry already exists, check if its supposed to be deleted!!",entry);
                if (_allowCache)
                {
                    Logger.Info("Entry is supposed to be cached, not updating cache with duplicate!!!");
                    return true;
                }

                Logger.Info("Entry is NOT supposed to be cached, Removing entry and updating cache!!");
                cacheContents.RemoveAt(cacheContents.IndexOf(entry));
                try
                {
                    SaveCache(cacheContents);
                    return true;
                }
                catch (IOException ex)
                {
                    helper.ThrowError(string.Format("Failed to Update {0} file, {1}", _configFile, ex.Message));
                    return false;
                }
            }

            if (_allowCache) // Allowed to cache the file name 
            {
                Logger.Info("{0} is a new entry, going to cache!!",entry);
                try
                {
                    cacheContents.Insert(0, entry); // Adding at the top 
                    // if count is greater than 10 then delete all the entries after 10 - till end 
                    if (cacheContents.Count > 10) cacheContents.RemoveRange(10, (cacheContents.Count - 1));
                    SaveCache(cacheContents);
                    Logger.Info("Cache updated with the new entry");
                    return true;
                }
                catch (IOException ex)
                {
                    helper.ThrowError(string.Format("Failed to Update {0} file, {1}", _configFile, ex.Message));
                    return false;
                }
            } else
            {
                Logger.Info("Caching is not selected by user, hence not updating cache!!!");
                return true;
            }
        }

        // public static void logMe(string msg,)
    }
}
