using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace Nebula.Common
{
    public class Helper
    {
        // Logger
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        readonly Hashtable _extMapping = new Hashtable(5);
        public Dictionary<string, string> FailedFiles = new Dictionary<string, string>();
        private readonly bool _isThreaded;
        private bool _zerothTreadCreated = false;

        //private static bool _traceEnabled; 
        static volatile int _pCnt = 0 ;
        private int _allFileCnt = 0;

        public int ThreadProcessingCompleted = 0;
        public int ThreadsCreated = 0; 
        
        static Thread _thread0 ;
        static Thread _thread1 ;
        static Thread _thread2 ;
        static Thread _thread3 ;
        static Thread _thread4 ;
        static Thread _thread5 ;
        static Thread _thread6 ;
        static Thread _thread7 ;

        List<int> _finishedThreadList = new List<int>(8);
        
        public Helper(bool isThreaded)
        {
            Logger.Info("Called with the constructor {0}",isThreaded);
            _isThreaded = isThreaded;
            _allFileCnt = 0;
            _pCnt = 0; 
            Logger.Info("Resetting _pCnt : {0} & _allFileCount : {1}",_pCnt,_allFileCnt);
        }

        public Helper()
        {
            //_traceEnabled = traceEnabled != 0;
        }

        public void SetFileCountList(int cnt)
        {
            Logger.Info("Accessing private variable, setting all file count to be {0}",cnt);
            _allFileCnt = cnt; 
        }
        /// <summary>
        /// Used by progress bar to calculate the progress!!
        /// </summary>
        /// <returns>returns processed files count</returns>
        public int UpdateProgressBar()
        {
            Logger.Info("Caluclate completed percentage : Processed files {0} / All Files {1}",_pCnt,_allFileCnt);
            if (_pCnt <= _allFileCnt)
            {
                double percentage = Math.Round(((double) _pCnt/(double) _allFileCnt)*100);
                Logger.Info(" ( {0} / {1} ) * 100 = {2}", _pCnt, _allFileCnt, percentage);
                return (int) percentage;
            }
            return 100;
        }

        /// <summary>
        ///  Initialize the variables on request
        /// </summary>
        private void Init()
        {
            _extMapping.Add(".jpg", ".nylj");
            _extMapping.Add(".jpeg", ".nylj");
            _extMapping.Add(".bmp", ".nybj"); 
            _extMapping.Add(".nylj", ".jpg"); 
            _extMapping.Add(".nylb", ".bmp");
        }

        /// <summary>
        ///   Get the list of files with specified extension from the specified directory 
        /// </summary>
        /// <param name="fname">Folder Name where to find below files</param>
        /// <param name="extList">Array of extensions to search</param>
        /// <returns>files with specified extension</returns>
        public List<string> GetImages(string fname, string[] extList)
        {
            var list = new List<string>();
            Logger.Info("Searching the directory!!");

            if (Directory.Exists(fname) == false)
            {
                ThrowError("Directory [" + fname + "] does not exist!!\n --> Please select the directory first!!");
            }
            else
            {
                // If exists find all the image files ...

                foreach (var pat in extList)
                {
                    string[] tlist = { };
                    tlist = Directory.GetFiles(fname, pat, SearchOption.AllDirectories);
                    list.AddRange(tlist);
                }
            }
            Logger.Info("Directory list completed, found {0} files",list.Count);
            return list; 
        }

        /// <summary>
        ///     Throw Error Message 
        /// </summary>
        /// <param name="errMsg">Error Message string</param>
        public void ThrowError(string errMsg)
        {
            if (errMsg != null)
            {
                MessageBox.Show(errMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        ///    Create bunch of files from all the list
        /// </summary>
        /// <param name="array">Entire List of files</param>
        /// <param name="startCnt">Starting index</param>
        /// <param name="step">size of bunch, to create from the index!!</param>
        /// <returns></returns>
        public List<string> CreateFileBunchList(List<string> array, int startCnt, int step)
        {
            var subSetArray = new List<string>(array).GetRange(startCnt, step);
            return subSetArray;
        }

        /// <summary>
        ///    Detemine the chunk size, decided by the no. of CPU. Max there can be 8 cores 
        /// </summary>
        /// <param name="size">File COunt</param>
        /// <returns>chunk size</returns>
        public int SetChunkSize(int size)
        {
            var procCnt = Environment.ProcessorCount;

            procCnt = procCnt > 8 ? 8 : procCnt; // Set the maximum thread count to 8 if CPU cores are more than 8 

            var chunkSize = (size / procCnt);
            Logger.Info("Maximum files that can be processed by a single thread {0}",chunkSize);
            return chunkSize;
        }

        /// <summary>
        ///   Rename files to new name depeding upon the action 
        /// </summary>
        /// <param name="fileList">file list with the absolute path</param>
        /// <param name="maxLoopCnt"> </param>
        /// <returns>sucess / failure</returns>
        public void MoveFiles(List<string> fileList, int maxLoopCnt)
        {
           
            foreach (var name in fileList)
            {
                var fobj = new FileInfo(name);
                var fext = fobj.Extension;

                string newName;
                if (_extMapping.ContainsKey(fext))
                    newName = Regex.Replace(name, fext, (string)_extMapping[fext]);
                else { continue; }
                
                try
                {
                    // Thread.Sleep(30);
                    File.Move(name, newName);
                    // successCnt++;
                    _pCnt++; 

                }
                catch (IOException ex)
                {
                    // ThrowError("Failed to move file ..." + ex.Message);
                    FailedFiles.Add(name,ex.Message);
                    _pCnt++;
                }
            }
        }

        /// <summary>
        ///  Do the actual work of locking or unlocking the files depending upon the 
        ///  action specified
        /// </summary>
        /// <param name="flist">list of files to be locked/unlocked</param>
        /// <returns>
        ///      0 => Everything Worked well.
        ///      1 => No files found in selected folder 
        ///      2 => File Move Failed!!
        ///      
        /// </returns>
        public object DoWork(object flist) 
        {
            // Application.Run(new SplashProgress());
            
            Init();
            // bool isNoThreading = false;

            var newList = (List<string>) flist; 
            if (newList.Count == 0) { return 1; }
            var fileCnt = newList.Count;

            int chunkSize;
            int maxLoopCnt;
            
            if (fileCnt > 50 && _isThreaded) // there is no point in doing threding for 50 files ...
            {
                // MessageBox.Show("Check the Thread var " + _isThreaded);
                chunkSize = this.SetChunkSize(fileCnt);
                maxLoopCnt = (fileCnt/chunkSize);

                int localLoopCnt = maxLoopCnt;
                var i = 0;
                List<string> test;


                while (maxLoopCnt != 0)
                {
                    test = this.CreateFileBunchList(newList, i, chunkSize);
                    i += chunkSize;
                    // ProcessedFCnt.Add(maxLoopCnt,0);
                    StartFileMoveThread(test, maxLoopCnt);
                    maxLoopCnt--;
                }

                if (maxLoopCnt == 0 && (fileCnt%chunkSize) != 0) // do something for remaining files & if there are more files 
                {
                    test = this.CreateFileBunchList(newList, i, (fileCnt - i));
                    // ProcessedFCnt.Add(maxLoopCnt, 0);
                    StartFileMoveThread(test, maxLoopCnt);
                }

            }
            else
            {
                chunkSize = fileCnt;
                maxLoopCnt = 1;
                StartFileMoveThread(newList, 0);
                // _thread0 = new Thread(f => MoveFiles(newList, maxLoopCnt)); 
                // _thread0.Start();
            }

            return ThreadsCreated;
            // return FailedFiles; // Return the object of failed files...
        }

        public int CheckCompletedThreadCnt()
        {
            if (_isThreaded)
            {
                for (var j = ThreadsCreated; j >= 0; j--)
                {
                    if (!_finishedThreadList.Contains(j)) // If current thread is not finished then only check the status!!
                    {
                        try
                        {
                            if (!IsFileMoveThread(j))
                            {
                                _finishedThreadList.Add(j);
                                ThreadProcessingCompleted++;
                            }
                        }
                        catch (Exception)
                        {
                            Logger.Error("Trying to check status of thread which was never created!!! [{0}]", j);
                        }
                    }
                }
            }
            else
            {
                if (!_thread0.IsAlive) ThreadProcessingCompleted++;
            }

            return ThreadProcessingCompleted;
        }

        private bool IsFileMoveThread(int j)
        {
            bool stat = false;
            Logger.Info("Checking if THREAD-{0} is alive?", j);
            switch (j)
            {
                case 0:
                    if (_zerothTreadCreated)
                    {
                        stat = _thread0.IsAlive ? true : false;
                    } else
                    {
                        stat = true;
                    }
                    break;
                case 1:
                    stat = _thread1.IsAlive ? true : false;
                    break;
                case 2:
                    stat = _thread2.IsAlive ? true : false;
                    break;
                case 3:
                    stat = _thread3.IsAlive ? true : false;
                    break;
                case 4:
                    stat = _thread4.IsAlive ? true : false;
                    break;
                case 5:
                    stat = _thread5.IsAlive ? true : false;
                    break;
                case 6:
                    stat = _thread6.IsAlive ? true : false;
                    break;
                case 7:
                    stat = _thread7.IsAlive ? true : false;
                    break;
            }
            Logger.Info("Is THREAD-{0} running? {1}", j,stat);
            return stat; 
        }

        private void StartFileMoveThread(List<string> test, int maxLoopCnt)
        {
            ThreadsCreated++;
            Logger.Info("Created file move thread #{0}",ThreadsCreated);
            Logger.Info("Going to start thread-{0}",maxLoopCnt);
            switch (maxLoopCnt)
            {
                case 0 :
                         _thread0 = new Thread(f => this.MoveFiles(test,maxLoopCnt)); 
                         _thread0.Start();
                         _zerothTreadCreated = true;
                         break; 
                case 1:
                         _thread1 = new Thread(f => this.MoveFiles(test, maxLoopCnt)); 
                         _thread1.Start();
                         break; 
                case 2:
                         _thread2 = new Thread(f => MoveFiles(test, maxLoopCnt));
                         _thread2.Start();
                         break; 
                case 3:
                         _thread3 = new Thread(f => MoveFiles(test, maxLoopCnt));
                         _thread3.Start();
                         break; 
                case 4:
                         _thread4 = new Thread(f => MoveFiles(test, maxLoopCnt));
                         _thread4.Start();
                         break; 
                case 5:
                         _thread5 = new Thread(f => MoveFiles(test, maxLoopCnt));
                         _thread5.Start();
                         break; 
                case 6:
                         _thread6 = new Thread(f => MoveFiles(test, maxLoopCnt));
                         _thread6.Start();
                         break; 
                case 7:
                         _thread7 = new Thread(f => MoveFiles(test, maxLoopCnt));
                         _thread7.Start();
                         break; 
            }
        }

        public object GetFailedFiles()
        {
            //LogMe(LogLevels.Info, "Returning failed file list...");
            Logger.Info("Returning failed file list...");
            return FailedFiles; 
        }

        //public static void LogMe(LogLevels level,string msg)
        //{
            
        //    if ( level == LogLevels.Info && _traceEnabled )
        //    {
        //        logger.Trace(msg);
        //        return; 
        //    }

        //    switch (level)
        //    {
        //         case LogLevels.Debug: 
        //             if ( logger.IsDebugEnabled ) logger.Debug(msg);
        //             break;
        //         case LogLevels.Trace: 
        //             if ( logger.IsTraceEnabled ) logger.Trace(msg);
        //             break;
        //        case LogLevels.Info:
        //            if ( logger.IsInfoEnabled) logger.Info(msg);
        //            break;
        //        case LogLevels.Warn:
        //            if ( logger.IsWarnEnabled) logger.Warn(msg);
        //            break;  
        //        case LogLevels.Error:
        //            if ( logger.IsErrorEnabled) logger.Error(msg);
        //            break; 
        //        case LogLevels.Fatal:
        //            if ( logger.IsFatalEnabled) logger.Fatal(msg);
        //            break;
        //        default:
        //            if ( logger.IsInfoEnabled) logger.Info(msg);
        //            break; 
        //    }
        //}
        
        //// public static void TraceEnabled(bool isTraceEnabled)
        //{
        //    _traceEnabled = isTraceEnabled; 
        //}
    }



}
