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

using System.Configuration;

namespace DeployFast.Agent
{
    public class DeployTosSection : ConfigurationSection
    {
        [ConfigurationProperty("", IsRequired = true, IsDefaultCollection = true)]
        public DeployTosCollection Instances
        {
            get
            {
                return (DeployTosCollection)this[""];
            }
            set
            {
                this[""] = value;
            }
        }
    }

    public class DeployTosCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement() =>
            new DeployToElement();

        protected override object GetElementKey(ConfigurationElement element) =>
            ((DeployToElement)element).AppId;
    }

    public class DeployToElement : ConfigurationElement
    {
        [ConfigurationProperty("appId", IsKey = true, IsRequired = true)]
        public string AppId
        {
            get
            {
                return (string)base["appId"];
            }
            set
            {
                base["appId"] = value;
            }
        }

        [ConfigurationProperty("deployTo", IsRequired = true)]
        public string DeployTo
        {
            get
            {
                return (string)base["deployTo"];
            }
            set
            {
                base["deployTo"] = value;
            }
        }
    }
}