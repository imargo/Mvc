﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Reader and, if GENERATE_BASELINES is defined, writer for files compiled into an assembly as resources.
    /// </summary>
    /// <remarks>Inspired by Razor's BaselineWriter and TestFile test classes.</remarks>
    public static class ResourceFile
    {
        private static object writeLock = new object();

        /// <summary>
        /// Return <see cref="Stream"/> for <paramref name="resourceName"/> from <paramref name="assembly"/>'s
        /// manifest.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> containing <paramref name="resourceName"/>.</param>
        /// <param name="resourceName">
        /// Name of the manifest resource in <paramref name="assembly"/>. A path relative to the test project
        /// directory.
        /// </param>
        /// <param name="sourceFile">
        /// If <c>true</c> <paramref name="resourceName"/> is used as a source file and must exist. Otherwise
        /// <paramref name="resourceName"/> is an output file and, if <c>GENERATE_BASELINES</c> is defined, it will
        /// soon be generated if missing.
        /// </param>
        /// <returns>
        /// <see cref="Stream"/> for <paramref name="resourceName"/> from <paramref name="assembly"/>'s
        /// manifest. <c>null</c> if <c>GENERATE_BASELINES</c> is defined, <paramref name="sourceFile"/> is
        /// <c>false</c>, and <paramref name="resourceName"/> is not found in <paramref name="assembly"/>.
        /// </returns>
        /// <exception cref="Xunit.Sdk.TrueException">
        /// Thrown if <c>GENERATE_BASELINES</c> is not defined or <paramref name="sourceFile"/> is <c>true</c> and
        /// <paramref name="resourceName"/> is not found in <paramref name="assembly"/>.
        /// </exception>
        public static Stream GetResourceStream(Assembly assembly, string resourceName, bool sourceFile)
        {
            // The DNX runtime compiles every file under the resources folder as a resource available at runtime with
            // the same name as the file name.
            var fullName = $"{ assembly.GetName().Name }.{ resourceName.Replace('/', '.') }";
            if (!Exists(assembly, fullName))
            {
#if GENERATE_BASELINES
                if (sourceFile)
                {
                    // Even when generating baselines, a missing source file is a serious problem.
                    Assert.True(false, $"Manifest resource: { fullName } not found.");
                }
#else
                // When not generating baselines, a missing source or output file is always an error.
                Assert.True(false, $"Manifest resource '{ fullName }' not found.");
#endif

                return null;
            }

            return assembly.GetManifestResourceStream(fullName);
        }

        /// <summary>
        /// Return <see cref="string"/> content of <paramref name="resourceName"/> from <paramref name="assembly"/>'s
        /// manifest.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> containing <paramref name="resourceName"/>.</param>
        /// <param name="resourceName">
        /// Name of the manifest resource in <paramref name="assembly"/>. A path relative to the test project
        /// directory.
        /// </param>
        /// <param name="sourceFile">
        /// If <c>true</c> <paramref name="resourceName"/> is used as a source file and must exist. Otherwise
        /// <paramref name="resourceName"/> is an output file and, if <c>GENERATE_BASELINES</c> is defined, it will
        /// soon be generated if missing.
        /// </param>
        /// <returns>
        /// A <see cref="Task{string}"/> which on completion returns the <see cref="string"/> content of
        /// <paramref name="resourceName"/> from <paramref name="assembly"/>'s manifest. <c>null</c> if
        /// <c>GENERATE_BASELINES</c> is defined, <paramref name="sourceFile"/> is <c>false</c>, and
        /// <paramref name="resourceName"/> is not found in <paramref name="assembly"/>.
        /// </returns>
        /// <exception cref="Xunit.Sdk.TrueException">
        /// Thrown if <c>GENERATE_BASELINES</c> is not defined or <paramref name="sourceFile"/> is <c>true</c> and
        /// <paramref name="resourceName"/> is not found in <paramref name="assembly"/>.
        /// </exception>
        /// <remarks>Normalizes line endings to <see cref="Environment.NewLine"/>.</remarks>
        public static async Task<string> ReadResourceAsync(Assembly assembly, string resourceName, bool sourceFile)
        {
            using (var stream = GetResourceStream(assembly, resourceName, sourceFile))
            {
                if (stream == null)
                {
                    return null;
                }

                using (var streamReader = new StreamReader(stream))
                {
                    var content = await streamReader.ReadToEndAsync();

                    // Normalize line endings to Environment.NewLine. This removes core.autocrlf, core.eol,
                    // core.safecrlf, and .gitattributes from the equation and matches what MVC returns.
                    return content
                        .Replace("\r", string.Empty)
                        .Replace("\n", Environment.NewLine);
                }
            }
        }

        /// <summary>
        /// Return <see cref="string"/> content of <paramref name="resourceName"/> from <paramref name="assembly"/>'s
        /// manifest.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> containing <paramref name="resourceName"/>.</param>
        /// <param name="resourceName">
        /// Name of the manifest resource in <paramref name="assembly"/>. A path relative to the test project
        /// directory.
        /// </param>
        /// <param name="sourceFile">
        /// If <c>true</c> <paramref name="resourceName"/> is used as a source file and must exist. Otherwise
        /// <paramref name="resourceName"/> is an output file and, if <c>GENERATE_BASELINES</c> is defined, it will
        /// soon be generated if missing.
        /// </param>
        /// <returns>
        /// The <see cref="string"/> content of <paramref name="resourceName"/> from <paramref name="assembly"/>'s
        /// manifest. <c>null</c> if <c>GENERATE_BASELINES</c> is defined, <paramref name="sourceFile"/> is
        /// <c>false</c>, and <paramref name="resourceName"/> is not found in <paramref name="assembly"/>.
        /// </returns>
        /// <exception cref="Xunit.Sdk.TrueException">
        /// Thrown if <c>GENERATE_BASELINES</c> is not defined or <paramref name="sourceFile"/> is <c>true</c> and
        /// <paramref name="resourceName"/> is not found in <paramref name="assembly"/>.
        /// </exception>
        /// <remarks>Normalizes line endings to <see cref="Environment.NewLine"/>.</remarks>
        public static string ReadResource(Assembly assembly, string resourceName, bool sourceFile)
        {
            using (var stream = GetResourceStream(assembly, resourceName, sourceFile))
            {
                if (stream == null)
                {
                    return null;
                }

                using (var streamReader = new StreamReader(stream))
                {
                    var content = streamReader.ReadToEnd();

                    // Normalize line endings to Environment.NewLine. This removes core.autocrlf, core.eol,
                    // core.safecrlf, and .gitattributes from the equation and matches what MVC returns.
                    return content
                        .Replace("\r", string.Empty)
                        .Replace("\n", Environment.NewLine);
                }
            }
        }

        /// <summary>
        /// Write <paramref name="content"/> to file that will become <paramref name="resourceName"/> in
        /// <paramref name="assembly"/> the next time the project is built. Does nothing if
        /// <paramref name="previousContent"/> and <paramref name="content"/> already match.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> containing <paramref name="resourceName"/>.</param>
        /// <param name="resourceName">
        /// Name of the manifest resource in <paramref name="assembly"/>. A path relative to the test project
        /// directory.
        /// </param>
        /// <param name="previousContent">
        /// Current content of <paramref name="resourceName"/>. <c>null</c> if <paramref name="resourceName"/> does
        /// not currently exist in <paramref name="assembly"/>.
        /// </param>
        /// <param name="content">
        /// New content of <paramref name="resourceName"/> in <paramref name="assembly"/>.
        /// </param>
        [Conditional("GENERATE_BASELINES")]
        public static void UpdateFile(Assembly assembly, string resourceName, string previousContent, string content)
        {
            if (!string.Equals(previousContent, content, StringComparison.Ordinal))
            {
                // The DNX runtime compiles every file under the resources folder as a resource available at runtime with
                // the same name as the file name. Need to update this file on disc.
                var projectName = assembly.GetName().Name;
                var projectPath = GetProjectPath(projectName);
                var fullPath = Path.Combine(projectPath, resourceName);
                WriteFile(fullPath, content);
            }
        }

        private static bool Exists(Assembly assembly, string fullName)
        {
            var resourceNames = assembly.GetManifestResourceNames();
            foreach (var resourceName in resourceNames)
            {
                // Resource names are case-sensitive.
                if (string.Equals(fullName, resourceName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetProjectPath(string projectName)
        {
            // Initial guess: Already in the project directory.
            var projectPath = Path.GetFullPath(".");

            var currentDirectoryName = new DirectoryInfo(projectPath).Name;
            if (!string.Equals(projectName, currentDirectoryName, StringComparison.Ordinal))
            {
                // Not running from test project directory. Should be in "test" or solution directory.
                if (string.Equals("test", currentDirectoryName, StringComparison.Ordinal))
                {
                    projectPath = Path.Combine(projectPath, projectName);
                }
                else
                {
                    projectPath = Path.Combine(projectPath, "test", projectName);
                }
            }

            return projectPath;
        }

        private static void WriteFile(string fullPath, string content)
        {
            // Serialize writes to minimize contention for file handles and directory access.
            lock (writeLock)
            {
                // Write content to the file, creating it if necessary.
                using (var stream = File.Open(fullPath, FileMode.Create, FileAccess.Write))
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(content);
                    }
                }
            }
        }
    }
}
