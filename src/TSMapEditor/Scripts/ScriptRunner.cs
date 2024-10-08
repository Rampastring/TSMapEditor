using Rampastring.Tools;
using System;
using System.IO;
using System.Reflection;
using TSMapEditor.Models;
using Westwind.Scripting;

namespace TSMapEditor.Scripts
{
    public static class ScriptRunner
    {
        private static object scriptClassInstance;
        private static MethodInfo getDescriptionMethod;
        private static MethodInfo performMethod;
        private static MethodInfo getSuccessMessageMethod;

        public static string RunScript(Map map, string scriptPath)
        {
            if (scriptClassInstance == null || performMethod == null || getSuccessMessageMethod == null)
                throw new InvalidOperationException("Script not properly compiled!");

            Logger.Log("Running script from " + scriptPath);

            try
            {
                performMethod.Invoke(scriptClassInstance, new object[] { map });
                return (string)getSuccessMessageMethod.Invoke(scriptClassInstance, null);
            }
            catch (Exception ex) // rare case where catching Exception is OK, we cannot know what the script can throw
            {
                string errorMessage = ex.Message;

                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    errorMessage += Environment.NewLine + Environment.NewLine + 
                        "Inner exception message: " + ex.Message + Environment.NewLine + 
                        "Stack trace: " + ex.StackTrace;
                }

                Logger.Log("Exception while running script. Returned exception message: " + errorMessage);

                return "An error occurred while running the script. Returned error message: " + Environment.NewLine + Environment.NewLine + errorMessage;
            }
        }

        public static (string error, string description) GetDescriptionFromScript(Map map, string scriptPath)
        {
            if (!File.Exists(scriptPath))
                return ("The script file does not exist!", null);

            var sourceCode = File.ReadAllText(scriptPath);
            string error = CompileSource(map, sourceCode);
            if (error != null)
                return (error, null);

            return (null, (string)getDescriptionMethod.Invoke(scriptClassInstance, null));
        }

        private static string CompileSource(Map map, string source)
        {
            var script = new CSharpScriptExecution() { SaveGeneratedCode = true };
            script.AddLoadedReferences();
            script.AddNamespace("TSMapEditor");
            script.AddNamespace("TSMapEditor.Models");
            script.AddNamespace("TSMapEditor.Rendering");
            script.AddNamespace("TSMapEditor.GameMath");

            getDescriptionMethod = null;
            performMethod = null;
            getSuccessMessageMethod = null;

            object instance = script.CompileClass(source);

            if (script.Error)
            {
                return script.ErrorMessage;
            }

            scriptClassInstance = instance;
            Type classType = instance.GetType();
            var methods = classType.GetMethods();
            foreach (MethodInfo method in methods)
            {
                if (method.Name == "GetDescription")
                {
                    getDescriptionMethod = method;

                    if (getDescriptionMethod.ReturnType != typeof(string))
                        return "GetDescription does not return a string!";
                }
                else if (method.Name == "Perform")
                {
                    performMethod = method;
                }
                else if (method.Name == "GetSuccessMessage")
                {
                    getSuccessMessageMethod = method;

                    if (getSuccessMessageMethod.ReturnType != typeof(string))
                        return "GetSuccessMessage does not return a string!";
                }
            }

            if (getDescriptionMethod == null || performMethod == null || getSuccessMessageMethod == null)
            {
                return "The script does not declare one or more required methods.";
            }

            return null;
        }
    }
}
