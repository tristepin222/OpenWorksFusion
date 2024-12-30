#if UNITY_EDITOR
using UnityEditor;
using Crosstales.TB.Util;

namespace Crosstales.TB.Task
{
   /// <summary>Setup Unity after a restore.</summary>
   [InitializeOnLoad]
   public static class SetupUnity
   {
      #region Constructor

      static SetupUnity()
      {
         if (Config.USE_LEGACY && Config.SETUP_DATE < Config.RESTORE_DATE)
         {
            Helper.RefreshAssetDatabase();

            Config.SETUP_DATE = System.DateTime.Now;
            Config.Save();
         }
      }

      #endregion
   }
}
#endif
// © 2019-2024 crosstales LLC (https://www.crosstales.com)