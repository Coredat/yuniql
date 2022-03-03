﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Yuniql.Core
{
    /// <summary>
    /// Wraps usage of <see cref="Directory"/>
    /// </summary>
    public class DirectoryService : IDirectoryService
    {
        ///<inheritdoc/>
        public string[] GetDirectories(string path, string searchPattern)
        {
            return Directory.GetDirectories(path, searchPattern);
        }

        ///<inheritdoc/>
        public string[] GetAllDirectories(string path, string searchPattern)
        {
            return Directory.GetDirectories(path, searchPattern, SearchOption.AllDirectories);
        }

        ///<inheritdoc/>
        public string[] GetFiles(string path, string searchPattern)
        {
            return Directory.GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        ///<inheritdoc/>
        public string[] GetAllFiles(string path, string searchPattern)
        {
            return Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories);
        }

        ///<inheritdoc/>
        public bool Exists(string path)
        {
            return Directory.Exists(path);
        }

        ///<inheritdoc/>
        public string GetFileCaseInsensitive(string path, string fileName)
        {
            return Directory.GetFiles(path, "*.dll")
                .ToList()
                .FirstOrDefault(f => new FileInfo(f).Name.ToLower() == fileName.ToLower());
        }

        ///<inheritdoc/>
        public string[] FilterFiles(string workingPath, string[] environmentCodes, List<string> files)
        {
            var reservedDirectories = new List<string>
            {
                RESERVED_DIRECTORY_NAME.INIT,
                RESERVED_DIRECTORY_NAME.PRE,
                RESERVED_DIRECTORY_NAME.DRAFT,
                RESERVED_DIRECTORY_NAME.POST,
                RESERVED_DIRECTORY_NAME.ERASE,
                RESERVED_DIRECTORY_NAME.DROP,
                RESERVED_DIRECTORY_NAME.TRANSACTION,
            };

            var directoryPathParts = Split(new DirectoryInfo(workingPath)).ToList();
            directoryPathParts.Reverse();

            //check for any presence of an environment-specific directory
            //those are those that starts with "_" such as "_dev", "_test", "_prod" but not the known reserved names
            var hasEnvironmentAwareDirectories = files.Any(f =>
            {
                var filePathParts = Split(new DirectoryInfo(Path.GetDirectoryName(f)))
                    .Where(x => !x.Equals(RESERVED_DIRECTORY_NAME.TRANSACTION, System.StringComparison.InvariantCultureIgnoreCase))
                    .ToList();
                filePathParts.Reverse();
                
                return filePathParts.Skip(directoryPathParts.Count).Any(a => 
                    a.StartsWith(RESERVED_DIRECTORY_NAME.PREFIX) 
                    && !reservedDirectories.Exists(x => x.Equals(a, System.StringComparison.InvariantCultureIgnoreCase))
                );
            });

            if ((environmentCodes == null || environmentCodes.Length == 0) && !hasEnvironmentAwareDirectories)
                return files.ToArray();

            //throws exception when no environment code passed but environment-aware directories are present
            if ((environmentCodes == null || environmentCodes.Length == 0) && hasEnvironmentAwareDirectories)
                throw new YuniqlMigrationException("Found environment aware directories but no environment code passed. " +
                    "See https://github.com/rdagumampan/yuniql/wiki/environment-aware-scripts.");

            var possibleEnvironmentFolders = (environmentCodes ?? Array.Empty<string>())
                    .Select(x => $"{RESERVED_DIRECTORY_NAME.PREFIX}{x}")
                    .GenerateCombinations()
                    .Where(x => x.Length > 0)
                    .Select(x => String.Join("", x))
                    .ToHashSet();

            //remove all script files from environment-aware directories except the target environment
            var sqlScriptFiles = new List<string>(files);
            files.ForEach(f =>
            {
                var fileParts = Split(new DirectoryInfo(Path.GetDirectoryName(f))).Where(x => !x.Equals(RESERVED_DIRECTORY_NAME.TRANSACTION, System.StringComparison.InvariantCultureIgnoreCase)).ToList();
                fileParts.Reverse();

                var foundFile = fileParts.Skip(directoryPathParts.Count).FirstOrDefault(a => {

                    return a.StartsWith(RESERVED_DIRECTORY_NAME.PREFIX) && !possibleEnvironmentFolders.Contains(a.ToLower());
                });
                if (null != foundFile)
                    sqlScriptFiles.Remove(f);
            });

            return sqlScriptFiles.ToArray();
        }

        ///<inheritdoc/>
        public string[] FilterDirectories(string workingPath, string[] environmentCodes, List<string> directories)
        {
            throw new System.NotImplementedException();
        }

        private IEnumerable<string> Split(DirectoryInfo directory)
        {
            while (directory != null)
            {
                yield return directory.Name;
                directory = directory.Parent;
            }
        }

        ///<inheritdoc/>
        public DirectoryInfo CreateDirectory(string path)
        {
            return Directory.CreateDirectory(path);
        }
    }
}
