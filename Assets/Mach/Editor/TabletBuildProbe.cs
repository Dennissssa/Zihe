using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;

namespace Mach.Editor
{
    // Verification component is included only in development builds, never in saved scenes.
    public sealed class TabletBuildProbe : IProcessSceneWithReport
    {
        public int callbackOrder => 0;
        public void OnProcessScene(Scene scene,BuildReport report)
        {
            if(report==null || (report.summary.options & UnityEditor.BuildOptions.Development)==0)return;
            foreach(var root in scene.GetRootGameObjects())
            {
                var tablet=root.GetComponentInChildren<HeldTablet>();
                if(tablet)tablet.gameObject.AddComponent<TabletSmokeProbe>();
                if(tablet)tablet.gameObject.AddComponent<TruckSmokeProbe>();
                if(tablet && tablet.GetComponent<BehaviorProgram>())tablet.gameObject.AddComponent<CircuitSmokeProbe>();
                if(tablet)tablet.gameObject.AddComponent<LightingSmokeProbe>();
                if(tablet)tablet.gameObject.AddComponent<PointHeightSmokeProbe>();
            }
        }
    }
}
