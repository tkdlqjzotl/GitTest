#if UNITY_ANDROID
using UnityEngine.Rendering;
using UnityEditor.Build.Reporting;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.MARS.Build
{
    [MovedFrom("Unity.MARS.Build")]
    public class ElectiveExtensionsConfigurationAndroid : ElectiveExtensions
    {
        public override void OnPreprocessBuild(BuildReport report)
        {
            RunReport(ConfigAndroid);
        }

        public static readonly SupportedConfiguration ConfigAndroid = new SupportedConfiguration(
            "Android",
            new[] {BuildTarget.Android},
            new[]
            {
                new PackageItem("com.unity.xr.arfoundation", "2.1.8"),
                new PackageItem("com.unity.xr.arcore", "2.1.8")
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
            supportedEditorVersions: new[]
            {
                "2019.3"
            },
            reportOutputLevel: SupportedConfiguration.ReportOutputLevels.WarnOnIssue);
    }
}
#endif
