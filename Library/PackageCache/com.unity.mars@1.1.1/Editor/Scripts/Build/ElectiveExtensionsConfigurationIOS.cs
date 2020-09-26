#if UNITY_IOS
using UnityEngine.Rendering;
using UnityEditor.Build.Reporting;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.MARS.Build
{
    [MovedFrom("Unity.MARS.Build")]
    public class ElectiveExtensionsConfigurationIOS : ElectiveExtensions
    {
        public override void OnPreprocessBuild(BuildReport report)
        {
            RunReport(ConfigIOS);
        }

        public static readonly SupportedConfiguration ConfigIOS = new SupportedConfiguration(
            "iOS",
            new[] {BuildTarget.iOS},
            new[]
            {
                new PackageItem("com.unity.xr.arfoundation", "2.1.8"),
                new PackageItem("com.unity.xr.arkit", "2.1.9"),
                new PackageItem("com.unity.xr.arkit-face-tracking", "1.0.7")
            },
            new[]
            {
                //UNITY_CCU
                new DefineSymbolItem("UNITY_CCU",
                    true
                    #if UNITY_CCU
                    ,true
                    #endif
                    )
                },
            supportedEditorVersions:new[]
            {
                "2019.3"
            },
            reportOutputLevel: SupportedConfiguration.ReportOutputLevels.WarnOnIssue);
    }
}
#endif
