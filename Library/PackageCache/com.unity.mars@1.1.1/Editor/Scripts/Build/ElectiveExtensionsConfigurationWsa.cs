#if UNITY_WSA
using UnityEditor.Build.Reporting;

namespace UnityEditor.MARS.Build
{
    public class ElectiveExtensionsConfigurationWsa : ElectiveExtensions
    {
        public override void OnPreprocessBuild(BuildReport report)
        {
            RunReport(ConfigWsa);
        }

        public static readonly SupportedConfiguration ConfigWsa = new SupportedConfiguration(
            "Universal Windows Platform",
            new[] {BuildTarget.WSAPlayer},
            new[]
            {
                new PackageItem("com.unity.xr.windowsmr", "2.0.3"),
                new PackageItem("com.unity.xr.arfoundation", "2.1.8")
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
            reportOutputLevel: SupportedConfiguration.ReportOutputLevels.DebugLogOnly);
    }
}
#endif
