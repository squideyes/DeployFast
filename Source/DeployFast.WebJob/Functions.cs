#region Copyright, Author Details and Related Context  
//<notice lastUpdateOn="4/18/2016">  
//  <solution>DeployFast</solution> 
//  <assembly>DeployFast.WebJob</assembly>  
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
using DeployFast.Shared.Models;
using Microsoft.Azure.WebJobs;
using SendGrid;
using System.Configuration;
using System.IO;
using System.Net.Mail;
using System.Text;

namespace DeployFast.WebJob
{
    public class Functions
    {
        public static void ProcessQueueMessage(
            [QueueTrigger("alerts")] AlertInfo info,
            TextWriter log,
            [SendGrid] SendGridMessage message)
        {
            message.From = new MailAddress(
                ConfigurationManager.AppSettings["AlertFrom"]);

            var alertTos = ConfigurationManager.AppSettings["AlertTos"];

            foreach (var alertTo in alertTos.Split(',', ';'))
                message.AddTo(alertTo);

            message.Subject = $"[DeployFast {info.Outcome}] - {info.ServerName}/{info.AppId}";

            var text = new StringBuilder();

            text.AppendLine($"Server:    {info.ServerName}");
            text.AppendLine($"App ID:    {info.AppId}");
            text.AppendLine($"Blob Name: {info.BlobName}");
            text.AppendLine($"Posted On: {info.PostedOn}");
            text.AppendLine($"Outcome:   {info.Outcome}");

            if (info.Outcome == Outcome.Error)
            {
                text.AppendLine();
                text.AppendLine("Error:");
                text.AppendLine(info.Error.ToString());
            }

            text.AppendLine();

            message.Text = text.ToString();

            log.WriteLine($"Sent {info} alert to \"{alertTos}\"");
        }
    }
}
