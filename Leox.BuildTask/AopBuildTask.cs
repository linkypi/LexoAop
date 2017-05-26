using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Leox.BuildTask
{
    public class AopBuildTask : Task
    {
        private string outputFile;

        [Microsoft.Build.Framework.Required]
        public string OutputFile
        {
            get { return outputFile; }
            set { outputFile = value; }
        }
        [Microsoft.Build.Framework.Required]
        public string TaskFile { get; set; }

        [Output]
        public string Paths { get; set; }

        private static readonly string[] _fileSuffix = new string[] { ".exe" };
        public override bool Execute()
        {
            try
            {
                Log.LogMessage(MessageImportance.Normal, "build " + outputFile);
                Paths = outputFile;
                int index = outputFile.LastIndexOf("\\");
                var projectPath = outputFile.Substring(0, index + 1);
                var projectName = outputFile.Substring(index + 1, outputFile.Length - index - 1);
                projectName = projectName.Substring(0, projectName.LastIndexOf("."));

                //Log.LogMessage(MessageImportance.Normal, string.Format("build project path :{0}, project name: {1} .", projectPath, projectName));

                var binPath = Path.Combine(projectPath, "bin", "Debug");
                DirectoryInfo directory = new DirectoryInfo(binPath);
                BuildByCmd(projectName + ".exe");

                //var injector = new Injector();
                //foreach (var item in directory.GetFiles().Where(f => _fileSuffix.Any(s => f.Name.EndsWith(s))))
                //{
                //    Log.LogMessage(MessageImportance.Normal, "find attr from " + item.FullName);
                //    injector.Inject(item.FullName);
                //}
                //BuildByCmd(Path.Combine("bin", "Debug", projectName + ".exe"));
            }
            catch (Exception ex)
            {
                Log.LogMessage(MessageImportance.High, "build failed : " + ex.Message + ", stacktrace : " + ex.StackTrace);
                return false;
            }

            return true;
        }

        private void BuildByCmd(string file)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.StandardInput.AutoFlush = true;
                process.StandardInput.WriteLine("cd bin/Debug");
                process.StandardInput.WriteLine(TaskFile + " " + file);
                process.StandardInput.WriteLine("exit");
                string strRst = process.StandardOutput.ReadToEnd();

                process.WaitForExit();
                process.Close();
                Log.LogMessage(strRst);
                Log.LogWarning(strRst);
            }
        }

    }
}
