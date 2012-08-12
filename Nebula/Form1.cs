using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using System.Diagnostics;
using Nebula.Common;
using NLog; 


/* TODO : 
 * 1. Create Log -- Next step !! -- Done
 * 3. Activate Progress Bar -- Way back on burner !!!! -- Done
 * 2. Enable Threding -- Done!!!
 * 4. Display results in the Text Box at the bottom with properly formatted text -  Done!! 
 * 6. Follow .Net Standards - Done
 * 6. Remove stupid things used in code - Done!!!
 * 7. Add better Summary report!! - Done
 * 8. Operation based on the current functionality that can be performed - Done.
 * 9. Add Favorites -- Done 
 * 10. Too much of code in 1 file, segregate the code -- Done
*/


namespace Nebula
{
    public partial class Nebula : Form
    {
        public string FolderName { get; set; }
        public static string[] LockExts = { Properties.Resources.Jpg, Properties.Resources.Bmp };
        public static string[] UnlockExts = { Properties.Resources.Nylj, Properties.Resources.Nylb };
        public string[] Extenstion = {}; 
        readonly string _configFile = Path.Combine(Application.StartupPath,"Config", Properties.Resources.FAVS_XML);
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private bool _allowCache = true ;

        List<string> _fileList = new List<string>();
        readonly Stopwatch _sw = new Stopwatch();

        public Nebula()
        {
            InitializeComponent();
            Logger.Info("Starting Application"); 
        }


        /// <summary>
        ///    Functionality to do once window is loaded 
        ///    1. Set current directory as the selected directory 
        ///    2. If there is any list then, populate fav list
        /// </summary>
        private void NebulaLoad(object sender, EventArgs e) // Load the current directory in the test box...
        {
            SetDirectory(Environment.CurrentDirectory);
            OpenOPFolder.Enabled = false;
            PopulateFavList();
            Logger.Info("Loading Application UI");
        }

        /// <summary>
        ///     Show folder browse button 
        /// </summary>
        private void BrowseBtnClick(object sender, EventArgs e) // Browse Button 
        {
            Logger.Info("Select Directory to be actioned");
            folderBrowserDialog1.ShowDialog();
            if (!string.IsNullOrEmpty(folderBrowserDialog1.SelectedPath))
            {
                SetDirectory(folderBrowserDialog1.SelectedPath);
            }
            Logger.Info("Selected directory [{0}]",FolderName);
            InitFunctionality(); 
        }

        /// <summary>
        ///    Do the locking functionality on the selected folder
        /// </summary>
        private void LockBtnClick(object sender, EventArgs e) // Lock Button
        {
            Logger.Info("Locking the {0} directory!",FolderName);
            Extenstion = LockExts;
            // label4.Text = "Locking ...";
            CommonAction("Lock");
        }

        /// <summary>
        ///   Unlocking functionality .. Its a repeat code merge functionality of lock and unlock into 1 
        /// </summary>
        private void UnlockBtnClick(object sender, EventArgs e) // Unlock Button
        {
           Logger.Info("Unlocking the {0} directory!", FolderName);
           Extenstion = UnlockExts;
           // label4.Text = "Unlocking ...";
           CommonAction("Unlock");
        }

        /// <summary>
        ///      Do the action as suggested by action, locker & unlocker will set the extensions to use 
        ///      this method will perform the actions
        /// </summary>
        /// <param name="action">Lock / Unlock</param>
        private void CommonAction(string action)
        {
            Logger.Info("   "); 
            Logger.Info("Performing the actual operation of {0}",action);
            bool isThreaded = checkBox1.Checked;
            Logger.Info("Is Threading enabled {0}",isThreaded);

            _allowCache = !checkBox2.Checked;
            var helper = new Helper(isThreaded);
            var config = new FavoritesFile(_configFile,_allowCache);

            progressBar1.Value = 1;
            progressBar1.Invalidate();
            // label4.Text = string.Empty; 

            ClearSummaryGrid();
            _sw.Reset();
            _sw.Start();

            Logger.Info("Reseted the timer to 0");
            if (!Directory.Exists(FolderName))
            {
                helper.ThrowError(string.Format("{0} : Folder does not exist!!", FolderName));
                return;
            }
            Logger.Info("{0} exists.",FolderName);
            _fileList = helper.GetImages(FolderName, Extenstion);
            helper.SetFileCountList(_fileList.Count);
            Logger.Info("Found {0} files to be {1}ed in {2} directory",_fileList.Count,action,FolderName);
            if (_fileList.Count > 0)
            {
                Enabled = false; // Disable elements on the form 
                Logger.Info("Disabled the form controls, to avoid user clicking any other buttons!");
                var threadCnt = (int)helper.DoWork(_fileList);
                Logger.Info("{0} threads created",threadCnt);
                var threadFinCnt = 0; 
                Logger.Info("Checking if all threads finished the processing...");
                while ( threadCnt != threadFinCnt)
                {
                    Logger.Info("Threads created [{0}] Threads Finished [{1}]",threadCnt,threadFinCnt);
                    ProgressBarUpdater(helper.UpdateProgressBar()); 
                    threadFinCnt = helper.CheckCompletedThreadCnt();
                    Logger.Info("Sleeping for 1/5 sec before checking the status again");
                    Thread.Sleep(200);
                }
                // Assume if finished thread count == started thread count 
                ProgressBarUpdater(100);

                Logger.Info("All threads finished, completed thread count {0}",threadFinCnt);
                var failedCnt = (Dictionary<string, string>) helper.GetFailedFiles();
                Logger.Info("Failed file count {0}",failedCnt.Count);
                var retCode = failedCnt.Count > 0 ? 2 : 0; 
                CheckRCode(retCode, _fileList.Count, action);
                Logger.Info("Enabling the main window again");
                Enabled = true; // Enable elements on the form 
                config.UpdateFavoriteCache(FolderName);
                PopulateFavList();
                progressBar1.Step = 0;
                progressBar1.PerformStep();
                progressBar1.Refresh();
            }
            else
            {
                Logger.Error("There are no files to be {0}ed",action);
                helper.ThrowError(string.Format("{0} : Has 0 files to {1}!!", FolderName,action.ToUpper()));
            }
        }

        /// <summary>
        ///   Action on fav list item is selected, if selected item is 0th entry then control is given back to browse button
        /// </summary>
        private void FavListSelectedIndexChanged(object sender, EventArgs e)
        {
            if (FavList.SelectedIndex == 0)
            {
                // Indicates i am selecting empty value .. hence enable the browse button 
                Logger.Info("Index zero selected, enabling the browse button!!");
                if (button1.Enabled == false) button1.Enabled = true;
                textBox1.Clear();
            }
            else
            {
                SetDirectory(FavList.SelectedValue.ToString());
                Logger.Info("Selecting directory from the favorites, current directory {0}",FolderName);
                Logger.Info("Disabling the browse button");
                button1.Enabled = false; // disable browse button 
                InitFunctionality();
            }

        }

        /// <summary>
        ///  Show Help message and about me box .. disables after 1 use
        ///  method can be static 
        /// </summary>
        private void ShowHelp(object sender, EventArgs e) // Help 
        {
            Logger.Info("Showing help!!");
            const string help = "Nebula - Is a simple file disguiser\n"
                                + "1. This just renames the common image file from 1 extension to another.\n"
                                + "2. I have created this program as fun utility, not a serious security software.\n"
                                + "3. Use it if you dont want others to see your photos just by 1 click..\n"
                                + "Let them put some efforts!!!\n";

            MessageBox.Show(help, "About Me!!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            button4.Enabled = false;
        }

        /// <summary>
        ///  Open folder in explorer once the operation is performed
        /// </summary>
        private void OpenOpFolderClick(object sender, EventArgs e)
        {
            Logger.Info("Opening actioned folderd {0}",FolderName);
            Process.Start("explorer.exe", FolderName);
        }

        /* Below Methods are the Support methods which can not be moved to helper class 
         * as they need to access the form elements
         */

        /// <summary>
        ///    Trying to populate Favorites drop down!!
        /// </summary>
        private void PopulateFavList()
        {
            Logger.Info("Populating Favorite list");
            _allowCache = !checkBox2.Checked;
            Logger.Info("Does user want to cache the folder ? {0}",_allowCache);
            var config = new FavoritesFile(_configFile,_allowCache); 
            var folderList = config.ReadFavoritesCache();

            if (folderList.Count > 0)
            {
                Logger.Info("Cache folder count {0}",folderList.Count);
                folderList.Insert(0, "<< Select With Browse >>");
                FavList.DataSource = folderList;
                SetDirectory(FolderName); // Not sure why setting here .. need to find it out !!
            }
            else
            {
                Logger.Info("Cache is empty, only browse available. Disabling Fav list.");
                FavList.Enabled = false;
                folderList.Insert(0, "Nothing Stored In Favorites, Disabling!!");
                FavList.DataSource = folderList;
                SetDirectory(FolderName);
            }

            // label4.Text = string.Empty;
        }

        /// <summary>
        ///    Check the return code of the lock / unlock operation 
        /// </summary>
        /// <param name="rc">return code 0/1/2</param>
        /// <param name="fcnt">failed count -- Not used</param>
        /// <param name="action">action - lock / unlock </param>
        private void CheckRCode(int rc,int fcnt, string action)
        {
            Logger.Info("Check the return code of the {0} operation, rc : {1}, failed count {2}",rc,fcnt,action.ToUpper());
            var result = "" ;
            var failedCnt = 0 ;
            Logger.Info("Stopping the timer!!");
            _sw.Stop();
            TimeSpan ts = _sw.Elapsed;
            var timeDuration = string.Format("Process took {0:00}:{1:00}:{2:00} (Min:Sec:MiliSec)", ts.Minutes, ts.Seconds, ts.Milliseconds); 
            Logger.Info(timeDuration);

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
            Logger.Info("Operation result : {0}",result);
            var summary = new Hashtable {{"result", result}, {"failed_cnt", failedCnt.ToString()}, {"time_duration", timeDuration}};
            PopulateSummary(action, summary); 

        }

        /// <summary>
        ///   Populate summary at the bottom of the page in the grid box!!!
        /// </summary>
        /// <param name="workType">action performed - lock / unlock</param>
        /// <param name="summary">summary of operation</param>
        private void PopulateSummary(string workType,Hashtable summary)
        {
            Logger.Info("Populating summary, Enabling open o/p folder");
            OpenOPFolder.Enabled = true;
            ClearSummaryGrid(); 
            dataGridView1.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            
            AddSummaryRow("Selected Folder",FolderName);
            AddSummaryRow("Number of files", _fileList.Count.ToString());
            AddSummaryRow("Action",workType.ToUpper());
            AddSummaryRow("Result", (string)summary["result"]);
            AddSummaryRow("Failed File Count", (string)summary["failed_cnt"]); 
            AddSummaryRow("Used Threading", checkBox1.Checked.ToString().ToUpper());
            AddSummaryRow("Time Duration", (string)summary["time_duration"]);

            // Try to enable the buttons .. 
            var hObj = new Helper();
            button2.Enabled = hObj.GetImages(FolderName, LockExts).Count > 0 ? true : false;
            button3.Enabled = hObj.GetImages(FolderName, UnlockExts).Count > 0 ? true : false; 
        }

        /// <summary>
        ///    Helper method to add new row into summary table
        /// </summary>
        /// <param name="param">Key</param>
        /// <param name="paramValue">Value - int converted to string</param>
        private void AddSummaryRow(string param,string paramValue)
        {
            var n = dataGridView1.Rows.Add(); // Get the new row #
            Logger.Info("Adding [{0}], Key : [{1}] Value : [{2}]",n,param,paramValue);
            dataGridView1.Rows[n].Cells[0].Value = param;
            dataGridView1.Rows[n].Cells[1].Value = paramValue; 
        }

        /// <summary>
        ///  Clear the summary grid before each operation, clear only if its not empty!
        /// </summary>
        private void ClearSummaryGrid()
        {
            Logger.Info("Clearing Summary grid...");
            if (dataGridView1.RowCount != 0) dataGridView1.Rows.Clear();
        }

        /// <summary>
        ///   Do the initialization work, try to determine what kind of action can be performed and provide feedback to user!!
        /// </summary>
        private void InitFunctionality()
        {
            // Get both files to unlock & lock in current folder ... 
            Logger.Info("Initializing the Nebula in {0} folder", FolderName);
            var hObj = new Helper();
            
            Application.UseWaitCursor = true; 
            var lockImageCnt = hObj.GetImages(FolderName, LockExts).Count;
            var unlockImageCnt = hObj.GetImages(FolderName, UnlockExts).Count;
            Application.UseWaitCursor = false; 

            Logger.Info("Images can be locked ? [{0}] Images can be Unlocked ? [{1}]",lockImageCnt,unlockImageCnt);

            if (lockImageCnt == 0)
            {
                if ( button2.Enabled == true )
                    button2.Enabled = false;
            } else
            {
                if (button2.Enabled == false)
                    button2.Enabled = true; 
            }

            if (unlockImageCnt == 0)
            {
                if ( button3.Enabled == true )
                  button3.Enabled = false;
            }
            else
            {
                if ( button3.Enabled == false )
                  button3.Enabled = true;
            }

            Logger.Info("Lock button status {0}",button2.Enabled);
            Logger.Info("Unlock button status {0}", button3.Enabled);
            ClearSummaryGrid();
            AddSummaryRow("Number of Images Can be LOCKED?", lockImageCnt.ToString(CultureInfo.InvariantCulture));
            AddSummaryRow("Number of Images Can be UNLOCKED?", unlockImageCnt.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        ///    Set global variables
        /// </summary>
        /// <param name="dname">directory name</param>
        private void SetDirectory(string dname)
        {
            Logger.Info("Setting action directory {0}",dname);
            textBox1.Text = dname;
            FolderName = dname; 
        }

        public void ProgressBarUpdater(int pCnt)
        {
            Logger.Info("Progress : {0}%",pCnt);
            if (progressBar1.Value >= 100) return;
            // label4.Clear(); 
            // label4.Text = string.Format("{0} %", pCnt);
            progressBar1.Step = pCnt;
            progressBar1.PerformStep();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Logger.Info("Opening Log file...");
            Process.Start("notepad.exe", Path.Combine(Environment.CurrentDirectory,"log.txt"));
        }

        //private void CheckBox3CheckedChanged(object sender, EventArgs e)
        //{
        //    Helper.TraceEnabled(checkBox3.Checked);
        //}
    }
}