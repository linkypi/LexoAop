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

        private string _projectPath;

        private static readonly string[] _fileSuffix = new string[] { ".exe" };

        private static readonly List<string> _excludeFiles = new List<string>() {
           "ilasm.exe" , "ildasm.exe" , "Leox.Injector.exe"
        };
        public override bool Execute()
        {
            try
            {
                Log.LogMessage(MessageImportance.High, "build " + outputFile);
                Paths = outputFile;
                int index = outputFile.LastIndexOf("\\");
                _projectPath = outputFile.Substring(0, index + 1);
                var projectName = outputFile.Substring(index + 1, outputFile.Length - index - 1);
                projectName = projectName.Substring(0, projectName.LastIndexOf("."));

                //Log.LogMessage(MessageImportance.Normal, string.Format("build project path :{0}, project name: {1} .", projectPath, projectName));

                //BuildByCmd(projectPath, projectName + ".exe");
                BuildInObjDirectory();
                //BuildInLibsDirectory();
            }
            catch (Exception ex)
            {
                Log.LogMessage(MessageImportance.High, "build failed : " + ex.Message + ", stacktrace : " + ex.StackTrace);
                return false;
            }

            return true;
        }

        private void BuildInObjDirectory()
        {
            Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), "obj","Debug"));

            var objPath = Path.Combine(_projectPath, "obj", "Debug");
            var binPath = Path.Combine(_projectPath, "bin", "Debug");
            if (!Directory.Exists(objPath)) Directory.CreateDirectory(objPath);

            //copy libs/*.dll to obj/Debug
            DirectoryInfo libsDir = new DirectoryInfo(Path.Combine(_projectPath, "libs"));
            foreach (var item in libsDir.GetFiles())
            {
                string filename = Path.Combine(objPath, item.Name);
                File.Copy(item.FullName, filename, true);
                Log.LogMessage(MessageImportance.High, string.Format("copy file {0} -> {1}", item.FullName, filename));
            }

            Inject(objPath);
        }

        private void BuildInLibsDirectory()
        {
            //需要设置当前目录为libs  否则执行Inject会找不到 Leox.Aop.dll
            Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), "libs"));
            //var objPath = Path.Combine(_projectPath, "obj", "Debug");
            var binPath = Path.Combine(_projectPath, "bin", "Debug");

            Inject(binPath);
        }

        private void Inject(string path)
        {
            DirectoryInfo directory = new DirectoryInfo(path);
            var files = directory.GetFiles().Where(f => _fileSuffix.Any(s => f.Name.EndsWith(s)
              && !f.Name.Contains(".vshost") && !_excludeFiles.Any(a => a.Equals(f.Name))));

            var injector = new Injector.Injector();
            Exception ex = null;
            foreach (var item in files)
            {
                Log.LogMessage(MessageImportance.High, "find attr from " + item.FullName);
                var injected = injector.Inject(item.FullName, out ex);

                if (!injected)
                {
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

                GeneratePdb(item.DirectoryName, item.Name);
                //File.Copy(item.FullName, Path.Combine(binPath, item.Name), true);
                Log.LogMessage(MessageImportance.High, "inject finished .");
            }
        }

        private void GeneratePdb(string path, string fileName)
        {
            Log.LogMessage(MessageImportance.High, "generate pdb file ...");

            fileName = Path.ChangeExtension(fileName, "");
            fileName = fileName.Substring(0, fileName.Length - 1);
            var binPath = Path.Combine(_projectPath, "bin", "Debug");

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
                process.StandardInput.WriteLine("cd " + path);
                process.StandardInput.WriteLine(string.Format("ildasm {0}.exe /out={0}.il", fileName));
                process.StandardInput.WriteLine(string.Format("ilasm {0}.il /pdb", fileName));//
                process.StandardInput.WriteLine(string.Format("copy /y {0}.exe {1}", fileName, binPath));
                process.StandardInput.WriteLine(string.Format("copy /y {0}.pdb {1}", fileName, binPath));

                process.StandardInput.WriteLine("exit");
                string strRst = process.StandardOutput.ReadToEnd();

                process.WaitForExit();
                //process.Close();
                Log.LogMessage(strRst);
                Log.LogWarning(strRst);
            }
            Log.LogMessage(MessageImportance.High, "generate pdb finished .");
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
