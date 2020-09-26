using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.XRTools.Utils;
using UnityEngine;

namespace UnityEditor.XRTools.Utils.Tests
{
    class CompilationTest
    {
        [Test]
        public void CCUEnabled()
        {
            Assert.IsTrue(ConditionalCompilationUtility.enabled);
        }

        /// <summary>
        /// Test that no compile errors or warnings are introduced if CCU defines aren't present
        /// </summary>
        [Test]
        public void NoCCUDefines()
        {
            var defines = EditorUserBuildSettings.activeScriptCompilationDefines.ToList();
            var ccuDefines = ConditionalCompilationUtility.defines;
            if (ccuDefines != null)
                defines = defines.Except(ccuDefines).ToList();

            TestCompile(defines.ToArray());
        }

        static void TestCompile(string[] defines)
        {
            var outputFile = "Temp/CCUTest.dll";

            var references = new List<string>();
            ReflectionUtils.ForEachAssembly(assembly =>
            {
#if NET_4_6
                if (assembly.IsDynamic)
                    return;
#endif
                // Ignore project assemblies because they will cause conflicts
                if (assembly.FullName.StartsWith("Assembly-CSharp", StringComparison.OrdinalIgnoreCase))
                    return;

                // System.dll is included automatically and will cause conflicts if referenced explicitly
                if (assembly.FullName.StartsWith("System", StringComparison.OrdinalIgnoreCase))
                    return;

                // This assembly causes a ReflectionTypeLoadException on compile
                if (assembly.FullName.StartsWith("ICSharpCode.NRefactory", StringComparison.OrdinalIgnoreCase))
                    return;

                if (assembly.FullName.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase))
                    return;

                var codeBase = assembly.CodeBase;
                var uri = new UriBuilder(codeBase);
                var path = Uri.UnescapeDataString(uri.Path);

                references.Add(path);
            });

            var sources = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

            var output = EditorUtility.CompileCSharp(sources, references.ToArray(), defines, outputFile);
            foreach (var o in output)
            {
                var line = o.ToLower();
                if (line.Contains("com.unity") || line.Contains("com.unity"))
                    Assert.IsFalse(line.Contains("exception") || line.Contains("error") || line.Contains("warning"), string.Join("\n", output));
            }
        }
    }
}
