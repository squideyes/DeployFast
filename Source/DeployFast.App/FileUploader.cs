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
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DeployFast.App
{
    public class FileUploader
    {
        public class ProgressArgs : EventArgs
        {
            public ProgressArgs(Statistics statistics)
            {
                Statistics = statistics;
            }

            public Statistics Statistics { get; }
        }

        private class BlockInfo
        {
            internal BlockInfo(int id, long length, int bytesPerChunk)
            {
                Id = id;
                BlockId = Convert.ToBase64String(BitConverter.GetBytes(id));
                Index = (long)id * bytesPerChunk;
                Length = (int)Math.Min(length - Index, bytesPerChunk);
            }

            public int Id { get; private set; }
            public string BlockId { get; private set; }
            public long Index { get; private set; }
            public int Length { get; private set; }
        }

        private CancellationTokenSource cts;
        private CloudStorageAccount account;
        private string containerName;
        private int partitionCount;
        private int bytesPerBlock;

        public event EventHandler<ProgressArgs> OnProgress;

        public FileUploader(string connString, string containerName, int partitionCount,
            int kbPerBlock = WellKnown.MaxBytesPerBlock)
        {
            if (!CloudStorageAccount.TryParse(connString, out account))
                throw new ArgumentOutOfRangeException(nameof(connString));

            if (!containerName.IsContainerName())
                throw new ArgumentOutOfRangeException(nameof(containerName));

            if (!partitionCount.InRange(1, 64))
                throw new ArgumentOutOfRangeException(nameof(partitionCount));

            var bytesPerBlock = kbPerBlock * 1024;

            if (!bytesPerBlock.InRange(1024, WellKnown.MaxBytesPerBlock))
                throw new ArgumentOutOfRangeException(nameof(kbPerBlock));

            if (bytesPerBlock % 1024 != 0)
                throw new ArgumentOutOfRangeException(nameof(kbPerBlock));

            this.containerName = containerName;
            this.partitionCount = partitionCount;
            this.bytesPerBlock = bytesPerBlock;
        }

        public async Task<string> UploadAsync(string fileName, string blobName)
        {
            var fileInfo = new FileInfo(fileName);

            if (!fileInfo.Exists)
            {
                throw new ArgumentException(string.Format(
                    "The \"{0}\" file doesn't exist!", fileInfo.Name));
            }

            cts = new CancellationTokenSource();

            var allBlocks = Enumerable.Range(0, 1 + ((int)(fileInfo.Length / bytesPerBlock)))
                 .Select(id => new BlockInfo(id, fileInfo.Length, bytesPerBlock))
                 .Where(block => block.Length > 0)
                 .ToList();

            var blockIds = allBlocks.Select(bi => bi.BlockId).ToList();

            Func<long, int, Task<byte[]>> fetchLocalData =
                (offset, length) => fileInfo.GetFileContentAsync(offset, length);

            var blobClient = account.CreateCloudBlobClient();

            var container = blobClient.GetContainerReference(containerName);

            await container.CreateIfNotExistsAsync(cts.Token);

            //if (cts.IsCancellationRequested)
            //    return;

            var blob = container.GetBlockBlobReference(blobName);

            List<BlockInfo> missingBlocks = null;

            try
            {
                var existingBlocks = (await blob.DownloadBlockListAsync(
                        BlockListingFilter.Uncommitted,
                        AccessCondition.GenerateEmptyCondition(),
                        new BlobRequestOptions(),
                        new OperationContext(), cts.Token))
                    .Where(lbi => lbi.Length == bytesPerBlock)
                    .ToList();

                //if (cts.IsCancellationRequested)
                //    return;

                missingBlocks = allBlocks.Where(bi => !existingBlocks.Any(
                    existingBlock => existingBlock.Name == bi.BlockId &&
                        existingBlock.Length == bi.Length)).ToList();
            }
            catch (StorageException)
            {
                missingBlocks = allBlocks;
            }

            Func<BlockInfo, Statistics, Task> uploadBlockAsync =
                async (block, stats) =>
                {
                    var blockData = await fetchLocalData(block.Index, block.Length);

                    var contentHash = GetMd5Func()(blockData);

                    await ExecuteUntilSuccessAsync(async () =>
                    {
                        await blob.PutBlockAsync(
                            blockId: block.BlockId,
                            blockData: new MemoryStream(blockData, true),
                            contentMD5: contentHash,
                            accessCondition: AccessCondition.GenerateEmptyCondition(),
                            options: new BlobRequestOptions
                            {
                                StoreBlobContentMD5 = true,
                                UseTransactionalMD5 = true
                            },
                            operationContext: new OperationContext(),
                            cancellationToken: cts.Token);
                    },
                    consoleExceptionHandler);

                    stats.AddToBytesUploaded(block.Length);

                    OnProgress?.Invoke(this, new ProgressArgs(stats));
                };

            var s = new Statistics(fileName, missingBlocks.Sum(b => b.Length));

            await missingBlocks.ForEachAsync(partitionCount, bi => uploadBlockAsync(bi, s));

            await ExecuteUntilSuccessAsync(
                async () => await blob.PutBlockListAsync(blockIds),
                consoleExceptionHandler);

            s.Finished();

            OnProgress?.Invoke(this, new ProgressArgs(s));

            return blob.Uri.AbsoluteUri;
        }

        public void Cancel()
        {
            if (cts != null)
                cts.Cancel();
        }

        internal static void consoleExceptionHandler(Exception error)
        {
            Console.WriteLine("Problem occured, trying again. Details of the problem: ");

            for (var e = error; e != null; e = e.InnerException)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("---------------------------------------------------------------------");
            Console.WriteLine(error.StackTrace);
            Console.WriteLine("---------------------------------------------------------------------");
        }

        public static async Task ExecuteUntilSuccessAsync(Func<Task> action, Action<Exception> exceptionHandler)
        {
            var success = false;

            while (!success)
            {

                try
                {
                    await action();

                    success = true;
                }
                catch (Exception error)
                {
                    exceptionHandler?.Invoke(error);
                }
            }
        }

        private static Func<byte[], string> GetMd5Func()
        {
            var hashFunction = MD5.Create();

            return (content) => Convert.ToBase64String(hashFunction.ComputeHash(content));
        }
    }
}
