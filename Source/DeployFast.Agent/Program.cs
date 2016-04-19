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
using SafeConfig;
using System;
using System.Configuration;
using System.IO;
using Topshelf;

namespace DeployFast.Agent
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length>0)
            {
                var cmd = string.Join(" ", args);

                CloudStorageAccount account;

                if (cmd.Equals("DELETECONN",
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    var folder = ConfigurationManager
                        .AppSettings["SettingsFolder"];

                    if (Directory.Exists(folder))
                        Directory.Delete(folder, true);

                    Console.WriteLine(
                        "The Azure Storage connection string was deleted!");
                }
                else if (CloudStorageAccount.TryParse(cmd, out account))
                {
                    new ConfigManager()
                        .WithLocalMachineScope()
                        .Set(WellKnown.ConnStringName, cmd)
                        .AtFolder(ConfigurationManager
                            .AppSettings["SettingsFolder"])
                        .Save();

                    Console.WriteLine(
                        "The Azure Storage connection string was saved!");
                }
                else
                {
                    RunService();
                }
            }
        }

        private static void RunService()
        {
            HostFactory.Run(x =>
            {
                x.Service<AgentService>(s =>
                {
                    s.ConstructUsing(name => new AgentService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });

                x.RunAsLocalSystem();

                x.SetDescription("FastDeploy Agent");
                x.SetDisplayName("FastDeploy Agent");
                x.SetServiceName("DeployFast");
            });
        }
    }
}
