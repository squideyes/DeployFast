#region Copyright, Author Details and Related Context  
//<notice lastUpdateOn="4/18/2016">  
//  <solution>DeployFast</solution> 
//  <assembly>DeployFast.App</assembly>  
//  <description>A simple Azure-mediated deployment utility</description>  
//  <copyright>  
//    Copyright (C) 2016 Louis S. Berman   

//    This program is free software: you can redistribute it and/or modify  
//    it under the terms of the GNU General Public License as published by  
//    the Free Software Foundation, either version 3 of the License, or  
//    (at your option) any later version.  
  
//    This program is distributed in the hope that it will be useful,  
//    but WITHOUT ANY WARRANTY; without even the implied warranty of  
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the  
//    GNU General Public License for more details.   

//    You should have received a copy of the GNU General Public License  
//    along with this program.  If not, see http://www.gnu.org/licenses/.  
//  </copyright>  
//  <author>  
//    <fullName>Louis S. Berman</fullName>  
//    <email>louis@squideyes.com</email>  
//    <website>http://squideyes.com</website>  
//  </author>  
//</notice>  
#endregion 

using DeployFast.Shared.Constants;
using DeployFast.Shared.Generics;
using DeployFast.Shared.Logging;
using DeployFast.Shared.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Nito.AsyncEx;
using SafeConfig;
using System;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace DeployFast.App
{
    class Program
    {
        private const string CREATING =
            "Creating the \"{0}\" {1}, if it doesn't already exist.";

        private class EventLogInfo
        {
            public Exception OriginalError { get; set; }
            public Exception LoggingError { get; set; }
        }

        private const string CONNSTRING = "ConnString";

        static void Main(string[] args)
        {
            try
            {
                AsyncContext.Run(() => DoWork(args));

                Environment.ExitCode = 1;
            }
            catch
            {
                Environment.ExitCode = -1;
            }
        }

        private static async Task DoWork(string[] args)
        {
            var parser = new ArgsParser<Options>();

            var options = parser.Parse(args);

            if (options == null)
                parser.ShowHelp();
            else if (options.DeleteConnString)
                DeleteConnString();
            else if (!string.IsNullOrWhiteSpace(options.ConnString))
                SaveConnString(options);
            else
                await ZipUploadAndDeploy(options);
        }

        private static async Task ZipUploadAndDeploy(Options options)
        {
            CloudTable logTable = null;
            Logger logger = null;

            try
            {
                var startedOn = DateTime.UtcNow;

                ////////////////////////////////////////////////////////////////

                var connString = new ConfigManager()
                    .AtFolder(Properties.Settings.Default.SettingsFolder)
                    .Load()
                    .Get<string>(CONNSTRING);

                if (connString == null)
                    throw new Exception("A deployment may not be kicked off before a connection string is first saved!");

                var account = CloudStorageAccount.Parse(connString);

                var blobClient = account.CreateCloudBlobClient();

                var tableClient = account.CreateCloudTableClient();

                var cts = new CancellationTokenSource();

                ////////////////////////////////////////////////////////////////

               logger = new Logger(typeof(Program), account, cts,
                   Properties.Settings.Default.MinSeverity);

                logger.LogToConsole(Severity.Info,
                    $"Deploying \"{options.SourcePath}\\*.*\"");

                await logger.Init();

                ////////////////////////////////////////////////////////////////

                var zipFileContainer = await CreateContainer(logger,
                    blobClient, options.AppId.ToString().ToLower(), logTable);

                ////////////////////////////////////////////////////////////////

                var deployContolTable = tableClient.GetTableReference(
                    WellKnown.ControlTableName);

                await logger.Log(Severity.Debug,
                    CREATING, WellKnown.ControlTableName, "table");

                var wasCreated = await deployContolTable.CreateIfNotExistsAsync();

                await logger.Log(Severity.Debug,
                    "The \"{0}\" {1} {2}.", WellKnown.ControlTableName,
                    "table", wasCreated ? "was created" : "already exists");

                ////////////////////////////////////////////////////////////////

                var zipFileName = Path.Combine(Path.GetTempPath(),
                    Guid.NewGuid().ToString("N") + ".zip");

                await logger.Log(Severity.Debug,
                    "Zipping \"{0}\\*.*\" into \"{1}\".",
                    options.SourcePath, zipFileName);

                ZipFile.CreateFromDirectory(options.SourcePath, zipFileName);

                await logger.Log(Severity.Debug,
                    $"The \"{zipFileName}\" archive was created!");

                ////////////////////////////////////////////////////////////////

                var blob = zipFileContainer.GetBlockBlobReference(
                    options.BuildName + ".zip");

                await logger.Log(Severity.Debug,
                    $"Uploading the \"{zipFileName}\" archive to \"{blob.Name}\".");

                //var uploader = new FileUploader(connString, 
                //    zipFileContainer.Name, Environment.ProcessorCount);

                //await uploader.UploadAsync(zipFileName, blob.Name);

                await blob.UploadFromFileAsync(zipFileName);

                await logger.Log(Severity.Info,
                    "The \"{0}\" blob was uploaded to the \"{1}\" container.",
                    blob.Name, zipFileContainer.Name);

                ////////////////////////////////////////////////////////////////

                File.Delete(zipFileName);

                await logger.Log(Severity.Debug,
                    $"The temporary \"{zipFileName}\" archive was deleted.");

                ////////////////////////////////////////////////////////////////

                await logger.Log(Severity.Debug,
                    "Upserting \"{0}\" table entries for \"{1}\".",
                    options.AppId, string.Join(",", options.HostNames));

                for (int i = 0; i < options.HostNames.Count; i++)
                {
                    var entity = new ControlEntity()
                    {
                        Timestamp = DateTime.UtcNow,
                        PartitionKey = options.HostNames[i],
                        RowKey = options.AppId.ToString(),
                        BlobName = options.BuildName + ".zip",
                        AlertTos = string.Join(";", options.AlertTos),
                        Status = (int)(i == options.HostNames.Count - 1 ?
                            DeployStatus.DeployNow : DeployStatus.CanDeploy)
                    };

                    await deployContolTable.ExecuteAsync(
                        TableOperation.InsertOrReplace(entity));

                    await logger.Log(Severity.Info,
                        "Upserted an \"{0}\" entry into the \"{1}\" table for \"{2}\".",
                        entity.RowKey, WellKnown.ControlTableName, entity.PartitionKey);
                }

                ////////////////////////////////////////////////////////////////

                await logger.Log(Severity.Debug,
                    $"Elapsed: {DateTime.UtcNow - startedOn}");
            }
            catch (Exception error)
            {
                try
                {
                    await logger.Log(error);
                }
                catch (Exception loggingError)
                {
                    logger.LogToConsole(Severity.Failure,
                        "The \"{0}\" error couldn't be logged.  See the EventLog for details.",
                        error.Message.ToSingleLine());

                    var info = new EventLogInfo()
                    {
                        OriginalError = error,
                        LoggingError = loggingError
                    };

                    var fileName = Path.Combine(
                        Properties.Settings.Default.FailureLogsPath,
                        string.Format("{0}_Failure_{1:yyyyMMdd_HHmmssff}.json",
                        typeof(Program).Namespace, DateTime.UtcNow));

                    if (!Directory.Exists(Properties.Settings.Default.FailureLogsPath))
                        Directory.CreateDirectory(Properties.Settings.Default.FailureLogsPath);

                    using (var writer = new StreamWriter(fileName))
                        writer.Write(JsonConvert.SerializeObject(info, Formatting.Indented));
                }

                throw;
            }
        }

        private static async Task<CloudBlobContainer> CreateContainer(
            Logger logger,  CloudBlobClient blobClient, 
            string containerName, CloudTable logTable)
        {
            var container = blobClient.GetContainerReference(
                containerName.ToLower());

            await logger.Log(Severity.Debug,
                CREATING, container.Name, "container");

            var wasCreated = await container.CreateIfNotExistsAsync();

            await logger.Log(Severity.Debug,
                "The \"{0}\" {1} {2}.", container.Name,
                "container", wasCreated ? "was created" : "already exists");

            return container;
        }

        private static void SaveConnString(Options options)
        {
            new ConfigManager()
                .WithLocalMachineScope()
                .Set(WellKnown.ConnStringName, options.ConnString)
                .AtFolder(Properties.Settings.Default.SettingsFolder)
                .Save();

            Console.WriteLine("The Azure Storage connection string was saved!");
        }

        private static void DeleteConnString()
        {
            if (Directory.Exists(
                Properties.Settings.Default.SettingsFolder))
            {
                Directory.Delete(
                    Properties.Settings.Default.SettingsFolder, true);
            }

            Console.WriteLine("The Azure Storage connection string was deleted!");
        }
    }
}
