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
using System.Threading;

namespace DeployFast.App
{
    public class Statistics
    {
        private readonly object addLock = new object();
        private long bytesUploaded = 0;

        internal Statistics(string fileName, long bytesToUpload)
        {
            StartedOn = DateTime.UtcNow;

            FileName = fileName;
            BytesToUpload = bytesToUpload;

            IsFinished = false;
        }

        public string FileName { get; }
        public DateTime StartedOn { get; }
        public long BytesToUpload { get; }

        public bool IsFinished { get; set; }

        public TimeSpan Elapsed { get; private set; }
        public double? GbPerHour { get; private set; }

        public long BytesUploaded
        {
            get
            {
                return bytesUploaded;
            }
        }

        public void AddToBytesUploaded(long bytesToUpload)
        {
            var bytesUploaded = Interlocked.Add(ref this.bytesUploaded, bytesToUpload);
        }

        public void Finished()
        {
            IsFinished = true;

            Elapsed = DateTime.UtcNow.Subtract(StartedOn);

            var bytesPerMillisecond = BytesUploaded / Elapsed.TotalMilliseconds;

            GbPerHour = Math.Round((bytesPerMillisecond * 1000 * 60 * 60) / WellKnown.GB, 2);
        }
    }
}
