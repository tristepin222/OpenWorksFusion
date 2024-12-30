#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Crosstales.TB.Util;

namespace Crosstales.TB.EditorTask
{
   /// <summary>Automatically backup in a set interval (in minutes).</summary>
   [InitializeOnLoad]
   public class AutoBackup
   {
      private static bool isWorking;

      public static int BackupInterval
      {
         get => Config.AUTO_BACKUP_INTERVAL;

         set
         {
            if (value != Config.AUTO_BACKUP_INTERVAL)
            {
               Config.AUTO_BACKUP_INTERVAL = Mathf.Abs(value);
               Config.AUTO_BACKUP_DATE = BackupInterval > 0 ? System.DateTime.Now.AddMinutes(BackupInterval) : System.DateTime.Now.AddYears(1976);
            }
         }
      }

      static AutoBackup()
      {
         EditorApplication.update += update;
         EditorApplication.quitting += onQuitting;

         BAR.OnBackupStart += onBackupStart;
         BAR.OnBackupComplete += onBackupComplete;
         BAR.OnRestoreStart += onRestoreStart;
         BAR.OnRestoreComplete += onRestoreComplete;

         if (BackupInterval > 0)
         {
            if (Config.AUTO_BACKUP_DATE < System.DateTime.Now)
               Config.AUTO_BACKUP_DATE = System.DateTime.Now.AddMinutes(BackupInterval);

            //Debug.Log($"Auto backup enabled: {BackupInterval} - {Config.AUTO_BACKUP_DATE}");
         }

         /*
         else
         {
            Debug.Log("Auto backup disabled!");
         }
         */
      }


      private static void onQuitting()
      {
         //Common.CTPlayerPrefs.DeleteKey(Constants.KEY_AUTO_BACKUP_DATE);
         //Common.CTPlayerPrefs.Save();

         Config.Save();
      }

      private static void onBackupStart()
      {
         //Debug.Log("+++ Auto backup started: " + System.DateTime.Now);
         isWorking = true;
      }

      private static void onBackupComplete(bool success)
      {
         //Debug.Log("--- Auto backup ended: " + System.DateTime.Now);
         isWorking = false;

         Config.AUTO_BACKUP_DATE = System.DateTime.Now.AddMinutes(BackupInterval);
         Config.Save();
      }

      private static void onRestoreStart()
      {
         //Debug.Log("+++ Restore started: " + System.DateTime.Now);
         isWorking = true;
      }

      private static void onRestoreComplete(bool success)
      {
         //Debug.Log("--- Restore ended: " + System.DateTime.Now);
         isWorking = false;

         Config.AUTO_BACKUP_DATE = System.DateTime.Now.AddMinutes(BackupInterval);
         Config.Save();
      }

      private static void update()
      {
         if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying || BackupInterval < 1 || Config.AUTO_BACKUP_DATE > System.DateTime.Now)
            return;

         if (!isWorking)
         {
            BAR.Backup();
         }
      }
   }
}
#endif