// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace System.IO.Tests
{
    public class Perf_Directory
    {
        private const int CreateInnerIterations = 10;
        
        private readonly string _testFile = FileUtils.GetTestFilePath();
        private readonly IReadOnlyDictionary<int, string> _testDeepFilePaths = new Dictionary<int, string>
        {
            { 10, GetTestDeepFilePath(10) },
            { 100, GetTestDeepFilePath(100) },
            { 1000, GetTestDeepFilePath(1000) }
        };
        private string[] _directoriesToCreate;

        [Benchmark]
        public string GetCurrentDirectory() => Directory.GetCurrentDirectory();
        
        [GlobalSetup(Target = nameof(CreateDirectory))]
        public void SetupCreateDirectory()
        {
            var testFile = FileUtils.GetTestFilePath();
            _directoriesToCreate = Enumerable.Range(1, CreateInnerIterations).Select(index => testFile + index).ToArray();
        }

        [Benchmark(OperationsPerInvoke = CreateInnerIterations)]
        public void CreateDirectory()
        {
            var directoriesToCreate = _directoriesToCreate;
            foreach (var directory in directoriesToCreate)
                Directory.CreateDirectory(directory);
        }
        
        [IterationCleanup(Target = nameof(CreateDirectory))]
        public void CleanupDirectoryIteration()
        {
            foreach (var directory in _directoriesToCreate)
                Directory.Delete(directory);
        }

        [GlobalSetup(Target = nameof(Exists))]
        public void SetupExists() => Directory.CreateDirectory(_testFile);

        [Benchmark]
        public bool Exists() => Directory.Exists(_testFile);
        
        [GlobalCleanup(Target = nameof(Exists))]
        public void CleanupExists() => Directory.Delete(_testFile);

        public IEnumerable<object> RecursiveDepthData()
        {
            yield return 10;

            // Length of the path can be 260 characters on netfx.
            if (PathFeatures.AreAllLongPathsAvailable())
            {
                yield return 100;
                // Most Unix distributions have a maximum path length of 1024 characters (1024 UTF-8 bytes). 
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    yield return 1000;
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(RecursiveDepthData))]
        public void RecursiveCreateDeleteDirectory(int depth)
        {
            var root = _testFile;
            var name = root + Path.DirectorySeparatorChar + _testDeepFilePaths[depth];

            Directory.CreateDirectory(name);
            Directory.Delete(root, recursive: true);
        }

        private static string GetTestDeepFilePath(int depth)
        {
            string directory = Path.DirectorySeparatorChar + "a";
            StringBuilder sb = new StringBuilder(depth * 2);
            for (int i = 0; i < depth; i++)
            {
                sb.Append(directory);
            }

            return sb.ToString();
        }
    }
}
