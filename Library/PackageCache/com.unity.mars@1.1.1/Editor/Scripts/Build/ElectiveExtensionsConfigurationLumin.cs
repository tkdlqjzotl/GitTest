#if UNITY_LUMIN
using UnityEngine.Rendering;
using UnityEditor.Build.Reporting;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.MARS.Build
{
    [MovedFrom("Unity.MARS.Build")]
    public class ElectiveExtensionsConfigurationLumin : ElectiveExtensions
    {
        public override void OnPreprocessBuild(BuildReport report)
        {
            RunReport(ConfigLumin);
        }

        public static readonly SupportedConfiguration ConfigLumin = new SupportedConfiguration(
            "Lumin",
            new[] {BuildTarget.Lumin},
            new[]
            {
                new PackageItem("com.unity.xr.magicleap", "4.1.3"),
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
            new[]
            {
                GraphicsDeviceType.OpenGLCore, GraphicsDeviceType.OpenGLES3
            },
            new[]
            {
                "2019.3"
            },
            supportedGraphicsApIsIsOrdered:true,
            reportOutputLevel: SupportedConfiguration.ReportOutputLevels.DebugLogOnly);
    }
}
#endif
