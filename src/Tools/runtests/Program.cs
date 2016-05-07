﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace runtests
{
    class Program
    {
        enum TestResult
        {
            Unknown,
            Succeeded,
            Failed,
            Crashed,
        }

        static void Main(string[] args)
        {
            // parse args
            string cwd = Environment.CurrentDirectory;
            var testdirs = new List<string>();
            string phpexepath = Path.Combine(cwd, "php.exe");

            foreach (var arg in args)
            {
                if (arg.EndsWith("php.exe", StringComparison.OrdinalIgnoreCase))
                {
                    phpexepath = arg;
                }
                else
                {
                    testdirs.Add(Path.GetFullPath(Path.Combine(cwd, arg)));
                }
            }

            // run tests
            var results = testdirs
                .SelectMany(dir => ExpandTestDir(dir))
                .Select(testpath => new
                {
                    Test = testpath,
                    Result = TestCore(testpath, phpexepath)
                }).ToList();

            // output results
            foreach (var result in results)
            {
                Console.WriteLine($"{result.Test} ... {result.Result}");
            }
        }

        static IEnumerable<string> ExpandTestDir(string testdir)
        {
            return Directory.EnumerateFiles(testdir, "*.php", SearchOption.AllDirectories);
        }

        static TestResult TestCore(string testpath, string phpexepath)
        {
            var testfname = Path.GetFileName(testpath);
            var outputexe = Path.Combine(testfname + ".exe");   // current dir so it finds pchpcor.dll etc.

            // php.exe 'testpath' >> OUTPUT1
            var phpoutput = RunProcess(phpexepath, testpath);

            // pchp.exe /target:exe '/out:testpath.exe' 'testpath'
            var compileroutput = RunProcess("pchp.exe", $"/target:exe \"/out:{outputexe}\" \"{testpath}\"");

            // TODO: check compiler crashed

            // testpath.exe >> OUTPUT2
            var output = RunProcess(outputexe, string.Empty);

            if (output == phpoutput)
            {
                return TestResult.Succeeded;
            }

            // TODO: log details

            //
            return TestResult.Failed;
        }

        static string RunProcess(string exe, string args)
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo(exe, args)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8,
                UseShellExecute = false,
            };

            //
            process.Start();

            // To avoid deadlocks, always read the output stream first and then wait.
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            //
            if (!string.IsNullOrEmpty(error))
                return error;

            //
            return output;
        }
    }
}
