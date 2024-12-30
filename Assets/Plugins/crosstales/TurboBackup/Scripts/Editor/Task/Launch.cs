#if UNITY_EDITOR && !UNITY_CLOUD_BUILD
using System.Linq;
using UnityEditor;

namespace Crosstales.TB.Task
{
   /// <summary>Show the configuration window on the first launch.</summary>
   public class Launch : AssetPostprocessor
   {
      public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
      {
         if (importedAssets.Any(str => str.Contains(Crosstales.TB.Util.Constants.ASSET_UID.ToString())))
         {
            Crosstales.Common.EditorTask.SetupResources.Setup();
            SetupResources.Setup();

            EditorIntegration.ConfigWindow.ShowWindow(3);
         }
      }
   }
}
#endif
// © 2018-2024 crosstales LLC (https://www.crosstales.com)