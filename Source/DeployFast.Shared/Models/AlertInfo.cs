#region Copyright, Author Details and Related Context  
//<notice lastUpdateOn="4/18/2016">  
//  <solution>DeployFast</solution> 
//  <assembly>DeployFast.Shared.Models</assembly>  
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

using System;
using System.Collections.Generic;

namespace DeployFast.Shared.Models
{
    public class AlertInfo
    {
        public DateTime PostedOn { get; set; }
        public string ServerName { get; set; }
        public AppId AppId { get; set; }
        public string BlobName { get; set; }
        public List<string> AlertTos { get; set; }
        public Exception Error { get; set; }

        public Outcome Outcome => Error != null ?
            Outcome.Error : Outcome.Success;

        public override string ToString() =>
            $"{ServerName}/{AppId} {Outcome}";
    }
}
