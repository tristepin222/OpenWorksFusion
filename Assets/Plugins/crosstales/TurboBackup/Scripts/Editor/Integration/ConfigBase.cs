#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Crosstales.TB.Util;
using Crosstales.TB.Task;
using Crosstales.TB.EditorTask;
using System.Runtime.InteropServices;
using Crosstales.Common.Util;

namespace Crosstales.TB.EditorIntegration
{
   /// <summary>Base class for editor windows.</summary>
   public abstract class ConfigBase : EditorWindow
   {
      #region Variables

      private static string updateText = UpdateCheck.TEXT_NOT_CHECKED;
      private static UpdateStatus updateStatus = UpdateStatus.NOT_CHECKED;

      private System.Threading.Thread worker;

      private Vector2 scrollPosBAR;
      private Vector2 scrollPosConfig;
      private Vector2 scrollPosHelp;
      private Vector2 scrollPosAboutUpdate;
      private Vector2 scrollPosAboutReadme;
      private Vector2 scrollPosAboutVersions;

      private static string readme;
      private static string versions;

      private int aboutTab;

      private const int space = 4;

      private static readonly string[] vcsOptions = { "None", "git", "SVN", "Mercurial", "Collab", "PlasticSCM" };

      private static readonly System.Random rnd = new System.Random();

      private readonly int adRnd1 = rnd.Next(0, 3);
      private readonly int adRnd2 = rnd.Next(0, 3);
      private readonly int adRnd3 = rnd.Next(0, 3);

      #endregion


      #region Protected methods

      protected void showBAR()
      {
         if (!Application.isPlaying)
         {
            if (!EditorApplication.isCompiling && !EditorApplication.isUpdating)
            {
               if (!BAR.isBusy)
               {
                  scrollPosBAR = EditorGUILayout.BeginScrollView(scrollPosBAR, false, false);

                  GUILayout.Space(3);

                  if (Config.SLOTS > 1)
                  {
                     GUILayout.Label("Slot", EditorStyles.boldLabel);

                     string[] slotStrings = new string[Config.SLOTS];

                     for (int ii = 0; ii < Config.SLOTS; ii++)
                     {
                        slotStrings[ii] = (ii + 1).ToString();
                     }

                     if (Config.CURRENT_SLOT < 1 || Config.CURRENT_SLOT > Config.SLOTS)
                        Config.CURRENT_SLOT = 1;

                     int newSlot = GUILayout.Toolbar(Config.CURRENT_SLOT - 1, slotStrings) + 1;

                     if (newSlot != Config.CURRENT_SLOT)
                        GUI.FocusControl(null);

                     Config.CURRENT_SLOT = newSlot;

                     Helper.SeparatorUI();
                  }

                  GUILayout.Label("Backup", EditorStyles.boldLabel);

                  if (Helper.isBackupEnabled)
                  {
                     GUI.enabled = !Helper.isDeleting;

                     Config.BACKUP_NOTE_NEW = EditorGUILayout.TextField(new GUIContent("Note", "Optional note for the backup."), Config.BACKUP_NOTE_NEW);

                     GUILayout.Space(3);

                     if (GUILayout.Button(new GUIContent(" Backup", Helper.Action_Backup, "Backup the project")))
                     {
                        if (!Config.CONFIRM_BACKUP || EditorUtility.DisplayDialog("Backup the project?",
                               (Config.USE_LEGACY ? Constants.ASSET_NAME + " will now close Unity and save the following folders: " : "Save the following folders: ") + System.Environment.NewLine +
                               (Config.COPY_ASSETS ? "• Assets" + System.Environment.NewLine : string.Empty) +
                               (Config.COPY_LIBRARY && Config.USE_LEGACY ? "• Library" + System.Environment.NewLine : string.Empty) +
                               (Config.COPY_SETTINGS ? "• ProjectSettings" + System.Environment.NewLine : string.Empty) +
                               (Config.COPY_PACKAGES ? "• Packages" + System.Environment.NewLine : string.Empty) +
                               (Config.COPY_USER_SETTINGS ? "• UserSettings" + System.Environment.NewLine : string.Empty) +
                               System.Environment.NewLine +
                               "Backup directory: " + Config.PATH_BACKUP_SLOT +
                               System.Environment.NewLine +
                               System.Environment.NewLine +
                               "This operation could take some time." + System.Environment.NewLine + System.Environment.NewLine + "Would you like to start the backup?", "Yes", "No"))
                        {
                           if (Config.DEBUG)
                              Debug.Log("Backup initiated");

                           BAR.Backup();

                           GUIUtility.ExitGUI();
                        }
                     }

                     //GUILayout.Label($"Last Backup:\t{(Helper.hasBackupSlot ? $"{Config.BACKUP_DATE} (#{Config.BACKUP_COUNT})" : "never")}");
                     GUILayout.Label($"Last Backup:\t{(Config.BACKUP_DATE.Year > 2020 ? $"{Config.BACKUP_DATE}" : "never")}");

                     if (!string.IsNullOrEmpty(Config.BACKUP_NOTE))
                        GUILayout.Label($"Note:\t\t{Config.BACKUP_NOTE}");

                     GUILayout.Label($"Auto Backup:\t{(AutoBackup.BackupInterval > 0 ? $"{Config.AUTO_BACKUP_DATE.ToString()} (in {Helper.FormatSecondsToHRF((Config.AUTO_BACKUP_DATE - System.DateTime.Now).TotalSeconds)})" : "disabled")}");

                     Helper.SeparatorUI();

                     GUILayout.Label("Restore", EditorStyles.boldLabel);

                     if (Helper.hasBackupSlot)
                     {
                        if (GUILayout.Button(new GUIContent(" Restore", Helper.Action_Restore, "Restore the project")))
                        {
                           if (!Config.CONFIRM_RESTORE || EditorUtility.DisplayDialog("Restore the project?",
                                  (Config.USE_LEGACY ? Constants.ASSET_NAME + " will now close Unity and restore the following folders: " : "Restore the following folders: ") + System.Environment.NewLine +
                                  (Config.COPY_ASSETS ? "• Assets" + System.Environment.NewLine : string.Empty) +
                                  (Config.COPY_LIBRARY && Config.USE_LEGACY ? "• Library" + System.Environment.NewLine : string.Empty) +
                                  (Config.COPY_SETTINGS ? "• ProjectSettings" + System.Environment.NewLine : string.Empty) +
                                  (Config.COPY_PACKAGES ? "• Packages" + System.Environment.NewLine : string.Empty) +
                                  (Config.COPY_USER_SETTINGS ? "• UserSettings" + System.Environment.NewLine : string.Empty) +
                                  System.Environment.NewLine +
                                  "Backup directory: " + Config.PATH_BACKUP_SLOT +
                                  System.Environment.NewLine +
                                  System.Environment.NewLine +
                                  "This operation could take some time." + System.Environment.NewLine + System.Environment.NewLine + "Would you like to start the restore?", "Yes", "No"))
                           {
                              if (!Config.CONFIRM_RESTORE || !Config.CONFIRM_WARNING || !EditorUtility.DisplayDialog("Overwrite existing project?",
                                     "This operation will overwrite ALL files. Any progress since the last backup will BE LOST!" + System.Environment.NewLine + System.Environment.NewLine + "Would you really want to continue?", "NO!", "Yes"))
                              {
                                 if (Config.DEBUG)
                                    Debug.Log("Restore initiated");

                                 BAR.Restore();

                                 GUIUtility.ExitGUI();
                              }
                           }
                        }

                        GUILayout.Label("Last Restore:\t" + (Config.RESTORE_DATE.Year > 2020 ? Config.RESTORE_DATE.ToString() : "never"));
                     }
                     else
                     {
                        EditorGUILayout.HelpBox("No backup found, restore is not possible. Please use 'Backup' first.", MessageType.Info);
                     }

                     //TODO re-enable later?
                     /*
                                          if (Config.SLOTS > 1)
                                          {
                                             Helper.SeparatorUI();

                                             GUI.enabled = Helper.hasBackupSlot && !Helper.isDeleting;

                                             if (GUILayout.Button(new GUIContent($" Show Backup", Helper.Icon_Show, $"Show the backup from slot {Config.CURRENT_SLOT}.")))
                                             {
                                                Crosstales.Common.Util.FileHelper.ShowFile($"{Config.PATH_BACKUP_SLOT}");
                                             }

                                             if (GUILayout.Button(new GUIContent($" Delete Backup", Helper.Icon_Delete, $"Delete the backup from slot {Config.CURRENT_SLOT}")))
                                             {
                                                if (EditorUtility.DisplayDialog($"Delete the backup from slot {Config.CURRENT_SLOT}?", $"If you delete the backup from slot {Config.CURRENT_SLOT}, {Constants.ASSET_NAME} must store all data for the next backup.{System.Environment.NewLine}This operation could take some time.{System.Environment.NewLine}{System.Environment.NewLine}Would you like to delete the backup?", "Yes", "No"))
                                                {
                                                   Helper.DeleteBackupSlot(Config.CURRENT_SLOT);

                                                   if (Config.DEBUG)
                                                      Debug.Log($"Backup from slot {Config.CURRENT_SLOT} deleted");
                                                }
                                             }
                                             GUI.enabled = true;
                                          }
                     */

                     EditorGUILayout.EndScrollView();
                  }
                  else
                  {
                     EditorGUILayout.HelpBox("All backup folders are disabled. No actions possible.", MessageType.Error);
                  }
               }
               else
               {
                  EditorGUILayout.HelpBox($"{Constants.ASSET_NAME} is busy, please wait...", MessageType.Info);
               }
            }
            else
            {
               EditorGUILayout.HelpBox("Unity Editor is busy, please wait...", MessageType.Info);
            }
         }
         else
         {
            EditorGUILayout.HelpBox("Disabled in Play-mode!", MessageType.Info);
         }
      }

      protected void showConfiguration()
      {
         scrollPosConfig = EditorGUILayout.BeginScrollView(scrollPosConfig, false, false);
         {
            GUILayout.Label("General Settings", EditorStyles.boldLabel);
            Config.CUSTOM_PATH_BACKUP = EditorGUILayout.BeginToggleGroup(new GUIContent("Custom Backup Path", "Enable or disable a custom backup path (default: " + Constants.DEFAULT_CUSTOM_PATH_BACKUP + ")."), Config.CUSTOM_PATH_BACKUP);
            {
               EditorGUI.indentLevel++;

               EditorGUILayout.BeginHorizontal();
               {
                  EditorGUILayout.SelectableLabel(Config.PATH_BACKUP);

                  if (GUILayout.Button(new GUIContent(" Select", Helper.Icon_Folder, "Select path for the backup")))
                  {
                     string path = EditorUtility.OpenFolderPanel("Select path for the backup", Config.PATH_BACKUP.Substring(0, Config.PATH_BACKUP.Length - (Constants.BACKUP_DIRNAME.Length + 1)), string.Empty);

                     if (!string.IsNullOrEmpty(path))
                        Config.PATH_BACKUP = FileHelper.isRoot(path) ? path + Constants.BACKUP_DIRNAME : path + "/" + Constants.BACKUP_DIRNAME;
                  }
               }
               EditorGUILayout.EndHorizontal();

               EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndToggleGroup();

            //GUILayout.Space(space);
            GUI.enabled = !Config.CUSTOM_PATH_BACKUP;

            Config.VCS = EditorGUILayout.Popup("Version Control", Config.VCS, vcsOptions);

            GUILayout.Space(space);
            GUI.enabled = true;

            Config.USE_LEGACY = EditorGUILayout.BeginToggleGroup(new GUIContent("Legacy Mode", "Enable or disable legacy mode. If enabled, backup&restore will close and restart Unity (default: " + Constants.DEFAULT_USE_LEGACY + ")."), Config.USE_LEGACY);
            {
               EditorGUI.indentLevel++;

               Config.DELETE_LOCKFILE = EditorGUILayout.Toggle(new GUIContent("Delete UnityLockfile", "Enable or disable deleting the 'UnityLockfile' (default: " + Constants.DEFAULT_DELETE_LOCKFILE + ")."), Config.DELETE_LOCKFILE);

               EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndToggleGroup();

            GUILayout.Space(space);

            Config.BATCHMODE = EditorGUILayout.BeginToggleGroup(new GUIContent("Batch Mode", "Enable or disable batch mode for CLI operations (default: " + Constants.DEFAULT_BATCHMODE + ")"), Config.BATCHMODE);
            {
               EditorGUI.indentLevel++;

               Config.QUIT = EditorGUILayout.Toggle(new GUIContent("Quit", "Enable or disable quit Unity Editor for CLI operations (default: " + Constants.DEFAULT_QUIT + ")."), Config.QUIT);

               Config.NO_GRAPHICS = EditorGUILayout.Toggle(new GUIContent("No Graphics", "Enable or disable graphics device in Unity Editor for CLI operations (default: " + Constants.DEFAULT_NO_GRAPHICS + ")."), Config.NO_GRAPHICS);

               EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndToggleGroup();

            GUILayout.Space(space);

            Config.DEBUG = EditorGUILayout.Toggle(new GUIContent("Debug", "Enable or disable debug logs (default: " + Constants.DEFAULT_DEBUG + ")."), Config.DEBUG);

            Config.UPDATE_CHECK = EditorGUILayout.Toggle(new GUIContent("Update Check", "Enable or disable the update-checks for the asset (default: " + Constants.DEFAULT_UPDATE_CHECK + ")"), Config.UPDATE_CHECK);

            //Config.COMPILE_DEFINES = EditorGUILayout.Toggle(new GUIContent("Compile Defines", "Enable or disable adding compile define 'CT_TB' for the asset (default: " + Constants.DEFAULT_COMPILE_DEFINES + ")"), Config.COMPILE_DEFINES);

            Helper.SeparatorUI();

            GUILayout.Label("Backup & Restore Settings", EditorStyles.boldLabel);
            Config.SLOTS = EditorGUILayout.IntSlider(new GUIContent("Slots", $"Slots for the backup and restore operations (default: {Constants.DEFAULT_SLOTS}, range 1-16)"), Config.SLOTS, 1, 16);

            AutoBackup.BackupInterval = EditorGUILayout.IntSlider(new GUIContent("Auto Backup Interval", "Automatic backup after the given minutes  (default: 0, 0 = disabled, range: 0-300)"), AutoBackup.BackupInterval, 0, 300);

            Config.AUTO_SAVE = EditorGUILayout.Toggle(new GUIContent("Auto Save Scenes", $"Enable or disable automatic saving of all modified scenes (default: {Constants.DEFAULT_AUTO_SAVE})."), Config.AUTO_SAVE);

            Config.COPY_ASSETS = EditorGUILayout.Toggle(new GUIContent("Copy Assets", $"Enable or disable the copying the 'Assets' folder (default: {Constants.DEFAULT_COPY_ASSETS})."), Config.COPY_ASSETS);

            if (Config.USE_LEGACY)
               Config.COPY_LIBRARY = EditorGUILayout.Toggle(new GUIContent("Copy Library", $"Enable or disable the copying the 'Library' folder (default: {Constants.DEFAULT_COPY_LIBRARY})."), Config.COPY_LIBRARY);

            Config.COPY_SETTINGS = EditorGUILayout.Toggle(new GUIContent("Copy ProjectSettings", $"Enable or disable the copying the 'ProjectSettings' folder (default: {Constants.DEFAULT_COPY_SETTINGS})."), Config.COPY_SETTINGS);
#if UNITY_2020_1_OR_NEWER
            Config.COPY_USER_SETTINGS = EditorGUILayout.Toggle(new GUIContent("Copy UserSettings", "Enable or disable the copying the 'UserSettings' folder (default: " + Constants.DEFAULT_COPY_USER_SETTINGS + ")."), Config.COPY_USER_SETTINGS);
#endif
            Config.COPY_PACKAGES = EditorGUILayout.Toggle(new GUIContent("Copy Packages", $"Enable or disable the copying the 'Packages' folder (default: {Constants.DEFAULT_COPY_PACKAGES})."), Config.COPY_PACKAGES);

            if (!Helper.isBackupEnabled)
            {
               EditorGUILayout.HelpBox("Please enable at least one folder!", MessageType.Error);
            }
#if UNITY_EDITOR_WIN
            Config.THREADS = EditorGUILayout.IntSlider(new GUIContent("Threads", $"Threads for the backup and restore operations (default: {Constants.DEFAULT_THREADS}, range 2-128)"), Config.THREADS, 2, 128);
#endif
            Helper.SeparatorUI();

            GUILayout.Label("UI Settings", EditorStyles.boldLabel);
            Config.CONFIRM_BACKUP = EditorGUILayout.Toggle(new GUIContent("Confirm Backup", "Enable or disable the backup confirmation dialog (default: " + Constants.DEFAULT_CONFIRM_BACKUP + ")."), Config.CONFIRM_BACKUP);

            Config.CONFIRM_RESTORE = EditorGUILayout.BeginToggleGroup(new GUIContent("Confirm Restore", "Enable or disable the backup confirmation dialog (default: " + Constants.DEFAULT_CONFIRM_RESTORE + ")."), Config.CONFIRM_RESTORE);
            {
               EditorGUI.indentLevel++;

               Config.CONFIRM_WARNING = EditorGUILayout.Toggle(new GUIContent("Confirm Warning", "Enable or disable the restore warning confirmation dialog (default: " + Constants.DEFAULT_CONFIRM_WARNING + ")."), Config.CONFIRM_WARNING);

               EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndToggleGroup();

            Helper.SeparatorUI();

            GUILayout.Label("Methods", EditorStyles.boldLabel);

            Config.EXECUTE_METHOD_PRE_BACKUP = EditorGUILayout.TextField(new GUIContent("Method Before Backup", "Execute static method <ClassName.MethodName> in Unity before a backup (default: empty, e.g. 'Crosstales.TB.BAR.MethodBeforeBackup')."), Config.EXECUTE_METHOD_PRE_BACKUP);
            Config.EXECUTE_METHOD_BACKUP = EditorGUILayout.TextField(new GUIContent("Method After Backup", "Execute static method <ClassName.MethodName> in Unity after a backup (default: empty, e.g. 'Crosstales.TB.BAR.MethodAfterBackup')."), Config.EXECUTE_METHOD_BACKUP);
            Config.EXECUTE_METHOD_PRE_RESTORE = EditorGUILayout.TextField(new GUIContent("Method Before Restore", "Execute static method <ClassName.MethodName> in Unity before a restore (default: empty, e.g. 'Crosstales.TB.BAR.MethodBeforeRestore')."), Config.EXECUTE_METHOD_PRE_RESTORE);
            Config.EXECUTE_METHOD_RESTORE = EditorGUILayout.TextField(new GUIContent("Method After Restore", "Execute static method <ClassName.MethodName> in Unity after a restore (default: empty, e.g. 'Crosstales.TB.BAR.MethodAfterRestore')."), Config.EXECUTE_METHOD_RESTORE);
         }
         EditorGUILayout.EndScrollView();

         Helper.SeparatorUI(3);

         GUILayout.Label("Backup Usage", EditorStyles.boldLabel);

         GUILayout.Label($"Backup Count:\t{Config.BACKUP_COUNT}");
         GUILayout.Label($"Restore Count:\t{Config.RESTORE_COUNT}");

         //GUI.skin.label.wordWrap = true;

         GUILayout.Label($"Details:\t\t{Helper.BackupInfo}");

         //GUI.skin.label.wordWrap = false;

         GUILayout.Space(6);

         GUI.enabled = Helper.hasBackup && !Helper.isDeleting;

         if (GUILayout.Button(new GUIContent(" Show Backup", Helper.Icon_Show, "Show the backup.")))
         {
            Crosstales.Common.Util.FileHelper.ShowFile(Config.PATH_BACKUP);
         }

         if (GUILayout.Button(new GUIContent(" Delete Backup", Helper.Icon_Delete, "Delete the complete backup")))
         {
            if (EditorUtility.DisplayDialog("Delete the complete backup?", $"If you delete the complete backup, {Constants.ASSET_NAME} must store all data for the next backup.{System.Environment.NewLine}This operation could take some time.{System.Environment.NewLine}{System.Environment.NewLine}Would you like to delete the backup?", "Yes", "No"))
            {
               Helper.DeleteBackup();

               if (Config.DEBUG)
                  Debug.Log("Complete backup deleted");
            }
         }

         GUI.enabled = true;
      }

      protected void showHelp()
      {
         scrollPosHelp = EditorGUILayout.BeginScrollView(scrollPosHelp, false, false);
         {
            GUILayout.Label("Resources", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            {
               GUILayout.BeginVertical();
               {
                  if (GUILayout.Button(new GUIContent(" Manual", Helper.Icon_Manual, "Show the manual.")))
                     Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_MANUAL_URL);

                  GUILayout.Space(6);

                  if (GUILayout.Button(new GUIContent(" Forum", Helper.Icon_Forum, "Visit the forum page.")))
                     Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_FORUM_URL);
               }
               GUILayout.EndVertical();

               GUILayout.BeginVertical();
               {
                  if (GUILayout.Button(new GUIContent(" API", Helper.Icon_API, "Show the API.")))
                     Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_API_URL);

                  GUILayout.Space(6);

                  if (GUILayout.Button(new GUIContent(" Product", Helper.Icon_Product, "Visit the product page.")))
                     Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_WEB_URL);
               }
               GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            Helper.SeparatorUI();

            GUILayout.Label("Videos", EditorStyles.boldLabel);

            if (GUILayout.Button(new GUIContent(" Tutorial", Helper.Video_Tutorial, "View the tutorial video on 'Youtube'.")))
               Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_VIDEO_TUTORIAL);

            GUILayout.Space(6);

            if (GUILayout.Button(new GUIContent(" All Videos", Helper.Icon_Videos, "Visit our 'Youtube'-channel for more videos.")))
               Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_SOCIAL_YOUTUBE);

            Helper.SeparatorUI();

            GUILayout.Label("3rd Party Assets", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            {
               if (GUILayout.Button(new GUIContent(string.Empty, Helper.Asset_RockTomate, "More information about 'RockTomate'.")))
                  Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_3P_ROCKTOMATE);

               //CT Ads
               switch (adRnd1)
               {
                  case 0:
                     {
                        if (GUILayout.Button(new GUIContent(string.Empty, Helper.Logo_Asset_BWF, "More information about 'Bad Word Filter'.")))
                           Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_BWF);

                        break;
                     }
                  case 1:
                     {
                        if (GUILayout.Button(new GUIContent(string.Empty, Helper.Logo_Asset_DJ, "More information about 'DJ'.")))
                           Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_DJ);

                        break;
                     }
                  default:
                     {
                        if (GUILayout.Button(new GUIContent(string.Empty, Helper.Logo_Asset_FB, "More information about 'File Browser'.")))
                           Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_FB);

                        break;
                     }
               }

               switch (adRnd2)
               {
                  case 0:
                     {
                        if (GUILayout.Button(new GUIContent(string.Empty, Helper.Logo_Asset_Radio, "More information about 'Radio'.")))
                           Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_RADIO);

                        break;
                     }
                  case 1:
                     {
                        if (GUILayout.Button(new GUIContent(string.Empty, Helper.Logo_Asset_RTV, "More information about 'RT-Voice'.")))
                           Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_RTV);

                        break;
                     }
                  default:
                     {
                        if (GUILayout.Button(new GUIContent(string.Empty, Helper.Logo_Asset_OC, "More information about 'Online Check'.")))
                           Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_OC);

                        break;
                     }
               }

               switch (adRnd3)
               {
                  case 0:
                     {
                        if (GUILayout.Button(new GUIContent(string.Empty, Helper.Logo_Asset_TPS, "More information about 'Turbo Switch'.")))
                           Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_TPS);

                        break;
                     }
                  case 1:
                     {
                        if (GUILayout.Button(new GUIContent(string.Empty, Helper.Logo_Asset_TPB, "More information about 'Turbo Builder'.")))
                           Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_TPB);

                        break;
                     }
                  default:
                     {
                        if (GUILayout.Button(new GUIContent(string.Empty, Helper.Logo_Asset_TR, "More information about 'True Random'.")))
                           Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_TR);

                        break;
                     }
               }
            }
            GUILayout.EndHorizontal();
         }
         EditorGUILayout.EndScrollView();

         GUILayout.Space(6);
      }

      protected void showAbout()
      {
         GUILayout.Space(3);
         GUILayout.Label(Constants.ASSET_NAME, EditorStyles.boldLabel);

         GUILayout.BeginHorizontal();
         {
            GUILayout.BeginVertical(GUILayout.Width(60));
            {
               GUILayout.Label("Version:");

               GUILayout.Space(12);

               GUILayout.Label("Web:");

               GUILayout.Space(2);

               GUILayout.Label("Email:");
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(170));
            {
               GUILayout.Space(0);

               GUILayout.Label(Constants.ASSET_VERSION);

               GUILayout.Space(12);

               EditorGUILayout.SelectableLabel(Constants.ASSET_AUTHOR_URL, GUILayout.Height(16), GUILayout.ExpandHeight(false));

               GUILayout.Space(2);

               EditorGUILayout.SelectableLabel(Constants.ASSET_CONTACT, GUILayout.Height(16), GUILayout.ExpandHeight(false));
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            {
               //GUILayout.Space(0);
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(64));
            {
               if (GUILayout.Button(new GUIContent(string.Empty, Helper.Logo_Asset, "Visit asset website")))
                  Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_URL);
            }
            GUILayout.EndVertical();
         }
         GUILayout.EndHorizontal();

         GUILayout.Label("© 2018-2024 by " + Constants.ASSET_AUTHOR);

         Helper.SeparatorUI();

         GUILayout.BeginHorizontal();
         {
            if (GUILayout.Button(new GUIContent(" AssetStore", Helper.Logo_Unity, "Visit the 'Unity AssetStore' website.")))
               Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_CT_URL);

            if (GUILayout.Button(new GUIContent(" " + Constants.ASSET_AUTHOR, Helper.Logo_CT, "Visit the '" + Constants.ASSET_AUTHOR + "' website.")))
               Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_AUTHOR_URL);
         }
         GUILayout.EndHorizontal();

         Helper.SeparatorUI();

         aboutTab = GUILayout.Toolbar(aboutTab, new[] { "Readme", "Versions", "Update" });

         switch (aboutTab)
         {
            case 2:
               {
                  scrollPosAboutUpdate = EditorGUILayout.BeginScrollView(scrollPosAboutUpdate, false, false);
                  {
                     Color fgColor = GUI.color;

                     GUI.color = Color.yellow;

                     switch (updateStatus)
                     {
                        case UpdateStatus.NO_UPDATE:
                           GUI.color = Color.green;
                           GUILayout.Label(updateText);
                           break;
                        case UpdateStatus.UPDATE:
                           {
                              GUILayout.Label(updateText);

                              if (GUILayout.Button(new GUIContent(" Download", "Visit the 'Unity AssetStore' to download the latest version.")))
                                 UnityEditorInternal.AssetStore.Open("content/" + Constants.ASSET_ID);

                              break;
                           }
                        case UpdateStatus.UPDATE_VERSION:
                           {
                              GUILayout.Label(updateText);

                              if (GUILayout.Button(new GUIContent(" Upgrade", "Upgrade to the newer version in the 'Unity AssetStore'")))
                                 Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_CT_URL);

                              break;
                           }
                        case UpdateStatus.DEPRECATED:
                           {
                              GUILayout.Label(updateText);

                              if (GUILayout.Button(new GUIContent(" More Information", "Visit the 'crosstales'-site for more information.")))
                                 Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_AUTHOR_URL);

                              break;
                           }
                        default:
                           GUI.color = Color.cyan;
                           GUILayout.Label(updateText);
                           break;
                     }

                     GUI.color = fgColor;
                  }
                  EditorGUILayout.EndScrollView();

                  if (updateStatus == UpdateStatus.NOT_CHECKED || updateStatus == UpdateStatus.NO_UPDATE)
                  {
                     bool isChecking = !(worker == null || worker?.IsAlive == false);

                     GUI.enabled = Crosstales.Common.Util.NetworkHelper.isInternetAvailable && !isChecking;

                     if (GUILayout.Button(new GUIContent(isChecking ? "Checking... Please wait." : " Check For Update", Helper.Icon_Check, "Checks for available updates of " + Constants.ASSET_NAME)))
                     {
                        worker = new System.Threading.Thread(() => UpdateCheck.UpdateCheckForEditor(out updateText, out updateStatus));
                        worker.Start();
                     }

                     GUI.enabled = true;
                  }

                  break;
               }
            case 0:
               {
                  if (readme == null)
                  {
                     string path = Application.dataPath + Config.ASSET_PATH + "README.txt";

                     try
                     {
                        readme = Crosstales.Common.Util.FileHelper.ReadAllText(path);
                     }
                     catch (System.Exception)
                     {
                        readme = "README not found: " + path;
                     }
                  }

                  scrollPosAboutReadme = EditorGUILayout.BeginScrollView(scrollPosAboutReadme, false, false);
                  {
                     GUILayout.Label(readme);
                  }
                  EditorGUILayout.EndScrollView();
                  break;
               }
            default:
               {
                  if (versions == null)
                  {
                     string path = Application.dataPath + Config.ASSET_PATH + "Documentation/VERSIONS.txt";

                     try
                     {
                        versions = Crosstales.Common.Util.FileHelper.ReadAllText(path);
                     }
                     catch (System.Exception)
                     {
                        versions = "VERSIONS not found: " + path;
                     }
                  }

                  scrollPosAboutVersions = EditorGUILayout.BeginScrollView(scrollPosAboutVersions, false, false);
                  {
                     GUILayout.Label(versions);
                  }

                  EditorGUILayout.EndScrollView();
                  break;
               }
         }

         Helper.SeparatorUI(6);

         GUILayout.BeginHorizontal();
         {
            if (GUILayout.Button(new GUIContent(string.Empty, Helper.Social_Discord, "Communicate with us via 'Discord'.")))
               Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_SOCIAL_DISCORD);

            if (GUILayout.Button(new GUIContent(string.Empty, Helper.Social_Facebook, "Follow us on 'Facebook'.")))
               Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_SOCIAL_FACEBOOK);

            if (GUILayout.Button(new GUIContent(string.Empty, Helper.Social_Twitter, "Follow us on 'Twitter'.")))
               Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_SOCIAL_TWITTER);

            if (GUILayout.Button(new GUIContent(string.Empty, Helper.Social_Linkedin, "Follow us on 'LinkedIn'.")))
               Crosstales.Common.Util.NetworkHelper.OpenURL(Constants.ASSET_SOCIAL_LINKEDIN);
         }
         GUILayout.EndHorizontal();

         GUILayout.Space(6);
      }

      protected static void save()
      {
         Config.Save();

         if (Config.DEBUG)
            Debug.Log("Config data saved");
      }

      #endregion
   }
}
#endif
// © 2018-2024 crosstales LLC (https://www.crosstales.com)