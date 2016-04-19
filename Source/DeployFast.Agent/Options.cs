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

using DeployFast.Shared.Generics;
using Microsoft.WindowsAzure.Storage;

namespace DeployFast.Agent
{
    public class Options : IOptions
    {
        [Option("CONN", "conn", 2, true,
            "The Azure Storage connection-string that is associated with DeployFast.Agent.exe.  If a /CONN value was previously saved to the local system, it will be overwritten.  In any case, the /CONN value will be persisted to the local machine using DPAPI security.")]
        public string ConnString { get; set; }

        [Option("DELETECONN", null, 3, true,
            "Indicates that any previously saved /CONN value should be deleted.  The /DELETECONN flag may only be used by iteself.")]
        public bool DeleteConnString { get; set; }

        public bool GetIsValid()
        {
            if (DeleteConnString)
            {
                if (!string.IsNullOrWhiteSpace(ConnString))
                    return false;
            }
            else if (!string.IsNullOrWhiteSpace(ConnString))
            {
                if (DeleteConnString)
                    return false;

                CloudStorageAccount account;

                if (!CloudStorageAccount.TryParse(ConnString, out account))
                    return false;
            }

            return true;
        }
    }
}
