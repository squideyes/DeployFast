﻿#region Copyright, Author Details and Related Context  
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

using System.Configuration;
using System.Threading.Tasks;

namespace DeployFast.Agent
{
    public class AgentService
    {
        private Worker worker = null;

        public AgentService()
        {
        }

        public void Start()
        {
            worker = new Worker(int.Parse(ConfigurationManager
                .AppSettings["PollingSeconds"]));

            Task.Factory.StartNew(() => worker.Start(), 
                worker.CancellationTokenSource.Token);
        }

        public void Stop()
        {
            if (worker != null)
                worker.Stop();
        }
    }
}
