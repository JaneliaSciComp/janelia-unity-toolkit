using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.Callbacks;
#endif

namespace Janelia
{
    public static class Timeout
    {
#if UNITY_EDITOR
        [PostProcessBuildAttribute(SessionParameters.POST_PROCESS_BUILD_ORDER + 1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            Debug.Log("Janelia.Timeout.OnPostprocessBuild: " + pathToBuiltProject);

            SessionParameters.AddFloatParameter("timeoutSecs", 0);
        }
#endif

        [RuntimeInitializeOnLoadMethod]
        private static async Task OnRuntimeMethodLoadAsync()
        {
            // Note that this function is marked `RuntimeInitializeOnLoadMethod` witn
            // no additional argument.  Thus, it should get executed after
            // `SessionParameters.OnRuntimeMethodLoad`, where the parameters file
            // gets loaded to set the value for thie `GetFloatParameters` call.
            double timeout = SessionParameters.GetFloatParameter("timeoutSecs");
            timeout = WithCommmandlineOverride(timeout);
            if (timeout > 0)
            {
                Debug.Log("Starting to wait for timeout period, " + timeout + " seconds");
                await Task.Delay(TimeSpan.FromSeconds(timeout));
                Debug.Log("Done waiting for timeout period, quitting");
                Application.Quit();
            }
        }

        private static double WithCommmandlineOverride(double timeout)
        {
            double result = timeout;
            string[] args = System.Environment.GetCommandLineArgs();
            if (args.Contains("-timeoutSecs"))
            {
                int i = Array.IndexOf(args, "-timeoutSecs");
                if (i + 1 < args.Length)
                {
                    double arg;
                    if (double.TryParse(args[i + 1], out arg))
                    {
                        result = arg;
                        Debug.Log("Commandline overriding timeout period to " + result);
                    }
                }
            }
            return result;
        }
    }
}
