using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// GEAYI: fuerza la configuración Android correcta antes de cada compilación.
// El proyecto se genera sin Unity Editor, así garantizamos los valores por código.
public class GeayiBuildConfig : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
            return;

        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);

        Debug.Log("[GEAYI] Config Android aplicada: ARM64 + IL2CPP");
    }
}
