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

using DeployFast.Shared;
using DeployFast.Shared.Generics;
using Microsoft.WindowsAzure.Storage;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DeployFast.App
{
    public class Options : IOptions
    {
        [Option("SOURCE", "path", 1, true,
            "A source / drop-folder path that contains a collection of deployable application files.")]
        public string SourcePath { get; set; }

        [Option("APPID", "id", 1, true,
            "The kind of deployment to perform (i.e. Application, Jobs, AuthService or ConnectService).")]
        public AppId AppId { get; set; }

        [Option("BUILD", "name", 1, true,
            "A TFS build name that will be used to generate the blob name of the zipped and uploaded deployment archive.")]
        public string BuildName { get; set; }

        [Option("HOSTS", "names", 1, true,
            "A comma-separated list of hosts / servers to deploy the /SOURCE files and folders to.")]
        public List<string> HostNames { get; set; }

        [Option("ALERT", "emails", 1, true,
            "A comma-separated list of email addresses to send alerts to.")]
        public List<string> AlertTos { get; set; }

        [Option("CONN", "conn", 2, true,
            "The Azure Storage connection-string that is associated with DeployFast.Agent.exe.  If a /CONN value was previously saved to the local system, it will be overwritten.  In any case, the /CONN value will be persisted to the local machine using DPAPI security.")]
        public string ConnString { get; set; }

        [Option("DELETECONN", null, 3, true,
            "Indicates that any previously saved /CONN value should be permanently deleted.")]
        public bool DeleteConnString { get; set; }

        private bool DeployFieldsEmpty()
        {
            if (!string.IsNullOrWhiteSpace(SourcePath))
                return false;

            if (AppId.IsDefined())
                return false;

            if (!string.IsNullOrWhiteSpace(BuildName))
                return false;

            if (HostNames != null)
                return false;

            return true;
        }

        private bool IsConnString()
        {
            CloudStorageAccount account;

            return CloudStorageAccount.TryParse(ConnString, out account);
        }

        public bool GetIsValid()
        {
            if (DeleteConnString)
            {
                if (!DeployFieldsEmpty())
                    return false;

                if (!string.IsNullOrWhiteSpace(ConnString))
                    return false;
            }
            else if (!string.IsNullOrWhiteSpace(ConnString))
            {
                if (!DeployFieldsEmpty())
                    return false;

                if (DeleteConnString)
                    return false;

                if (!IsConnString())
                    return false;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(SourcePath))
                    return false;
                else if (SourcePath.IndexOfAny(Path.GetInvalidPathChars()) != -1)
                    return false;

                if (!AppId.IsDefined())
                    return false;

                if (string.IsNullOrWhiteSpace(BuildName))
                    return false;

                if (HostNames == null || HostNames.Count == 0)
                    return false;

                if (AlertTos == null || AlertTos.Count == 0 ||
                    AlertTos.Any(alertTo => !alertTo.IsEmail()))
                {
                    return false;
                }

                if (DeleteConnString)
                    return false;

                if (!string.IsNullOrWhiteSpace(ConnString))
                    return false;
            }

            return true;
        }
    }
}
