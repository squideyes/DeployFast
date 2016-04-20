#region Copyright, Author Details and Related Context  
//<notice lastUpdateOn="4/18/2016">  
//  <solution>DeployFast</solution> 
//  <assembly>DeployFast.Agent</assembly>  
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
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using DeployFast.Shared.Models;
using DeployFast.Shared.Generics;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Queue;
using DeployFast.Shared;
using DeployFast.Shared.Logging;
using SafeConfig;

namespace DeployFast.Agent
{
    public class Worker
    {
        private CloudTable controlTable;
        private CloudQueue alertQueue;
        private int pollDelay;
        private CloudBlobClient blobClient;
        private Logger logger;

        private Dictionary<string, string> deployTos =
            new Dictionary<string, string>();

        public Worker(int pollAfterSeconds)
        {
            if (pollAfterSeconds < 10)
                throw new ArgumentOutOfRangeException(nameof(pollAfterSeconds));

            pollDelay = pollAfterSeconds * 1000;

            CancellationTokenSource = new CancellationTokenSource();

            var section = ConfigurationManager.GetSection("deployTos")
                 as DeployTosSection;

            foreach (DeployToElement e in section.Instances)
                deployTos.Add(e.AppId, e.DeployTo);

            var connString = new ConfigManager()
                .AtFolder(ConfigurationManager.AppSettings["SettingsFolder"])
                //.WithCurrentUserScope()  // discusss issues with team
                .Load()
                .Get<string>("ConnString"); // make WellKnown            

            var account = CloudStorageAccount.Parse(connString);

            var minSeverity = ConfigurationManager
                .AppSettings["MinSeverity"].ToEnum<Severity>();

            logger = new Logger(this.GetType(), account,
                CancellationTokenSource, minSeverity);

            var tableClient = account.CreateCloudTableClient();

            controlTable = tableClient.GetTableReference(
                WellKnown.ControlTableName);

            blobClient = account.CreateCloudBlobClient();

            var queueClient = account.CreateCloudQueueClient();

            alertQueue = queueClient.GetQueueReference(WellKnown.AlertQueueName);
        }

        public CancellationTokenSource CancellationTokenSource { get; private set; }

        public async Task Start()
        {
            // do I need this????????????????????
            await alertQueue.CreateIfNotExistsAsync();

            CancellationTokenSource = new CancellationTokenSource();

            while (!CancellationTokenSource.IsCancellationRequested)
            {
                var query = new TableQuery<ControlEntity>().Where(
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey",
                            QueryComparisons.Equal, Environment.MachineName),
                        TableOperators.And,
                        TableQuery.GenerateFilterConditionForInt("Status",
                            QueryComparisons.Equal, (int)DeployStatus.DeployNow)));

                var entities = await controlTable.ExecuteQueryAsync<ControlEntity>(
                    query, CancellationTokenSource.Token);

                if (CancellationTokenSource.IsCancellationRequested)
                    return;

                foreach (var entity in entities)
                {
                    await Deploy(entity);

                    if (CancellationTokenSource.IsCancellationRequested)
                        break;
                }

                await Task.Delay(pollDelay, CancellationTokenSource.Token);
            }
        }

        public void Stop()
        {
            CancellationTokenSource.Cancel();
        }

        private async Task Deploy(ControlEntity entity)
        {
            var alertInfo = new AlertInfo()
            {
                PostedOn = DateTime.UtcNow,
                ServerName = entity.PartitionKey,
                AppId = entity.RowKey.ToEnum<AppId>(),
                BlobName = entity.BlobName,
                AlertTos = entity.AlertTos.Split(';').ToList()
            };

            try
            {
                await logger.Log(Severity.Debug, $"Deploying \"{entity.BlobName}\"");

                //////////////////////////////////////////////////////////////////

                var container = blobClient.GetContainerReference(
                    entity.RowKey.ToLower());

                var blob = container.GetBlockBlobReference(entity.BlobName);

                await logger.Log(Severity.Debug, $"Downloading \"{blob.Uri}\"");

                var sourceFileName = Path.GetTempFileName();

                await blob.DownloadToFileAsync(sourceFileName, FileMode.Create);

                await logger.Log(Severity.Debug, $"Successfully downloaded \"{blob.Uri}\"");

                //////////////////////////////////////////////////////////////////

                if (CancellationTokenSource.IsCancellationRequested)
                    return;

                var filesToSkip = new HashSet<string>(
                    ConfigurationManager.AppSettings["FilesToSkip"]
                    .Split(';').Select(fts => fts.ToLower()));

                var tempPath = Path.Combine(
                    Path.GetTempPath(), Guid.NewGuid().ToString("N"));

                await logger.Log(Severity.Debug,
                    $"Unzipping \"{sourceFileName}\" to \"{tempPath}\"");

                int count = 0;

                using (var archive = ZipFile.OpenRead(sourceFileName))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (CancellationTokenSource.IsCancellationRequested)
                            return;

                        if (filesToSkip.Contains(entry.Name.ToLower()))
                            continue;

                        var saveTo = Path.GetFullPath(
                            Path.Combine(tempPath, entry.FullName));

                        saveTo.EnsurePathExists();

                        entry.ExtractToFile(saveTo, true);

                        count++;
                    }
                }

                if (CancellationTokenSource.IsCancellationRequested)
                    return;

                await logger.Log(Severity.Info,
                    $"Unzipped {count:N0} files from \"{sourceFileName}\" to \"{tempPath}\"");

                //////////////////////////////////////////////////////////////////

                if (CancellationTokenSource.IsCancellationRequested)
                    return;

                var deployTo = deployTos[entity.RowKey].WithSlash();

                await logger.Log(Severity.Debug,
                    $"Deleting all non-skipped files from \"{deployTo}\"");

                count = 0;

                deployTo.EnsurePathExists();

                var fileNames = Directory.GetFiles(
                    deployTo, "*.*", SearchOption.AllDirectories);

                foreach (var fileName in fileNames)
                {
                    if (CancellationTokenSource.IsCancellationRequested)
                        return;

                    var nameOnly = Path.GetFileName(fileName).ToLower();

                    if (filesToSkip.Contains(nameOnly))
                        continue;

                    File.Delete(fileName);

                    count++;
                }

                await logger.Log(Severity.Info,
                    $"Deleted {count:N0} files from \"{deployTo}\"");

                //////////////////////////////////////////////////////////////////

                if (CancellationTokenSource.IsCancellationRequested)
                    return;

                var filesToMove = Directory.GetFiles(tempPath, "*.*",
                    SearchOption.AllDirectories).ToList();

                await logger.Log(Severity.Debug,
                    $"Copying {filesToMove.Count:N0} files to \"{deployTo}\"");

                foreach (var fileName in filesToMove)
                {
                    if (CancellationTokenSource.IsCancellationRequested)
                        return;

                    var destFileName = Path.Combine(deployTo,
                        fileName.Substring(tempPath.Length + 1));

                    destFileName.EnsurePathExists();

                    File.Move(fileName, destFileName);
                }

                await logger.Log(Severity.Info,
                    $"Copied {filesToMove.Count:N0} files to \"{deployTo}\"");

                //////////////////////////////////////////////////////////////////

                await UpdateStatus(entity, DeployStatus.Success);

                if (CancellationTokenSource.IsCancellationRequested)
                    return;

                //////////////////////////////////////////////////////////////////

                Directory.Delete(tempPath, true);

                File.Delete(sourceFileName);

                //////////////////////////////////////////////////////////////////

                if (CancellationTokenSource.IsCancellationRequested)
                    return;

                await DeployNextCanDeploy(entity.RowKey);
            }
            catch (Exception error)
            {
                alertInfo.Error = error;

                await UpdateStatus(entity, DeployStatus.Error);
            }

            if (CancellationTokenSource.IsCancellationRequested)
                return;

            await alertQueue.AddMessageAsync(new CloudQueueMessage(
                JsonConvert.SerializeObject(alertInfo)));
        }

        private async Task DeployNextCanDeploy(string rowKey)
        {
            var query = new TableQuery<ControlEntity>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("RowKey",
                        QueryComparisons.Equal, rowKey),
                TableOperators.And,
                TableQuery.GenerateFilterConditionForInt("Status",
                    QueryComparisons.Equal, (int)DeployStatus.CanDeploy)));

            var entities = await controlTable.ExecuteQueryAsync<ControlEntity>(
                query, CancellationTokenSource.Token);

            foreach (var entity in entities)
            {
                await UpdateStatus(entity, DeployStatus.DeployNow);

                break;
            }
        }

        private async Task UpdateStatus(ControlEntity entity, DeployStatus status)
        {
            await logger.Log(Severity.Debug,
                $"Updating Control.Status for {Environment.MachineName}/{entity.RowKey} to {status}");

            var retrieveOperation = TableOperation
                .Retrieve<ControlEntity>(Environment.MachineName, entity.RowKey);

            if (CancellationTokenSource.IsCancellationRequested)
                return;

            var retrievedResult = await controlTable.ExecuteAsync(retrieveOperation);

            var updateEntity = (ControlEntity)retrievedResult.Result;

            if (updateEntity == null)
                throw new Exception($"The \"{Environment.MachineName}\" entity is unexpectedly missing!");

            updateEntity.Status = (int)status;

            var updateOperation = TableOperation.Replace(updateEntity);

            if (CancellationTokenSource.IsCancellationRequested)
                return;

            await controlTable.ExecuteAsync(updateOperation);

            await logger.Log(Severity.Debug,
                $"Updated Control.Status for {Environment.MachineName}/{entity.RowKey} to {status}");
        }
    }
}
