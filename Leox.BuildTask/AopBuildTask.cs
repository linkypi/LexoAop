using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Leox.Injector;

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
                //需要设置当前目录为libs  否则执行Inject会找不到 Leox.Aop.dll
                Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), "libs"));
                Log.LogMessage(MessageImportance.High, "build " + outputFile);
                Paths = outputFile;
                int index = outputFile.LastIndexOf("\\");
                var projectPath = outputFile.Substring(0, index + 1);
                var projectName = outputFile.Substring(index + 1, outputFile.Length - index - 1);
                projectName = projectName.Substring(0, projectName.LastIndexOf("."));

                //Log.LogMessage(MessageImportance.Normal, string.Format("build project path :{0}, project name: {1} .", projectPath, projectName));

                //BuildByCmd(projectPath, projectName + ".exe");

                Build(projectPath);
            }
            catch (Exception ex)
            {
                Log.LogMessage(MessageImportance.High, "build failed : " + ex.Message + ", stacktrace : " + ex.StackTrace);
                return false;
            }

            return true;
        }

        private void Build(string projectPath)
        {
            //var objPath = Path.Combine(projectPath, "obj", "Debug");
            var binPath = Path.Combine(projectPath, "bin", "Debug");
            //if (!Directory.Exists(objPath)) Directory.CreateDirectory(objPath);
           
            //DirectoryInfo libsDir = new DirectoryInfo(Path.Combine(projectPath, "libs"));
            //foreach (var item in libsDir.GetFiles())
            //{
            //    string filename = Path.Combine(objPath, item.Name);
            //    File.Copy(item.FullName, filename, true);
            //    Log.LogMessage(MessageImportance.High, string.Format("copy file {0} -> {1}", item.FullName, filename));
            //}

            DirectoryInfo binDirectory = new DirectoryInfo(binPath);
            var injector = new Injector.Injector();
            var files = binDirectory.GetFiles().Where(f => _fileSuffix.Any(s => f.Name.EndsWith(s)
                && !f.Name.Contains(".vshost") && !f.Name.Equals("Leox.Injector.exe")));

            Exception ex = null;
            foreach (var item in files)
            {
                Log.LogMessage(MessageImportance.High, "find attr from " + item.FullName);
                var injected = injector.Inject(item.FullName,out ex);

                if (!injected) {
                    if (ex != null)
                    {
                        Log.LogMessage(MessageImportance.High, "injected faild ." + ex.Message);
                    }
                    else
                    {
                        Log.LogMessage(MessageImportance.High, "unfound attr .");
                    }
                    continue; 
                }

                //File.Copy(item.FullName, Path.Combine(binPath, item.Name), true);
                Log.LogMessage(MessageImportance.High, "inject finished .");
            }
        }

        private void BuildByCmd(string projectPath,string file)
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
                process.StandardInput.WriteLine("cd obj/Debug");
                process.StandardInput.WriteLine(TaskFile + " " + file);
                process.StandardInput.WriteLine(string.Format("copy {0} {1}/bin/Debug/{0}", file, Path.Combine(projectPath , file)));

                process.StandardInput.WriteLine("exit");
                string strRst = process.StandardOutput.ReadToEnd();

                process.WaitForExit();
                //process.Close();
                Log.LogMessage(strRst);
                Log.LogWarning(strRst);
            }
        }

    }
}
