#if UNITY_EDITOR
namespace Crosstales.TB.Util
{
   /// <summary>Logger for the asset.</summary>
   public static class CTLogger
   {
      private static readonly string fileMethods = Crosstales.Common.Util.FileHelper.TempPath + "TB_Methods.log";
      private static readonly string fileLog = Crosstales.Common.Util.FileHelper.TempPath + "TB.log";

      public static void Log(string log)
      {
         System.IO.File.AppendAllText(fileLog, System.DateTime.Now.ToLocalTime() + " - " + log + System.Environment.NewLine);
      }

      public static void BeforeBackup()
      {
         System.IO.File.AppendAllText(fileMethods, System.DateTime.Now.ToLocalTime() + " - BeforeBackup" + System.Environment.NewLine);
      }

      public static void AfterBackup()
      {
         System.IO.File.AppendAllText(fileMethods, System.DateTime.Now.ToLocalTime() + " - AfterBackup" + System.Environment.NewLine);
      }

      public static void BeforeRestore()
      {
         System.IO.File.AppendAllText(fileMethods, System.DateTime.Now.ToLocalTime() + " - BeforeRestore" + System.Environment.NewLine);
      }

      public static void AfterRestore()
      {
         System.IO.File.AppendAllText(fileMethods, System.DateTime.Now.ToLocalTime() + " - AfterRestore" + System.Environment.NewLine);
      }
   }
}
#endif
// © 2019-2024 crosstales LLC (https://www.crosstales.com)