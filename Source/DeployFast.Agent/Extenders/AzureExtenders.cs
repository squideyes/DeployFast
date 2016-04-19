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

using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DeployFast.Agent
{
    public static class AzureExtenders
    {
        public static async Task<IList<T>> ExecuteQueryAsync<T>(
            this CloudTable table, TableQuery<T> query, 
            CancellationToken ct = default(CancellationToken), 
            Action<IList<T>> onProgress = null) where T : ITableEntity, new()
        {
            var runningQuery = new TableQuery<T>()
            {
                FilterString = query.FilterString,
                SelectColumns = query.SelectColumns
            };

            var items = new List<T>();

            TableContinuationToken token = null;

            do
            {
                runningQuery.TakeCount = query.TakeCount - items.Count;

                var segment = await table
                    .ExecuteQuerySegmentedAsync<T>(runningQuery, token);

                token = segment.ContinuationToken;

                items.AddRange(segment);

                onProgress?.Invoke(items);
            }
            while (token != null && !ct.IsCancellationRequested &&
                (!query.TakeCount.HasValue || items.Count < query.TakeCount.Value));

            return items;
        }
    }
}
