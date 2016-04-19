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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DeployFast.App
{
    public static class MiscExtenders
    {
        private static Regex containerNameRegex = new Regex(
            @"^[a-z0-9](?:[a-z0-9]|(\-(?!\-))){1,61}[a-z0-9]$", RegexOptions.Compiled);

        public static string WithSlash(this string value)
        {
            value = value.Trim();

            if (!value.EndsWith(Path.DirectorySeparatorChar.ToString()))
                value += Path.DirectorySeparatorChar;

            return value;
        }

        public static double ToMB(this long value, int digits = 2)
        {
            return Math.Round((double)value / WellKnown.MB, digits);
        }

        public static double ToGB(this long value, int digits = 2)
        {
            return Math.Round((double)value / WellKnown.GB, digits);
        }

        public static bool IsContainerName(this string value)
        {
            return containerNameRegex.IsMatch(value);
        }

        public static bool InRange<T>(this T value, T minValue, T maxValue)
            where T : IComparable<T>
        {
            return (value.CompareTo(minValue) >= 0) && (value.CompareTo(maxValue) <= 0);
        }

        public static async Task<byte[]> GetFileContentAsync(
            this FileInfo file, long offset, int length)
        {
            using (var stream = file.OpenRead())
            {
                stream.Seek(offset, SeekOrigin.Begin);

                var contents = new byte[length];

                var len = await stream.ReadAsync(contents, 0, contents.Length);

                if (len == length)
                    return contents;

                var result = new byte[len];

                Array.Copy(contents, result, len);

                return result;
            }
        }

        public static Task ForEachAsync<T>(
            this IEnumerable<T> source, int parallelUploads, Func<T, Task> body)
        {
            return Task.WhenAll(Partitioner
                .Create(source)
                .GetPartitions(parallelUploads)
                .Select(partition => Task.Run(async () =>
                {
                    using (partition)
                    {
                        while (partition.MoveNext())
                            await body(partition.Current);
                    }
                })));
        }
    }
}
