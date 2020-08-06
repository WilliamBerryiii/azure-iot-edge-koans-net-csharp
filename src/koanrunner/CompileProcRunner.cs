using System;
using System.Diagnostics;

namespace koanrunner
{

    public enum DotNetRunOption{
        Build,
        Test
    }
    public class CompilerProcRunner
    {

        public static Func<string, string> buildArgs = (string projectPath) => String.Format(@"build {0}", projectPath);
        public static Func<string, string> runTestsArgs = (string testProjectPath) => String.Format(@"test {0}", testProjectPath);
        public static void CompileProject(DotNetRunOption runOption, string projectPath)
        {
            string dotnetCommandArgs = "";
            switch (runOption){
                case DotNetRunOption.Build: 
                    dotnetCommandArgs = buildArgs(projectPath);
                    break;
                case DotNetRunOption.Test: 
                    dotnetCommandArgs = runTestsArgs(projectPath);
                    break;
                default:
                    Console.WriteLine("Unused.");
                    return;
            }

            using (var process = new Process())
            {
                Console.WriteLine("processing file: {0}", projectPath);
                process.StartInfo.FileName = "dotnet.exe";            
                process.StartInfo.Arguments = dotnetCommandArgs;

                // do not run in new window
                process.StartInfo.CreateNoWindow = true;

                // redirect std E/O to this console 
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;
                
                // Do not use the shell to execute
                process.StartInfo.UseShellExecute = false;

                //prompt the user with details about what's happening; e.g. "running build"

                // wire up process redirect
                process.OutputDataReceived += (sender, args) => Console.WriteLine("{0}", args.Data);

                //start the process
                process.Start();
                process.BeginOutputReadLine();

                // make sure the process has not error'ed out before we ask it to wait for exit 
                if (process != null && process.HasExited != true)
                {
                    process.WaitForExit();
                }
                // no else as the process will have returned and given the user the error.
            }
        }
    }
}