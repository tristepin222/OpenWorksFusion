#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Crosstales.TB.Util
{
   /// <summary>Various helper functions.</summary>
   public abstract class Helper : Crosstales.Common.EditorUtil.BaseEditorHelper
   {
      #region Static variables

      private static Texture2D action_backup;
      private static Texture2D action_restore;

      private static Texture2D logo_asset;
      private static Texture2D logo_asset_small;
      private static Texture2D icon_show;

      public static string ScanInfo;
      private static bool isScanning;
      public static bool isDeleting;

      #endregion


      #region Static properties

      public static Texture2D Action_Backup => loadImage(ref action_backup, "action_backup.png");

      public static Texture2D Action_Restore => loadImage(ref action_restore, "action_restore.png");

      public static Texture2D Logo_Asset => loadImage(ref logo_asset, "logo_asset_pro.png");

      public static Texture2D Logo_Asset_Small => loadImage(ref logo_asset_small, "logo_asset_small_pro.png");

      public static Texture2D Icon_Show => loadImage(ref icon_show, "icon_show.png");

      /// <summary>Checks if the backup for the project is enabled.</summary>
      /// <returns>True if a backup is enabled</returns>
      public static bool isBackupEnabled => Config.COPY_ASSETS || Config.COPY_LIBRARY || Config.COPY_SETTINGS || Config.COPY_PACKAGES;

      /// <summary>Checks if a backup for the project exists.</summary>
      /// <returns>True if a backup for the project exists</returns>
      public static bool hasBackup => Crosstales.Common.Util.FileHelper.ExistsDirectory(Config.PATH_BACKUP);

      /// <summary>Checks if a backup for the current slot exists.</summary>
      /// <returns>True if a backup for the current slot exists</returns>
      public static bool hasBackupSlot => Crosstales.Common.Util.FileHelper.ExistsDirectory(Config.PATH_BACKUP_SLOT);

      /// <summary>Scans the backup usage information.</summary>
      /// <returns>Backup usage information.</returns>
      public static string BackupInfo
      {
         get
         {
            string result = Constants.TEXT_NO_BACKUP;

            if (hasBackup)
            {
               if (!string.IsNullOrEmpty(ScanInfo))
               {
                  result = ScanInfo;
               }
               else
               {
                  if (!isScanning)
                  {
                     isScanning = true;

                     if (Crosstales.Common.Util.FileHelper.ExistsDirectory(Config.PATH_BACKUP))
                     {
                        System.Threading.Thread worker = isWindowsEditor ? new System.Threading.Thread(() => scanWindows(Config.PATH_BACKUP, ref ScanInfo)) : new System.Threading.Thread(() => scanUnix(Config.PATH_BACKUP, ref ScanInfo));
                        worker.Start();
                     }
                  }
                  else
                  {
                     result = "Scanning...";
                  }
               }
            }

            return result;
         }
      }

      #endregion


      #region Public static methods

      /// <summary>Backup the project (legacy implementation).</summary>
      /// <returns>True if the backup was successful.</returns>
      public static bool Backup()
      {
         bool success = false;

         if (saveScenes())
         {
            setupVCS();

            if (!string.IsNullOrEmpty(Config.EXECUTE_METHOD_PRE_BACKUP))
               InvokeMethod(Config.EXECUTE_METHOD_PRE_BACKUP.Substring(0, Config.EXECUTE_METHOD_PRE_BACKUP.CTLastIndexOf(".")), Config.EXECUTE_METHOD_PRE_BACKUP.Substring(Config.EXECUTE_METHOD_PRE_BACKUP.CTLastIndexOf(".") + 1));

            using (System.Diagnostics.Process process = new System.Diagnostics.Process())
            {
               try
               {
                  process.StartInfo.CreateNoWindow = true;
                  process.StartInfo.UseShellExecute = false;

                  string scriptfile;

                  if (isWindowsEditor)
                  {
                     scriptfile = Crosstales.Common.Util.FileHelper.TempPath + "TB-Backup_" + System.Guid.NewGuid() + ".cmd";

                     Crosstales.Common.Util.FileHelper.WriteAllText(scriptfile, generateWindowsBackupScript());

                     process.StartInfo.FileName = "cmd.exe";
                     process.StartInfo.Arguments = "/c start  \"\" " + '"' + scriptfile + '"';
                  }
                  else if (isMacOSEditor)
                  {
                     scriptfile = Crosstales.Common.Util.FileHelper.TempPath + "TB-Backup_" + System.Guid.NewGuid() + ".sh";

                     Crosstales.Common.Util.FileHelper.WriteAllText(scriptfile, generateMacBackupScript());

                     process.StartInfo.FileName = "/bin/sh";
                     process.StartInfo.Arguments = '"' + scriptfile + "\" &";
                  }
                  else if (isLinuxEditor)
                  {
                     scriptfile = Crosstales.Common.Util.FileHelper.TempPath + "TB-Backup_" + System.Guid.NewGuid() + ".sh";

                     Crosstales.Common.Util.FileHelper.WriteAllText(scriptfile, generateLinuxBackupScript());

                     process.StartInfo.FileName = "/bin/sh";
                     process.StartInfo.Arguments = '"' + scriptfile + "\" &";
                  }
                  else
                  {
                     Debug.LogError("Unsupported Unity Editor: " + Application.platform);
                     return false;
                  }

                  ScanInfo = null;
                  Config.BACKUP_DATE = System.DateTime.Now;
                  Config.BACKUP_COUNT++;
                  Config.Save();

                  process.Start();

                  if (isWindowsEditor)
                     process.WaitForExit(Constants.PROCESS_KILL_TIME);

                  success = true;
               }
               catch (System.Exception ex)
               {
                  string errorMessage = "Could not execute " + Constants.ASSET_NAME + "!" + System.Environment.NewLine + ex;
                  Debug.LogError(errorMessage);
               }
            }

            if (success)
               EditorApplication.Exit(0);
         }
         else
         {
            Debug.LogWarning("User canceled the backup.");
         }

         return success;
      }

      /// <summary>Backup the project.</summary>
      /// <returns>True if the backup was successful.</returns>
      public static bool BackupNew()
      {
         bool success = false;

         if (saveScenes())
         {
            setupVCS();

            if (!string.IsNullOrEmpty(Config.EXECUTE_METHOD_PRE_BACKUP))
               InvokeMethod(Config.EXECUTE_METHOD_PRE_BACKUP.Substring(0, Config.EXECUTE_METHOD_PRE_BACKUP.CTLastIndexOf(".")), Config.EXECUTE_METHOD_PRE_BACKUP.Substring(Config.EXECUTE_METHOD_PRE_BACKUP.CTLastIndexOf(".") + 1));

            AssetDatabase.SaveAssets();

            //AssetDatabase.StartAssetEditing();

            if (isWindowsEditor)
            {
               success = backupWindows();
            }
            else if (isMacOSEditor || isLinuxEditor)
            {
               success = backupUnix();
            }
            else
            {
               Debug.LogError("Unsupported Unity Editor: " + Application.platform);
               return false;
            }

            if (success)
            {
               Config.BACKUP_DATE = System.DateTime.Now;
               Config.BACKUP_COUNT++;
               Config.Save();
            }

            //AssetDatabase.StopAssetEditing();

            if (!string.IsNullOrEmpty(Config.EXECUTE_METHOD_BACKUP))
               InvokeMethod(Config.EXECUTE_METHOD_BACKUP.Substring(0, Config.EXECUTE_METHOD_BACKUP.CTLastIndexOf(".")), Config.EXECUTE_METHOD_BACKUP.Substring(Config.EXECUTE_METHOD_BACKUP.CTLastIndexOf(".") + 1));

            ScanInfo = null;
         }
         else
         {
            Debug.LogWarning("User canceled the backup.");
         }

         return success;
      }

      /// <summary>Restore the project (legacy implementation).</summary>
      /// <returns>True if the restore was successful.</returns>
      public static bool Restore()
      {
         bool success = false;

         if (saveScenes())
         {
            if (!string.IsNullOrEmpty(Config.EXECUTE_METHOD_PRE_RESTORE))
               InvokeMethod(Config.EXECUTE_METHOD_PRE_RESTORE.Substring(0, Config.EXECUTE_METHOD_PRE_RESTORE.CTLastIndexOf(".")), Config.EXECUTE_METHOD_PRE_RESTORE.Substring(Config.EXECUTE_METHOD_PRE_RESTORE.CTLastIndexOf(".") + 1));

            using (System.Diagnostics.Process process = new System.Diagnostics.Process())
            {
               try
               {
                  process.StartInfo.CreateNoWindow = true;
                  process.StartInfo.UseShellExecute = false;

                  string scriptfile;

                  switch (Application.platform)
                  {
                     case RuntimePlatform.WindowsEditor:
                        scriptfile = Crosstales.Common.Util.FileHelper.TempPath + "TB-Restore_" + System.Guid.NewGuid() + ".cmd";

                        Crosstales.Common.Util.FileHelper.WriteAllText(scriptfile, generateWindowsRestoreScript());

                        process.StartInfo.FileName = "cmd.exe";
                        process.StartInfo.Arguments = "/c start  \"\" " + '"' + scriptfile + '"';
                        break;
                     case RuntimePlatform.OSXEditor:
                        scriptfile = Crosstales.Common.Util.FileHelper.TempPath + "TB-Restore_" + System.Guid.NewGuid() + ".sh";

                        Crosstales.Common.Util.FileHelper.WriteAllText(scriptfile, generateMacRestoreScript());

                        process.StartInfo.FileName = "/bin/sh";
                        process.StartInfo.Arguments = '"' + scriptfile + "\" &";
                        break;
                     case RuntimePlatform.LinuxEditor:
                        scriptfile = Crosstales.Common.Util.FileHelper.TempPath + "TB-Restore_" + System.Guid.NewGuid() + ".sh";

                        Crosstales.Common.Util.FileHelper.WriteAllText(scriptfile, generateLinuxRestoreScript());

                        process.StartInfo.FileName = "/bin/sh";
                        process.StartInfo.Arguments = '"' + scriptfile + "\" &";
                        break;
                     default:
                        Debug.LogError("Unsupported Unity Editor: " + Application.platform);
                        return false;
                  }

                  ScanInfo = null;
                  Config.RESTORE_DATE = System.DateTime.Now;
                  Config.RESTORE_COUNT++;
                  Config.Save();

                  process.Start();

                  if (isWindowsEditor)
                     process.WaitForExit(Constants.PROCESS_KILL_TIME);

                  success = true;
               }
               catch (System.Exception ex)
               {
                  string errorMessage = "Could not execute " + Constants.ASSET_NAME + "!" + System.Environment.NewLine + ex;
                  Debug.LogError(errorMessage);
               }
            }

            if (success)
               EditorApplication.Exit(0);
         }
         else
         {
            Debug.LogWarning("User canceled the restore.");
         }

         return success;
      }

      /// <summary>Restore the project.</summary>
      /// <returns>True if the restore was successful.</returns>
      public static bool RestoreNew()
      {
         bool success = false;

         if (saveScenes())
         {
            if (!string.IsNullOrEmpty(Config.EXECUTE_METHOD_PRE_RESTORE))
               InvokeMethod(Config.EXECUTE_METHOD_PRE_RESTORE.Substring(0, Config.EXECUTE_METHOD_PRE_RESTORE.CTLastIndexOf(".")), Config.EXECUTE_METHOD_PRE_RESTORE.Substring(Config.EXECUTE_METHOD_PRE_RESTORE.CTLastIndexOf(".") + 1));

            AssetDatabase.SaveAssets();
            AssetDatabase.ReleaseCachedFileHandles();
            AssetDatabase.StartAssetEditing();

            if (isWindowsEditor)
            {
               success = restoreWindows();
            }
            else if (isMacOSEditor || isLinuxEditor)
            {
               success = restoreUnix();
            }
            else
            {
               Debug.LogError("Unsupported Unity Editor: " + Application.platform);
               return false;
            }

            AssetDatabase.StopAssetEditing();

            RefreshAssetDatabase(ImportAssetOptions.ForceUpdate);

            if (success)
            {
               Config.RESTORE_DATE = System.DateTime.Now;
               Config.RESTORE_COUNT++;
               Config.Save();
            }

            if (!string.IsNullOrEmpty(Config.EXECUTE_METHOD_RESTORE))
               InvokeMethod(Config.EXECUTE_METHOD_RESTORE.Substring(0, Config.EXECUTE_METHOD_RESTORE.CTLastIndexOf(".")), Config.EXECUTE_METHOD_RESTORE.Substring(Config.EXECUTE_METHOD_RESTORE.CTLastIndexOf(".") + 1));

            ScanInfo = null;
         }
         else
         {
            Debug.LogWarning("User canceled the restore.");
         }

         return success;
      }

      /// <summary>Delete the backup for all platforms.</summary>
      public static void DeleteBackup()
      {
         if (!isDeleting && Crosstales.Common.Util.FileHelper.ExistsDirectory(Config.PATH_BACKUP))
         {
            isDeleting = true;

            System.Threading.Thread worker = new System.Threading.Thread(deleteBackup);
            worker.Start();

            Config.BACKUP_COUNT = 0;
            Config.RESTORE_COUNT = 0;
            Config.Save();
         }
      }

      /// <summary>Delete the given backup slot for all platforms.</summary>
      /// <param name="slot">Backup slot to delete</param>
      public static void DeleteBackupSlot(int slot)
      {
         if (!isDeleting && Crosstales.Common.Util.FileHelper.ExistsDirectory($"{Config.PATH_BACKUP}{slot}"))
         {
            isDeleting = true;

            System.Threading.Thread worker = new System.Threading.Thread(() => deleteBackupSlot(slot));
            worker.Start();
         }
      }

      /*
      /// <summary>Delete all shell-scripts after a platform switch.</summary>
      public static void DeleteAllScripts()
      {
          //INFO: currently disabled since it could interfere with running scripts!

          DirectoryInfo dir = new DirectoryInfo(Path.GetTempPath());

          try
          {
              foreach (FileInfo file in dir.GetFiles("TPS-" + Constants.ASSET_ID + "*"))
              {
                  if (Constants.DEBUG)
                      Debug.Log("Script file deleted: " + file);

                  file.Delete();
              }
          }
          catch (Exception ex)
          {
              Debug.LogWarning("Could not delete all script files!" + Environment.NewLine + ex);
          }
      }
      */

      #endregion


      #region Private static methods

      private static bool saveScenes()
      {
         bool result = true;

         if (Config.AUTO_SAVE)
         {
            if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty)
               UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
         }
         else
         {
            result = UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
         }

         return result;
      }

      private static void setupVCS()
      {
         if (!Config.CUSTOM_PATH_BACKUP && Config.VCS != 0)
         {
            switch (Config.VCS)
            {
               case 1:
               {
                  // git
                  try
                  {
                     if (Crosstales.Common.Util.FileHelper.ExistsFile(Constants.APPLICATION_PATH + ".gitignore"))
                     {
                        string content = Crosstales.Common.Util.FileHelper.ReadAllText(Constants.APPLICATION_PATH + ".gitignore");

                        if (!content.Contains(Constants.BACKUP_DIRNAME + "/"))
                        {
                           Crosstales.Common.Util.FileHelper.WriteAllText(Constants.APPLICATION_PATH + ".gitignore", content.TrimEnd() + System.Environment.NewLine + Constants.BACKUP_DIRNAME + "/");
                        }
                     }
                     else
                     {
                        Crosstales.Common.Util.FileHelper.WriteAllText(Constants.APPLICATION_PATH + ".gitignore", Constants.BACKUP_DIRNAME + "/");
                     }
                  }
                  catch (System.Exception ex)
                  {
                     string errorMessage = "Could not add entry to .gitignore! Please add the entry '" + Constants.BACKUP_DIRNAME + "/' manually." + System.Environment.NewLine + ex;
                     Debug.LogError(errorMessage);
                  }

                  break;
               }
               case 2:
               {
                  // svn
                  using (System.Diagnostics.Process process = new System.Diagnostics.Process())
                  {
                     process.StartInfo.FileName = "svn";
                     process.StartInfo.Arguments = "propset svn: ignore " + Constants.BACKUP_DIRNAME + ".";
                     process.StartInfo.WorkingDirectory = Constants.APPLICATION_PATH;
                     process.StartInfo.UseShellExecute = false;

                     try
                     {
                        process.Start();
                        process.WaitForExit(Constants.PROCESS_KILL_TIME);
                     }
                     catch (System.Exception ex)
                     {
                        string errorMessage = "Could not execute svn-ignore! Please do it manually in the console: 'svn propset svn:ignore " + Constants.BACKUP_DIRNAME + ".'" + System.Environment.NewLine + ex;
                        Debug.LogError(errorMessage);
                     }
                  }

                  break;
               }
               case 3:
                  // mercurial
                  Debug.LogWarning("Mercurial currently not supported. Please add the following lines to your .hgignore: " + System.Environment.NewLine + "syntax: glob" + System.Environment.NewLine + Constants.BACKUP_DIRNAME + "/**");
                  break;
               case 4:
               {
                  // collab
                  try
                  {
                     if (Crosstales.Common.Util.FileHelper.ExistsFile(Constants.APPLICATION_PATH + ".collabignore"))
                     {
                        string content = Crosstales.Common.Util.FileHelper.ReadAllText(Constants.APPLICATION_PATH + ".collabignore");

                        if (!content.Contains(Constants.BACKUP_DIRNAME + "/"))
                        {
                           Crosstales.Common.Util.FileHelper.WriteAllText(Constants.APPLICATION_PATH + ".collabignore", content.TrimEnd() + System.Environment.NewLine + Constants.BACKUP_DIRNAME + "/");
                        }
                     }
                     else
                     {
                        Crosstales.Common.Util.FileHelper.WriteAllText(Constants.APPLICATION_PATH + ".collabignore", Constants.BACKUP_DIRNAME + "/");
                     }
                  }
                  catch (System.Exception ex)
                  {
                     string errorMessage = "Could not add entry to .collabignore! Please add the entry '" + Constants.BACKUP_DIRNAME + "/' manually." + System.Environment.NewLine + ex;
                     Debug.LogError(errorMessage);
                  }

                  break;
               }
               case 5:
               {
                  // PlasticSCM
                  try
                  {
                     if (Crosstales.Common.Util.FileHelper.ExistsFile(Constants.APPLICATION_PATH + "ignore.conf"))
                     {
                        string content = Crosstales.Common.Util.FileHelper.ReadAllText(Constants.APPLICATION_PATH + "ignore.conf");

                        if (!content.Contains(Constants.BACKUP_DIRNAME))
                        {
                           Crosstales.Common.Util.FileHelper.WriteAllText(Constants.APPLICATION_PATH + "ignore.conf", content.TrimEnd() + System.Environment.NewLine + Constants.BACKUP_DIRNAME);
                        }
                     }
                     else
                     {
                        Crosstales.Common.Util.FileHelper.WriteAllText(Constants.APPLICATION_PATH + "ignore.conf", Constants.BACKUP_DIRNAME);
                     }
                  }
                  catch (System.Exception ex)
                  {
                     string errorMessage = "Could not add entry to ignore.conf! Please add the entry '" + Constants.BACKUP_DIRNAME + "' manually." + System.Environment.NewLine + ex;
                     Debug.LogError(errorMessage);
                  }

                  break;
               }
               default:
               {
                  Debug.LogWarning("Unknown VCS selected: " + Config.VCS);
                  break;
               }
            }
         }
      }

      private static void deleteBackup()
      {
         try
         {
            Crosstales.Common.Util.FileHelper.DeleteDirectory(Config.PATH_BACKUP);

            //Config.BACKUP_COUNT = 0;
            //Config.RESTORE_COUNT = 0;
            ScanInfo = null;
         }
         catch (System.Exception ex)
         {
            Debug.LogWarning("Could not delete the backup!" + System.Environment.NewLine + ex);
         }

         isDeleting = false;
      }

      private static void deleteBackupSlot(int slot)
      {
         try
         {
            Crosstales.Common.Util.FileHelper.DeleteDirectory($"{Config.PATH_BACKUP}{slot}");

            ScanInfo = null;
         }
         catch (System.Exception ex)
         {
            Debug.LogWarning("Could not delete the backup!" + System.Environment.NewLine + ex);
         }

         isDeleting = false;
      }

      #region Windows

      private static void scanWindows(string path, ref string key)
      {
         using (System.Diagnostics.Process scanProcess = new System.Diagnostics.Process())
         {
            const string args = "/c dir * /s /a";

            if (Config.DEBUG)
               Debug.Log("Process arguments: '" + args + "'");

            System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>();

            scanProcess.StartInfo.FileName = "cmd.exe";
            scanProcess.StartInfo.WorkingDirectory = path;
            scanProcess.StartInfo.Arguments = args;
            scanProcess.StartInfo.CreateNoWindow = true;
            scanProcess.StartInfo.RedirectStandardOutput = true;
            scanProcess.StartInfo.RedirectStandardError = true;
            scanProcess.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
            scanProcess.StartInfo.UseShellExecute = false;
            scanProcess.OutputDataReceived += (sender, eventArgs) => result.Add(eventArgs.Data);

            bool success = true;

            try
            {
               scanProcess.Start();
               scanProcess.BeginOutputReadLine();
            }
            catch (System.Exception ex)
            {
               success = false;
               Debug.LogError("Could not start the scan process!" + System.Environment.NewLine + ex);
            }

            if (success)
            {
               do
               {
                  System.Threading.Thread.Sleep(100);
               } while (!scanProcess.HasExited);

               if (scanProcess.ExitCode == 0)
               {
                  /*
                  using (System.IO.StreamReader sr = scanProcess.StandardOutput)
                  {
                      result.AddRange(SplitStringToLines(sr.ReadToEnd()));
                  }
                  */

                  if (Config.DEBUG)
                     Debug.LogWarning("Scan completed: " + result.Count);

                  if (result.Count >= 3)
                  {
                     key = result[result.Count - 3].Trim();
                  }
                  else
                  {
                     Debug.LogWarning("Scan problem; not enough lines were returned: " + result.Count);
                     key = "Scan problem";
                  }
               }
               else
               {
                  using (System.IO.StreamReader sr = scanProcess.StandardError)
                  {
                     Debug.LogError("Could not scan the path: " + scanProcess.ExitCode + System.Environment.NewLine + sr.ReadToEnd());
                  }
               }
            }
         }

         isScanning = false;
      }

      private static bool backupWindows()
      {
         bool success = true;

         using (System.Diagnostics.Process process = new System.Diagnostics.Process())
         {
            try
            {
               process.StartInfo.CreateNoWindow = true;
               process.StartInfo.UseShellExecute = false;
               process.StartInfo.FileName = "cmd.exe";

               // Save Assets
               if (Config.COPY_ASSETS)
               {
                  EditorUtility.DisplayProgressBar("Backup", "Save Assets...", 0.25f);

                  System.Text.StringBuilder sb = new System.Text.StringBuilder();
                  sb.Append("/c robocopy \"");
                  sb.Append(Constants.APPLICATION_PATH);
                  sb.Append("Assets\" \"");
                  sb.Append(Config.PATH_BACKUP_SLOT);
                  sb.Append("Assets");
                  sb.Append($"\" /MIR /W:2 /R:3 /MT:{Config.THREADS} /NFL /NDL /NJH /NJS /nc /ns /np > NUL");

                  //Debug.Log(sb);

                  process.StartInfo.Arguments = sb.ToString();
                  process.Start();

                  process.WaitForExit();

                  success = process.ExitCode == 0 || process.ExitCode == 1;
               }

/*
               // Save Library
               if (Config.COPY_LIBRARY && success)
               {
                  EditorUtility.DisplayProgressBar("Backup", "Save Library...", 0.5f);

                  System.Text.StringBuilder sb = new System.Text.StringBuilder();
                  sb.Append("/c robocopy \"");
                  sb.Append(Constants.APPLICATION_PATH);
                  sb.Append("Library\" \"");
                  sb.Append(Config.PATH_BACKUP);
                  sb.Append("Library");
                  sb.Append($"\" /MIR /W:2 /R:3 /MT:{Config.THREADS} /NFL /NDL /NJH /NJS /nc /ns /np > NUL");

                  //Debug.Log(sb);

                  process.StartInfo.Arguments = sb.ToString();
                  process.Start();

                  process.WaitForExit();

                  success = process.ExitCode == 0 || process.ExitCode == 1;
               }
*/
               // Save ProjectSettings
               if (Config.COPY_SETTINGS && success)
               {
                  EditorUtility.DisplayProgressBar("Backup", "Save ProjectSettings...", 0.75f);

                  System.Text.StringBuilder sb = new System.Text.StringBuilder();
                  sb.Append("/c robocopy \"");
                  sb.Append(Constants.APPLICATION_PATH);
                  sb.Append("ProjectSettings\" \"");
                  sb.Append(Config.PATH_BACKUP_SLOT);
                  sb.Append("ProjectSettings");
                  sb.Append($"\" /MIR /W:2 /R:3 /MT:{Config.THREADS} /NFL /NDL /NJH /NJS /nc /ns /np > NUL");

                  process.StartInfo.Arguments = sb.ToString();
                  process.Start();

                  process.WaitForExit();

                  success = process.ExitCode == 0 || process.ExitCode == 1;
               }

               // Save UserSettings
               if (Config.COPY_USER_SETTINGS && success)
               {
                  EditorUtility.DisplayProgressBar("Backup", "Save UserSettings...", 0.8f);

                  System.Text.StringBuilder sb = new System.Text.StringBuilder();
                  sb.Append("/c robocopy \"");
                  sb.Append(Constants.APPLICATION_PATH);
                  sb.Append("UserSettings\" \"");
                  sb.Append(Config.PATH_BACKUP_SLOT);
                  sb.Append("UserSettings");
                  sb.Append($"\" /MIR /W:2 /R:3 /MT:{Config.THREADS} /NFL /NDL /NJH /NJS /nc /ns /np > NUL");

                  process.StartInfo.Arguments = sb.ToString();
                  process.Start();

                  process.WaitForExit();

                  success = process.ExitCode == 0 || process.ExitCode == 1;
               }

               // Save Packages
               if (Config.COPY_PACKAGES && success)
               {
                  EditorUtility.DisplayProgressBar("Backup", "Save Packages...", 1f);

                  System.Text.StringBuilder sb = new System.Text.StringBuilder();
                  sb.Append("/c robocopy \"");
                  sb.Append(Constants.APPLICATION_PATH);
                  sb.Append("Packages\" \"");
                  sb.Append(Config.PATH_BACKUP_SLOT);
                  sb.Append("Packages");
                  sb.Append($"\" /MIR /W:2 /R:3 /MT:{Config.THREADS} /NFL /NDL /NJH /NJS /nc /ns /np > NUL");

                  process.StartInfo.Arguments = sb.ToString();
                  process.Start();

                  process.WaitForExit();

                  success = process.ExitCode == 0 || process.ExitCode == 1;
               }
            }
            catch (System.Exception ex)
            {
               success = false;
               string errorMessage = "Could not execute " + Constants.ASSET_NAME + "!" + System.Environment.NewLine + ex;
               Debug.LogError(errorMessage);
            }
         }

         EditorUtility.ClearProgressBar();

         return success;
      }

      private static string generateWindowsBackupScript()
      {
         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         // setup
         sb.AppendLine("@echo off");
         sb.AppendLine("cls");

         // title
         sb.Append("title ");
         sb.Append(Constants.ASSET_NAME);
         sb.Append(" - Backup of ");
         sb.Append(Application.productName);
         sb.AppendLine(" in progress - DO NOT CLOSE THIS WINDOW!");

         // header
         sb.AppendLine("echo ##############################################################################");
         sb.AppendLine("echo #                                                                            #");
         sb.Append("echo #  ");
         sb.Append(Constants.ASSET_NAME);
         sb.Append(" ");
         sb.Append(Constants.ASSET_VERSION);
         sb.AppendLine(" - Windows                                       #");
         sb.AppendLine("echo #  Copyright 2018-2024 by www.crosstales.com                                 #");
         sb.AppendLine("echo #                                                                            #");
         sb.AppendLine("echo #  The files will now be saved to the backup destination.                    #");
         sb.AppendLine("echo #  This will take some time, so please be patient and DON'T CLOSE THIS       #");
         sb.AppendLine("echo #  WINDOW before the process is finished!                                    #");
         sb.AppendLine("echo #                                                                            #");
         sb.AppendLine("echo #  Unity will restart automatically after the backup.                        #");
         sb.AppendLine("echo #                                                                            #");
         sb.AppendLine("echo ##############################################################################");
         sb.AppendLine("echo " + Application.productName);
         sb.AppendLine("echo.");
         sb.AppendLine("echo.");

         // check if Unity is closed
         sb.AppendLine(":waitloop");
         sb.Append("if not exist \"");
         sb.Append(Constants.APPLICATION_PATH);
         sb.Append("Temp\\UnityLockfile\" goto waitloopend");
         sb.AppendLine();
         sb.AppendLine("echo.");
         sb.AppendLine("echo Waiting for Unity to close...");
         sb.AppendLine("timeout /t 3");

         if (Config.DELETE_LOCKFILE)
         {
            sb.Append("del \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.Append("Temp\\UnityLockfile\" /q");
            sb.AppendLine();
         }

         sb.AppendLine("goto waitloop");
         sb.AppendLine(":waitloopend");

         // Save files
         sb.AppendLine("echo.");
         sb.AppendLine("echo ##############################################################################");
         sb.AppendLine("echo #  Saving files                                                              #");
         sb.AppendLine("echo ##############################################################################");

         // Assets
         if (Config.COPY_ASSETS)
         {
            sb.Append("robocopy \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.Append("Assets\" \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("Assets");
            sb.AppendLine($"\" /MIR /W:2 /R:3 /MT:{Config.THREADS} /NFL /NDL /NJH /NJS /nc /ns /np > NUL");
         }

         // Library
         if (Config.COPY_LIBRARY)
         {
            sb.Append("robocopy \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.Append("Library\" \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("Library");
            sb.AppendLine($"\" /MIR /W:2 /R:3 /MT:{Config.THREADS} /NFL /NDL /NJH /NJS /nc /ns /np > NUL");
         }

         // ProjectSettings
         if (Config.COPY_SETTINGS)
         {
            sb.Append("robocopy \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.Append("ProjectSettings\" \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("ProjectSettings");
            sb.AppendLine($"\" /MIR /W:2 /R:3 /MT:{Config.THREADS} /NFL /NDL /NJH /NJS /nc /ns /np > NUL");
         }

         // UserSettings
         if (Config.COPY_USER_SETTINGS)
         {
            sb.Append("robocopy \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.Append("UserSettings\" \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("UserSettings");
            sb.AppendLine($"\" /MIR /W:2 /R:3 /MT:{Config.THREADS} /NFL /NDL /NJH /NJS /nc /ns /np > NUL");
         }

         // Packages
         if (Config.COPY_PACKAGES)
         {
            sb.Append("robocopy \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.Append("Packages\" \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("Packages");
            sb.AppendLine($"\" /MIR /W:2 /R:3 /MT:{Config.THREADS} /NFL /NDL /NJH /NJS /nc /ns /np > NUL");
         }

         // Restart Unity
         sb.AppendLine("echo.");
         sb.AppendLine("echo ##############################################################################");
         sb.AppendLine("echo #  Restarting Unity                                                          #");
         sb.AppendLine("echo ##############################################################################");
         sb.Append("start \"\" \"");
         sb.Append(Crosstales.Common.Util.FileHelper.ValidatePath(EditorApplication.applicationPath, false));
         sb.Append("\" -projectPath \"");
         sb.Append(Constants.APPLICATION_PATH.Substring(0, Constants.APPLICATION_PATH.Length - 1));
         sb.Append("\"");

         if (Config.BATCHMODE)
         {
            sb.Append(" -batchmode");

            if (Config.QUIT)
               sb.Append(" -quit");

            if (Config.NO_GRAPHICS)
               sb.Append(" -nographics");
         }

         if (!string.IsNullOrEmpty(Config.EXECUTE_METHOD_BACKUP))
         {
            sb.Append(" -executeMethod ");
            sb.Append(Config.EXECUTE_METHOD_BACKUP);
         }
         else
         {
            sb.Append(" -executeMethod Crosstales.TB.BAR.DefaultMethodAfterBackup");
         }

         sb.AppendLine();
         sb.AppendLine("echo.");

         // check if Unity is started
         sb.AppendLine(":waitloop2");
         sb.Append("if exist \"");
         sb.Append(Constants.APPLICATION_PATH);
         sb.Append("Temp\\UnityLockfile\" goto waitloopend2");
         sb.AppendLine();
         sb.AppendLine("echo Waiting for Unity to start...");
         sb.AppendLine("timeout /t 3");
         sb.AppendLine("goto waitloop2");
         sb.AppendLine(":waitloopend2");
         sb.AppendLine("echo.");
         sb.AppendLine("echo Bye!");
         sb.AppendLine("timeout /t 1");
         sb.AppendLine("exit");

         return sb.ToString();
      }

      private static bool restoreWindows()
      {
         bool success = true;

         using (System.Diagnostics.Process process = new System.Diagnostics.Process())
         {
            try
            {
               process.StartInfo.CreateNoWindow = true;
               process.StartInfo.UseShellExecute = false;
               process.StartInfo.FileName = "cmd.exe";

               // Restore Assets
               if (Config.COPY_ASSETS)
               {
                  EditorUtility.DisplayProgressBar("Restore", "Restore Assets...", 0.25f);

                  System.Text.StringBuilder sb = new System.Text.StringBuilder();
                  sb.Append("/c robocopy \"");
                  sb.Append(Config.PATH_BACKUP_SLOT);
                  sb.Append("Assets\" \"");
                  sb.Append(Constants.APPLICATION_PATH);
                  sb.Append($"Assets\" /MIR /W:2 /R:3 /MT:{Config.THREADS} /NFL /NDL /NJH /NJS /nc /ns /np > NUL");

                  process.StartInfo.Arguments = sb.ToString();
                  process.Start();

                  process.WaitForExit();

                  success = process.ExitCode == 0 || process.ExitCode == 1;
               }

/*
               // Restore Library
               if (Config.COPY_LIBRARY && success)
               {
                  EditorUtility.DisplayProgressBar("Restore", "Restore Library...", 0.5f);

                  System.Text.StringBuilder sb = new System.Text.StringBuilder();
                  sb.Append("/c robocopy \"");
                  sb.Append(Config.PATH_BACKUP);
                  sb.Append("Library\" \"");
                  sb.Append(Constants.APPLICATION_PATH);
                  sb.Append($"Library\" /MIR /W:2 /R:3 /MT:{Config.THREADS} /NFL /NDL /NJH /NJS /nc /ns /np > NUL");

                  process.StartInfo.Arguments = sb.ToString();
                  process.Start();

                  process.WaitForExit();

                  success = process.ExitCode == 0 || process.ExitCode == 1;
               }
*/
               // Restore ProjectSettings
               if (Config.COPY_SETTINGS && success)
               {
                  EditorUtility.DisplayProgressBar("Restore", "Restore ProjectSettings...", 0.75f);

                  System.Text.StringBuilder sb = new System.Text.StringBuilder();
                  sb.Append("/c robocopy \"");
                  sb.Append(Config.PATH_BACKUP_SLOT);
                  sb.Append("ProjectSettings\" \"");
                  sb.Append(Constants.APPLICATION_PATH);
                  sb.Append($"ProjectSettings\" /MIR /W:2 /R:3 /MT:{Config.THREADS} /NFL /NDL /NJH /NJS /nc /ns /np > NUL");

                  process.StartInfo.Arguments = sb.ToString();
                  process.Start();

                  process.WaitForExit();

                  success = process.ExitCode == 0 || process.ExitCode == 1;
               }

               // Restore UserSettings
               if (Config.COPY_USER_SETTINGS && success)
               {
                  EditorUtility.DisplayProgressBar("Restore", "Restore UserSettings...", 0.8f);

                  System.Text.StringBuilder sb = new System.Text.StringBuilder();
                  sb.Append("/c robocopy \"");
                  sb.Append(Config.PATH_BACKUP_SLOT);
                  sb.Append("UserSettings\" \"");
                  sb.Append(Constants.APPLICATION_PATH);
                  sb.AppendLine($"UserSettings\" /MIR /W:2 /R:3 /MT:{Config.THREADS} /NFL /NDL /NJH /NJS /nc /ns /np > NUL");

                  process.StartInfo.Arguments = sb.ToString();
                  process.Start();

                  process.WaitForExit();

                  success = process.ExitCode == 0 || process.ExitCode == 1;
               }

               // Restore Packages
               if (Config.COPY_PACKAGES && success)
               {
                  EditorUtility.DisplayProgressBar("Restore", "Restore Packages...", 1f);

                  System.Text.StringBuilder sb = new System.Text.StringBuilder();
                  sb.Append("/c robocopy \"");
                  sb.Append(Config.PATH_BACKUP_SLOT);
                  sb.Append("Packages\" \"");
                  sb.Append(Constants.APPLICATION_PATH);
                  sb.Append($"Packages\" /MIR /W:2 /R:3 /MT:{Config.THREADS} /NFL /NDL /NJH /NJS /nc /ns /np > NUL");

                  process.StartInfo.Arguments = sb.ToString();
                  process.Start();

                  process.WaitForExit();

                  success = process.ExitCode == 0 || process.ExitCode == 1;
               }
            }
            catch (System.Exception ex)
            {
               success = false;
               string errorMessage = "Could not execute " + Constants.ASSET_NAME + "!" + System.Environment.NewLine + ex;
               Debug.LogError(errorMessage);
            }
         }

         EditorUtility.ClearProgressBar();

         return success;
      }

      private static string generateWindowsRestoreScript()
      {
         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         // setup
         sb.AppendLine("@echo off");
         sb.AppendLine("cls");

         // title
         sb.Append("title ");
         sb.Append(Constants.ASSET_NAME);
         sb.Append(" - Restore of ");
         sb.Append(Application.productName);
         sb.AppendLine(" in progress - DO NOT CLOSE THIS WINDOW!");

         // header
         sb.AppendLine("echo ##############################################################################");
         sb.AppendLine("echo #                                                                            #");
         sb.Append("echo #  ");
         sb.Append(Constants.ASSET_NAME);
         sb.Append(" ");
         sb.Append(Constants.ASSET_VERSION);
         sb.AppendLine(" - Windows                                       #");
         sb.AppendLine("echo #  Copyright 2018-2024 by www.crosstales.com                                 #");
         sb.AppendLine("echo #                                                                            #");
         sb.AppendLine("echo #  The files will now be restored from the backup destination.               #");
         sb.AppendLine("echo #  This will take some time, so please be patient and DON'T CLOSE THIS       #");
         sb.AppendLine("echo #  WINDOW before the process is finished!                                    #");
         sb.AppendLine("echo #                                                                            #");
         sb.AppendLine("echo #  Unity will restart automatically after the restore.                       #");
         sb.AppendLine("echo #                                                                            #");
         sb.AppendLine("echo ##############################################################################");
         sb.AppendLine("echo " + Application.productName);
         sb.AppendLine("echo.");
         sb.AppendLine("echo.");

         // check if Unity is closed
         sb.AppendLine(":waitloop");
         sb.Append("if not exist \"");
         sb.Append(Constants.APPLICATION_PATH);
         sb.Append("Temp\\UnityLockfile\" goto waitloopend");
         sb.AppendLine();
         sb.AppendLine("echo.");
         sb.AppendLine("echo Waiting for Unity to close...");
         sb.AppendLine("timeout /t 3");

         if (Config.DELETE_LOCKFILE)
         {
            sb.Append("del \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.Append("Temp\\UnityLockfile\" /q");
            sb.AppendLine();
         }

         sb.AppendLine("goto waitloop");
         sb.AppendLine(":waitloopend");

         // Restore files
         sb.AppendLine("echo.");
         sb.AppendLine("echo ##############################################################################");
         sb.AppendLine("echo #  Restoring files                                                           #");
         sb.AppendLine("echo ##############################################################################");

         // Assets
         if (Config.COPY_ASSETS)
         {
            sb.Append("robocopy \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("Assets\" \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.AppendLine($"Assets\" /MIR /W:2 /R:3 /MT:{Config.THREADS} /NFL /NDL /NJH /NJS /nc /ns /np > NUL");
         }

         // Library
         if (Config.COPY_LIBRARY)
         {
            sb.Append("robocopy \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("Library\" \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.AppendLine($"Library\" /MIR /W:2 /R:3 /MT:{Config.THREADS} /NFL /NDL /NJH /NJS /nc /ns /np > NUL");
         }

         // ProjectSettings
         if (Config.COPY_SETTINGS)
         {
            sb.Append("robocopy \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("ProjectSettings\" \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.AppendLine($"ProjectSettings\" /MIR /W:2 /R:3 /MT:{Config.THREADS} /NFL /NDL /NJH /NJS /nc /ns /np > NUL");
         }

         // UserSettings
         if (Config.COPY_USER_SETTINGS)
         {
            sb.Append("robocopy \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("UserSettings\" \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.AppendLine($"UserSettings\" /MIR /W:2 /R:3 /MT:{Config.THREADS} /NFL /NDL /NJH /NJS /nc /ns /np > NUL");
         }

         // Packages
         if (Config.COPY_PACKAGES)
         {
            sb.Append("robocopy \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("Packages\" \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.AppendLine($"Packages\" /MIR /W:2 /R:3 /MT:{Config.THREADS} /NFL /NDL /NJH /NJS /nc /ns /np > NUL");
         }

         // Restart Unity
         sb.AppendLine("echo.");
         sb.AppendLine("echo ##############################################################################");
         sb.AppendLine("echo #  Restarting Unity                                                          #");
         sb.AppendLine("echo ##############################################################################");
         sb.Append("start \"\" \"");
         sb.Append(Crosstales.Common.Util.FileHelper.ValidatePath(EditorApplication.applicationPath, false));
         sb.Append("\" -projectPath \"");
         sb.Append(Constants.APPLICATION_PATH.Substring(0, Constants.APPLICATION_PATH.Length - 1));
         sb.Append("\"");

         if (Config.BATCHMODE)
         {
            sb.Append(" -batchmode");

            if (Config.QUIT)
               sb.Append(" -quit");

            if (Config.NO_GRAPHICS)
               sb.Append(" -nographics");
         }

         if (!string.IsNullOrEmpty(Config.EXECUTE_METHOD_RESTORE))
         {
            sb.Append(" -executeMethod ");
            sb.Append(Config.EXECUTE_METHOD_RESTORE);
         }
         else
         {
            sb.Append(" -executeMethod Crosstales.TB.BAR.DefaultMethodAfterRestore");
         }

         sb.AppendLine();
         sb.AppendLine("echo.");

         // check if Unity is started
         sb.AppendLine(":waitloop2");
         sb.Append("if exist \"");
         sb.Append(Constants.APPLICATION_PATH);
         sb.Append("Temp\\UnityLockfile\" goto waitloopend2");
         sb.AppendLine();
         sb.AppendLine("echo Waiting for Unity to start...");
         sb.AppendLine("timeout /t 3");
         sb.AppendLine("goto waitloop2");
         sb.AppendLine(":waitloopend2");
         sb.AppendLine("echo.");
         sb.AppendLine("echo Bye!");
         sb.AppendLine("timeout /t 1");
         sb.AppendLine("exit");

         return sb.ToString();
      }

      #endregion


      #region Mac

      private static string generateMacBackupScript()
      {
         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         // setup
         sb.AppendLine("#!/bin/bash");
         sb.AppendLine("set +v");
         sb.AppendLine("clear");

         // title
         sb.Append("title='");
         sb.Append(Constants.ASSET_NAME);
         sb.Append(" - Backup of ");
         sb.Append(Application.productName);
         sb.AppendLine(" in progress - DO NOT CLOSE THIS WINDOW!'");
         sb.AppendLine("echo -n -e \"\\033]0;$title\\007\"");

         // header
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.Append("echo \"¦  ");
         sb.Append(Constants.ASSET_NAME);
         sb.Append(" ");
         sb.Append(Constants.ASSET_VERSION);
         sb.AppendLine(" - macOS                                         ¦\"");
         sb.AppendLine("echo \"¦  Copyright 2018-2024 by www.crosstales.com                                 ¦\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.AppendLine("echo \"¦  The files will now be saved to the backup destination.                    ¦\"");
         sb.AppendLine("echo \"¦  This will take some time, so please be patient and DON'T CLOSE THIS       ¦\"");
         sb.AppendLine("echo \"¦  WINDOW before the process is finished!                                    ¦\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.AppendLine("echo \"¦  Unity will restart automatically after the backup.                        ¦\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"" + Application.productName + "\"");
         sb.AppendLine("echo");
         sb.AppendLine("echo");

         // check if Unity is closed
         sb.Append("while [ -f \"");
         sb.Append(Constants.APPLICATION_PATH);
         sb.Append("Temp/UnityLockfile\" ]");
         sb.AppendLine();
         sb.AppendLine("do");
         sb.AppendLine("  echo \"Waiting for Unity to close...\"");
         sb.AppendLine("  sleep 3");

         if (Config.DELETE_LOCKFILE)
         {
            sb.Append("  rm \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.Append("Temp/UnityLockfile\"");
            sb.AppendLine();
         }

         sb.AppendLine("done");

         // Save files
         sb.AppendLine("echo");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"¦  Saving files                                                              ¦\"");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");

         // Assets
         if (Config.COPY_ASSETS)
         {
            sb.Append("mkdir -p \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("Assets");
            sb.Append('"');
            sb.AppendLine();
            sb.Append("rsync -aq --delete \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.Append("Assets\" \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.AppendLine("\"");
         }

         // Library
         if (Config.COPY_LIBRARY)
         {
            sb.Append("mkdir -p \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("Library");
            sb.Append('"');
            sb.AppendLine();
            sb.Append("rsync -aq --delete \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.Append("Library\" \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.AppendLine("\"");
         }

         // ProjectSettings
         if (Config.COPY_SETTINGS)
         {
            sb.Append("mkdir -p \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("ProjectSettings");
            sb.Append('"');
            sb.AppendLine();
            sb.Append("rsync -aq --delete \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.Append("ProjectSettings\" \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.AppendLine("\"");
         }

         // UserSettings
         if (Config.COPY_USER_SETTINGS)
         {
            sb.Append("mkdir -p \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("UserSettings");
            sb.Append('"');
            sb.AppendLine();
            sb.Append("rsync -aq --delete \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.Append("UserSettings\" \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.AppendLine("\"");
         }

         // Packages
         if (Config.COPY_PACKAGES)
         {
            sb.Append("mkdir -p \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("Packages");
            sb.Append('"');
            sb.AppendLine();
            sb.Append("rsync -aq --delete \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.Append("Packages\" \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.AppendLine("\"");
         }

         // Restart Unity
         sb.AppendLine("echo");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"¦  Restarting Unity                                                          ¦\"");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.Append("open -a \"");
         sb.Append(EditorApplication.applicationPath);
         sb.Append("\" --args -projectPath \"");
         sb.Append(Constants.APPLICATION_PATH);
         sb.Append("\"");

         if (Config.BATCHMODE)
         {
            sb.Append(" -batchmode");

            if (Config.QUIT)
               sb.Append(" -quit");

            if (Config.NO_GRAPHICS)
               sb.Append(" -nographics");
         }

         if (!string.IsNullOrEmpty(Config.EXECUTE_METHOD_BACKUP))
         {
            sb.Append(" -executeMethod ");
            sb.Append(Config.EXECUTE_METHOD_BACKUP);
         }
         else
         {
            sb.Append(" -executeMethod Crosstales.TB.BAR.DefaultMethodAfterBackup");
         }

         sb.AppendLine();

         //check if Unity is started
         sb.AppendLine("echo");
         sb.Append("while [ ! -f \"");
         sb.Append(Constants.APPLICATION_PATH);
         sb.Append("Temp/UnityLockfile\" ]");
         sb.AppendLine();
         sb.AppendLine("do");
         sb.AppendLine("  echo \"Waiting for Unity to start...\"");
         sb.AppendLine("  sleep 3");
         sb.AppendLine("done");
         sb.AppendLine("echo");
         sb.AppendLine("echo \"Bye!\"");
         sb.AppendLine("sleep 1");
         sb.AppendLine("exit");

         return sb.ToString();
      }

      private static string generateMacRestoreScript()
      {
         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         // setup
         sb.AppendLine("#!/bin/bash");
         sb.AppendLine("set +v");
         sb.AppendLine("clear");

         // title
         sb.Append("title='");
         sb.Append(Constants.ASSET_NAME);
         sb.Append(" - Restore of ");
         sb.Append(Application.productName);
         sb.AppendLine(" in progress - DO NOT CLOSE THIS WINDOW!'");
         sb.AppendLine("echo -n -e \"\\033]0;$title\\007\"");

         // header
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.Append("echo \"¦  ");
         sb.Append(Constants.ASSET_NAME);
         sb.Append(" ");
         sb.Append(Constants.ASSET_VERSION);
         sb.AppendLine(" - macOS                                         ¦\"");
         sb.AppendLine("echo \"¦  Copyright 2018-2024 by www.crosstales.com                                 ¦\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.AppendLine("echo \"¦  The files will now be restored from the backup destination.               ¦\"");
         sb.AppendLine("echo \"¦  This will take some time, so please be patient and DON'T CLOSE THIS       ¦\"");
         sb.AppendLine("echo \"¦  WINDOW before the process is finished!                                    ¦\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.AppendLine("echo \"¦  Unity will restart automatically after the restore.                       ¦\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"" + Application.productName + "\"");
         sb.AppendLine("echo");
         sb.AppendLine("echo");

         // check if Unity is closed
         sb.Append("while [ -f \"");
         sb.Append(Constants.APPLICATION_PATH);
         sb.Append("Temp/UnityLockfile\" ]");
         sb.AppendLine();
         sb.AppendLine("do");
         sb.AppendLine("  echo \"Waiting for Unity to close...\"");
         sb.AppendLine("  sleep 3");

         if (Config.DELETE_LOCKFILE)
         {
            sb.Append("  rm \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.Append("Temp/UnityLockfile\"");
            sb.AppendLine();
         }

         sb.AppendLine("done");

         // Restore files
         sb.AppendLine("echo");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"¦  Restoring files                                                           ¦\"");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");

         // Assets
         if (Config.COPY_ASSETS)
         {
            sb.Append("rsync -aq --delete \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("Assets");
            sb.Append("/\" \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.AppendLine("Assets/\"");
         }

         // Library
         if (Config.COPY_LIBRARY)
         {
            sb.Append("rsync -aq --delete \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("Library");
            sb.Append("/\" \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.AppendLine("Library/\"");
         }

         // ProjectSettings
         if (Config.COPY_SETTINGS)
         {
            sb.Append("rsync -aq --delete \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("ProjectSettings");
            sb.Append("/\" \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.AppendLine("ProjectSettings/\"");
         }

         // UserSettings
         if (Config.COPY_USER_SETTINGS)
         {
            sb.Append("rsync -aq --delete \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("UserSettings");
            sb.Append("/\" \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.AppendLine("UserSettings/\"");
         }

         // Packages
         if (Config.COPY_PACKAGES)
         {
            sb.Append("rsync -aq --delete \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("Packages");
            sb.Append("/\" \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.AppendLine("Packages/\"");
         }

         // Restart Unity
         sb.AppendLine("echo");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"¦  Restarting Unity                                                          ¦\"");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.Append("open -a \"");
         sb.Append(EditorApplication.applicationPath);
         sb.Append("\" --args -projectPath \"");
         sb.Append(Constants.APPLICATION_PATH);
         sb.Append("\"");

         if (Config.BATCHMODE)
         {
            sb.Append(" -batchmode");

            if (Config.QUIT)
               sb.Append(" -quit");

            if (Config.NO_GRAPHICS)
               sb.Append(" -nographics");
         }

         if (!string.IsNullOrEmpty(Config.EXECUTE_METHOD_RESTORE))
         {
            sb.Append(" -executeMethod ");
            sb.Append(Config.EXECUTE_METHOD_RESTORE);
         }
         else
         {
            sb.Append(" -executeMethod Crosstales.TB.BAR.DefaultMethodAfterRestore");
         }

         sb.AppendLine();

         //check if Unity is started
         sb.AppendLine("echo");
         sb.Append("while [ ! -f \"");
         sb.Append(Constants.APPLICATION_PATH);
         sb.Append("Temp/UnityLockfile\" ]");
         sb.AppendLine();
         sb.AppendLine("do");
         sb.AppendLine("  echo \"Waiting for Unity to start...\"");
         sb.AppendLine("  sleep 3");
         sb.AppendLine("done");
         sb.AppendLine("echo");
         sb.AppendLine("echo \"Bye!\"");
         sb.AppendLine("sleep 1");
         sb.AppendLine("exit");

         return sb.ToString();
      }

      #endregion


      #region Linux

      private static void scanUnix(string path, ref string key)
      {
         using (System.Diagnostics.Process scanProcess = new System.Diagnostics.Process())
         {
            string args = "-sch \"" + path + '"';

            if (Config.DEBUG)
               Debug.Log("Process arguments: '" + args + "'");

            System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>();

            scanProcess.StartInfo.FileName = "du";
            scanProcess.StartInfo.Arguments = args;
            scanProcess.StartInfo.CreateNoWindow = true;
            scanProcess.StartInfo.RedirectStandardOutput = true;
            scanProcess.StartInfo.RedirectStandardError = true;
            scanProcess.StartInfo.StandardOutputEncoding = System.Text.Encoding.Default;
            scanProcess.StartInfo.UseShellExecute = false;

            bool success = true;

            try
            {
               scanProcess.Start();
               //scanProcess.BeginOutputReadLine();
            }
            catch (System.Exception ex)
            {
               success = false;
               Debug.LogError("Could not start the scan process!" + System.Environment.NewLine + ex);
            }

            if (success)
            {
               while (!scanProcess.HasExited)
               {
                  System.Threading.Thread.Sleep(100);
               }

               if (scanProcess.ExitCode == 0)
               {
                  if (Config.DEBUG)
                     Debug.LogWarning("Scan completed: " + result.Count);

                  using (System.IO.StreamReader sr = scanProcess.StandardOutput)
                  {
                     result.AddRange(SplitStringToLines(sr.ReadToEnd()));
                  }

                  if (result.Count >= 2)
                  {
                     key = result[result.Count - 1].Trim();
                  }
                  else
                  {
                     Debug.LogWarning("Scan problem; not enough lines were returned: " + result.Count);
                     key = "Scan problem";
                  }
               }
               else
               {
                  using (System.IO.StreamReader sr = scanProcess.StandardError)
                  {
                     Debug.LogError("Could not scan the path: " + scanProcess.ExitCode + System.Environment.NewLine + sr.ReadToEnd());
                  }
               }
            }
         }

         isScanning = false;
      }

      private static bool backupUnix()
      {
         bool success = true;

         using (System.Diagnostics.Process process = new System.Diagnostics.Process())
         {
            try
            {
               process.StartInfo.CreateNoWindow = true;
               process.StartInfo.UseShellExecute = false;
               process.StartInfo.FileName = "rsync";

               // Save Assets
               if (Config.COPY_ASSETS)
               {
                  EditorUtility.DisplayProgressBar("Backup", "Save Assets...", 0.25f);

                  string savePath = Config.PATH_BACKUP_SLOT + "Assets";

                  if (!Crosstales.Common.Util.FileHelper.ExistsDirectory(savePath))
                     Crosstales.Common.Util.FileHelper.CreateDirectory(savePath);

                  System.Text.StringBuilder sb = new System.Text.StringBuilder();
                  sb.Append("-aq --delete \"");
                  sb.Append(Constants.APPLICATION_PATH);
                  sb.Append("Assets/\" \"");
                  sb.Append(Config.PATH_BACKUP_SLOT);
                  sb.Append("Assets/\"");

                  process.StartInfo.Arguments = sb.ToString();
                  process.Start();

                  process.WaitForExit();

                  success = process.ExitCode == 0;
               }

/*
               // Save Library
               if (Config.COPY_LIBRARY && success)
               {
                  EditorUtility.DisplayProgressBar("Backup", "Save Library...", 0.5f);

                  string savePath = Config.PATH_BACKUP + "Library";

                  if (!System.IO.Directory.Exists(savePath))
                     System.IO.Directory.CreateDirectory(savePath);

                  System.Text.StringBuilder sb = new System.Text.StringBuilder();
                  sb.Append("-aq --delete \"");
                  sb.Append(Constants.APPLICATION_PATH);
                  sb.Append("Library/\" \"");
                  sb.Append(Config.PATH_BACKUP);
                  sb.Append("Library/\"");

                  process.StartInfo.Arguments = sb.ToString();
                  process.Start();

                  process.WaitForExit();

                  success = process.ExitCode == 0;
               }
*/
               // Save ProjectSettings
               if (Config.COPY_SETTINGS && success)
               {
                  EditorUtility.DisplayProgressBar("Backup", "Save ProjectSettings...", 0.75f);

                  string savePath = Config.PATH_BACKUP_SLOT + "ProjectSettings";

                  if (!Crosstales.Common.Util.FileHelper.ExistsDirectory(savePath))
                     Crosstales.Common.Util.FileHelper.CreateDirectory(savePath);

                  System.Text.StringBuilder sb = new System.Text.StringBuilder();
                  sb.Append("-aq --delete \"");
                  sb.Append(Constants.APPLICATION_PATH);
                  sb.Append("ProjectSettings/\" \"");
                  sb.Append(Config.PATH_BACKUP_SLOT);
                  sb.Append("ProjectSettings/\"");

                  process.StartInfo.Arguments = sb.ToString();
                  process.Start();

                  process.WaitForExit();

                  success = process.ExitCode == 0;
               }

               // Save UserSettings
               if (Config.COPY_USER_SETTINGS && success)
               {
                  EditorUtility.DisplayProgressBar("Backup", "Save UserSettings...", 0.8f);

                  string savePath = Config.PATH_BACKUP_SLOT + "UserSettings";

                  if (!Crosstales.Common.Util.FileHelper.ExistsDirectory(savePath))
                     Crosstales.Common.Util.FileHelper.CreateDirectory(savePath);

                  System.Text.StringBuilder sb = new System.Text.StringBuilder();
                  sb.Append("-aq --delete \"");
                  sb.Append(Constants.APPLICATION_PATH);
                  sb.Append("UserSettings/\" \"");
                  sb.Append(Config.PATH_BACKUP_SLOT);
                  sb.Append("UserSettings/\"");

                  process.StartInfo.Arguments = sb.ToString();
                  process.Start();

                  process.WaitForExit();

                  success = process.ExitCode == 0;
               }

               // Save Packages
               if (Config.COPY_PACKAGES && success)
               {
                  EditorUtility.DisplayProgressBar("Backup", "Save Packages...", 1f);

                  string savePath = Config.PATH_BACKUP_SLOT + "Packages";

                  if (!Crosstales.Common.Util.FileHelper.ExistsDirectory(savePath))
                     Crosstales.Common.Util.FileHelper.CreateDirectory(savePath);

                  System.Text.StringBuilder sb = new System.Text.StringBuilder();
                  sb.Append("-aq --delete \"");
                  sb.Append(Constants.APPLICATION_PATH);
                  sb.Append("Packages/\" \"");
                  sb.Append(Config.PATH_BACKUP_SLOT);
                  sb.Append("Packages/\"");

                  process.StartInfo.Arguments = sb.ToString();
                  process.Start();

                  process.WaitForExit();

                  success = process.ExitCode == 0;
               }
            }
            catch (System.Exception ex)
            {
               success = false;
               string errorMessage = "Could not execute " + Constants.ASSET_NAME + "!" + System.Environment.NewLine + ex;
               Debug.LogError(errorMessage);
            }
         }

         EditorUtility.ClearProgressBar();

         return success;
      }

      private static string generateLinuxBackupScript()
      {
         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         // setup
         sb.AppendLine("#!/bin/bash");
         sb.AppendLine("set +v");
         sb.AppendLine("clear");

         // title
         sb.Append("title='");
         sb.Append(Constants.ASSET_NAME);
         sb.Append(" - Backup of ");
         sb.Append(Application.productName);
         sb.AppendLine(" in progress - DO NOT CLOSE THIS WINDOW!'");
         sb.AppendLine("echo -n -e \"\\033]0;$title\\007\"");

         // header
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.Append("echo \"¦  ");
         sb.Append(Constants.ASSET_NAME);
         sb.Append(" ");
         sb.Append(Constants.ASSET_VERSION);
         sb.AppendLine(" - Linux                                         ¦\"");
         sb.AppendLine("echo \"¦  Copyright 2018-2024 by www.crosstales.com                                 ¦\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.AppendLine("echo \"¦  The files will now be saved to the backup destination.                    ¦\"");
         sb.AppendLine("echo \"¦  This will take some time, so please be patient and DON'T CLOSE THIS       ¦\"");
         sb.AppendLine("echo \"¦  WINDOW before the process is finished!                                    ¦\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.AppendLine("echo \"¦  Unity will restart automatically after the backup.                        ¦\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"" + Application.productName + "\"");
         sb.AppendLine("echo");
         sb.AppendLine("echo");

         // check if Unity is closed
         sb.Append("while [ -f \"");
         sb.Append(Constants.APPLICATION_PATH);
         sb.Append("Temp/UnityLockfile\" ]");
         sb.AppendLine();
         sb.AppendLine("do");
         sb.AppendLine("  echo \"Waiting for Unity to close...\"");
         sb.AppendLine("  sleep 3");

         if (Config.DELETE_LOCKFILE)
         {
            sb.Append("  rm \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.Append("Temp/UnityLockfile\"");
            sb.AppendLine();
         }

         sb.AppendLine("done");

         // Save files
         sb.AppendLine("echo");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"¦  Saving files                                                              ¦\"");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");

         // Assets
         if (Config.COPY_ASSETS)
         {
            sb.Append("mkdir -p \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("Assets");
            sb.Append('"');
            sb.AppendLine();
            sb.Append("rsync -aq --delete \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.Append("Assets/\" \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            //sb.Append("\"");
            sb.AppendLine("Assets/\"");
         }

         // Library
         if (Config.COPY_LIBRARY)
         {
            sb.Append("mkdir -p \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("Library");
            sb.Append('"');
            sb.AppendLine();
            sb.Append("rsync -aq --delete \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.Append("Library/\" \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            //sb.Append("\"");
            sb.AppendLine("Library/\"");
         }

         // ProjectSettings
         if (Config.COPY_SETTINGS)
         {
            sb.Append("mkdir -p \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("ProjectSettings");
            sb.Append('"');
            sb.AppendLine();
            sb.Append("rsync -aq --delete \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.Append("ProjectSettings/\" \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            //sb.Append("\"");
            sb.AppendLine("ProjectSettings/\"");
         }

         // UserSettings
         if (Config.COPY_USER_SETTINGS)
         {
            sb.Append("mkdir -p \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("UserSettings");
            sb.Append('"');
            sb.AppendLine();
            sb.Append("rsync -aq --delete \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.Append("UserSettings/\" \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            //sb.Append("\"");
            sb.AppendLine("UserSettings/\"");
         }

         // Packages
         if (Config.COPY_PACKAGES)
         {
            sb.Append("mkdir -p \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("Packages");
            sb.Append('"');
            sb.AppendLine();
            sb.Append("rsync -aq --delete \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.Append("Packages/\" \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            //sb.Append("\"");
            sb.AppendLine("Packages/\"");
         }

         // Restart Unity
         sb.AppendLine("echo");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"¦  Restarting Unity                                                          ¦\"");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         //sb.Append("nohup \"");
         sb.Append('"');
         sb.Append(EditorApplication.applicationPath);
         sb.Append("\" --args -projectPath \"");
         sb.Append(Constants.APPLICATION_PATH);
         sb.Append("\"");

         if (Config.BATCHMODE)
         {
            sb.Append(" -batchmode");

            if (Config.QUIT)
               sb.Append(" -quit");

            if (Config.NO_GRAPHICS)
               sb.Append(" -nographics");
         }

         if (!string.IsNullOrEmpty(Config.EXECUTE_METHOD_BACKUP))
         {
            sb.Append(" -executeMethod ");
            sb.Append(Config.EXECUTE_METHOD_BACKUP);
         }
         else
         {
            sb.Append(" -executeMethod Crosstales.TB.BAR.DefaultMethodAfterBackup");
         }

         sb.Append(" &");
         sb.AppendLine();

         // check if Unity is started
         sb.AppendLine("echo");
         sb.Append("while [ ! -f \"");
         sb.Append(Constants.APPLICATION_PATH);
         sb.Append("Temp/UnityLockfile\" ]");
         sb.AppendLine();
         sb.AppendLine("do");
         sb.AppendLine("  echo \"Waiting for Unity to start...\"");
         sb.AppendLine("  sleep 3");
         sb.AppendLine("done");
         sb.AppendLine("echo");
         sb.AppendLine("echo \"Bye!\"");
         sb.AppendLine("sleep 1");
         sb.AppendLine("exit");

         return sb.ToString();
      }

      private static bool restoreUnix()
      {
         bool success = true;

         using (System.Diagnostics.Process process = new System.Diagnostics.Process())
         {
            try
            {
               process.StartInfo.CreateNoWindow = true;
               process.StartInfo.UseShellExecute = false;
               process.StartInfo.FileName = "rsync";

               // Restore Assets
               if (Config.COPY_ASSETS)
               {
                  EditorUtility.DisplayProgressBar("Restore", "Restore Assets...", 0.25f);

                  System.Text.StringBuilder sb = new System.Text.StringBuilder();
                  sb.Append("-aq --delete \"");
                  sb.Append(Config.PATH_BACKUP_SLOT);
                  sb.Append("Assets");
                  sb.Append("/\" \"");
                  sb.Append(Constants.APPLICATION_PATH);
                  sb.Append("Assets/\"");

                  process.StartInfo.Arguments = sb.ToString();
                  process.Start();

                  process.WaitForExit();

                  success = process.ExitCode == 0;
               }

/*
               // Restore Library
               if (Config.COPY_LIBRARY && success)
               {
                  EditorUtility.DisplayProgressBar("Restore", "Restore Library...", 0.5f);

                  System.Text.StringBuilder sb = new System.Text.StringBuilder();
                  sb.Append("-aq --delete \"");
                  sb.Append(Config.PATH_BACKUP);
                  sb.Append("Library");
                  sb.Append("/\" \"");
                  sb.Append(Constants.APPLICATION_PATH);
                  sb.Append("Library/\"");

                  process.StartInfo.Arguments = sb.ToString();
                  process.Start();

                  process.WaitForExit();

                  success = process.ExitCode == 0;
               }
*/
               // Restore ProjectSettings
               if (Config.COPY_SETTINGS && success)
               {
                  EditorUtility.DisplayProgressBar("Restore", "Restore ProjectSettings...", 0.75f);

                  System.Text.StringBuilder sb = new System.Text.StringBuilder();
                  sb.Append("-aq --delete \"");
                  sb.Append(Config.PATH_BACKUP_SLOT);
                  sb.Append("ProjectSettings");
                  sb.Append("/\" \"");
                  sb.Append(Constants.APPLICATION_PATH);
                  sb.Append("ProjectSettings/\"");

                  process.StartInfo.Arguments = sb.ToString();
                  process.Start();

                  process.WaitForExit();

                  success = process.ExitCode == 0;
               }

               // Restore UserSettings
               if (Config.COPY_USER_SETTINGS && success)
               {
                  EditorUtility.DisplayProgressBar("Restore", "Restore UserSettings...", 0.8f);

                  System.Text.StringBuilder sb = new System.Text.StringBuilder();
                  sb.Append("-aq --delete \"");
                  sb.Append(Config.PATH_BACKUP_SLOT);
                  sb.Append("UserSettings");
                  sb.Append("/\" \"");
                  sb.Append(Constants.APPLICATION_PATH);
                  sb.Append("UserSettings/\"");

                  process.StartInfo.Arguments = sb.ToString();
                  process.Start();

                  process.WaitForExit();

                  success = process.ExitCode == 0;
               }

               // Restore Packages
               if (Config.COPY_PACKAGES && success)
               {
                  EditorUtility.DisplayProgressBar("Restore", "Restore Packages...", 1f);

                  System.Text.StringBuilder sb = new System.Text.StringBuilder();
                  sb.Append("-aq --delete \"");
                  sb.Append(Config.PATH_BACKUP_SLOT);
                  sb.Append("Packages");
                  sb.Append("/\" \"");
                  sb.Append(Constants.APPLICATION_PATH);
                  sb.Append("Packages/\"");

                  process.StartInfo.Arguments = sb.ToString();
                  process.Start();

                  process.WaitForExit();

                  success = process.ExitCode == 0;
               }
            }
            catch (System.Exception ex)
            {
               success = false;
               string errorMessage = "Could not execute " + Constants.ASSET_NAME + "!" + System.Environment.NewLine + ex;
               Debug.LogError(errorMessage);
            }
         }

         EditorUtility.ClearProgressBar();

         return success;
      }

      private static string generateLinuxRestoreScript()
      {
         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         // setup
         sb.AppendLine("#!/bin/bash");
         sb.AppendLine("set +v");
         sb.AppendLine("clear");

         // title
         sb.Append("title='");
         sb.Append(Constants.ASSET_NAME);
         sb.Append(" - Restore of ");
         sb.Append(Application.productName);
         sb.AppendLine(" in progress - DO NOT CLOSE THIS WINDOW!'");
         sb.AppendLine("echo -n -e \"\\033]0;$title\\007\"");

         // header
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.Append("echo \"¦  ");
         sb.Append(Constants.ASSET_NAME);
         sb.Append(" ");
         sb.Append(Constants.ASSET_VERSION);
         sb.AppendLine(" - Linux                                         ¦\"");
         sb.AppendLine("echo \"¦  Copyright 2018-2024 by www.crosstales.com                                 ¦\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.AppendLine("echo \"¦  The files will now be restored from the backup destination.               ¦\"");
         sb.AppendLine("echo \"¦  This will take some time, so please be patient and DON'T CLOSE THIS       ¦\"");
         sb.AppendLine("echo \"¦  WINDOW before the process is finished!                                    ¦\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.AppendLine("echo \"¦  Unity will restart automatically after the restore.                       ¦\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"" + Application.productName + "\"");
         sb.AppendLine("echo");
         sb.AppendLine("echo");

         // check if Unity is closed
         sb.Append("while [ -f \"");
         sb.Append(Constants.APPLICATION_PATH);
         sb.Append("Temp/UnityLockfile\" ]");
         sb.AppendLine();
         sb.AppendLine("do");
         sb.AppendLine("  echo \"Waiting for Unity to close...\"");
         sb.AppendLine("  sleep 3");

         if (Config.DELETE_LOCKFILE)
         {
            sb.Append("  rm \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.Append("Temp/UnityLockfile\"");
            sb.AppendLine();
         }

         sb.AppendLine("done");

         // Restore files
         sb.AppendLine("echo");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"¦  Restoring files                                                           ¦\"");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");

         // Assets
         if (Config.COPY_ASSETS)
         {
            sb.Append("rsync -aq --delete \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("Assets");
            sb.Append("/\" \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.AppendLine("Assets/\"");
         }

         // Library
         if (Config.COPY_LIBRARY)
         {
            sb.Append("rsync -aq --delete \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("Library");
            sb.Append("/\" \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.AppendLine("Library/\"");
         }

         // ProjectSettings
         if (Config.COPY_SETTINGS)
         {
            sb.Append("rsync -aq --delete \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("ProjectSettings");
            sb.Append("/\" \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.AppendLine("ProjectSettings/\"");
         }

         // ProjectSettings
         if (Config.COPY_USER_SETTINGS)
         {
            sb.Append("rsync -aq --delete \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("UserSettings");
            sb.Append("/\" \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.AppendLine("UserSettings/\"");
         }

         // Packages
         if (Config.COPY_PACKAGES)
         {
            sb.Append("rsync -aq --delete \"");
            sb.Append(Config.PATH_BACKUP_SLOT);
            sb.Append("Packages");
            sb.Append("/\" \"");
            sb.Append(Constants.APPLICATION_PATH);
            sb.AppendLine("Packages/\"");
         }

         // Restart Unity
         sb.AppendLine("echo");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"¦  Restarting Unity                                                          ¦\"");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         //sb.Append("nohup \"");
         sb.Append('"');
         sb.Append(EditorApplication.applicationPath);
         sb.Append("\" --args -projectPath \"");
         sb.Append(Constants.APPLICATION_PATH);
         sb.Append("\"");

         if (Config.BATCHMODE)
         {
            sb.Append(" -batchmode");

            if (Config.QUIT)
               sb.Append(" -quit");

            if (Config.NO_GRAPHICS)
               sb.Append(" -nographics");
         }

         if (!string.IsNullOrEmpty(Config.EXECUTE_METHOD_RESTORE))
         {
            sb.Append(" -executeMethod ");
            sb.Append(Config.EXECUTE_METHOD_RESTORE);
         }
         else
         {
            sb.Append(" -executeMethod Crosstales.TB.BAR.DefaultMethodAfterRestore");
         }

         sb.Append(" &");
         sb.AppendLine();

         // check if Unity is started
         sb.AppendLine("echo");
         sb.Append("while [ ! -f \"");
         sb.Append(Constants.APPLICATION_PATH);
         sb.Append("Temp/UnityLockfile\" ]");
         sb.AppendLine();
         sb.AppendLine("do");
         sb.AppendLine("  echo \"Waiting for Unity to start...\"");
         sb.AppendLine("  sleep 3");
         sb.AppendLine("done");
         sb.AppendLine("echo");
         sb.AppendLine("echo \"Bye!\"");
         sb.AppendLine("sleep 1");
         sb.AppendLine("exit");

         return sb.ToString();
      }

      #endregion

      /// <summary>Loads an image as Texture2D from 'Editor Default Resources'.</summary>
      /// <param name="logo">Logo to load.</param>
      /// <param name="fileName">Name of the image.</param>
      /// <returns>Image as Texture2D from 'Editor Default Resources'.</returns>
      private static Texture2D loadImage(ref Texture2D logo, string fileName)
      {
         if (logo == null)
         {
#if CT_DEVELOP
            logo = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets" + Config.ASSET_PATH + "Icons/" + fileName, typeof(Texture2D));
#else
                logo = (Texture2D)EditorGUIUtility.Load("crosstales/TurboBackup/" + fileName);
#endif

            if (logo == null)
            {
               Debug.LogWarning("Image not found: " + fileName);
            }
         }

         return logo;
      }

      #endregion
   }
}
#endif
// © 2018-2024 crosstales LLC (https://www.crosstales.com)