﻿#region Copyright, Author Details and Related Context  
//<notice lastUpdateOn="4/18/2016">  
//  <solution>DeployFast</solution> 
//  <assembly>DeployFast.Tests.Properties</assembly>  
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

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DeployFast.Tests.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("DeployFast.Tests.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to /SOURCE:C:\Some Folder /APPID:XXX /BUILD:App_SomeCo.SomeApp_20160304.1 /HOSTS:ABC123,XYZ987 /ALERT:somedude@someco.com;otherdude@someco.com.
        /// </summary>
        public static string BadAppIdArgs {
            get {
                return ResourceManager.GetString("BadAppIdArgs", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to /SOURCE: C:\Some Folder /APPID: JOBS /BUILD: App_SomeCo.SomeApp_20160304.1 /HOSTS: ABC123 , XYZ987 /ALERT: somedude@someco.com ; otherdude@someco.com.
        /// </summary>
        public static string GoodArgsWithSpaces {
            get {
                return ResourceManager.GetString("GoodArgsWithSpaces", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to /CONN:DefaultEndpointsProtocol=https;AccountName=someco;AccountKey=O21totdNsyxIgHMPIq0jVyBhjxkikfeVkOPfCzzdExvc9Yl4VxTXuC0VBfu275et1QnY/tzBqArmTwpoQyPn0w==;.
        /// </summary>
        public static string GoodConnArgs {
            get {
                return ResourceManager.GetString("GoodConnArgs", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to /SOURCE:C:\Some Folder /APPID:JOBS /BUILD:App_SomeCo.SomeApp_20160304.1 /HOSTS:ABC123,XYZ987 /ALERT:somedude@someco.com;otherdude@someco.com.
        /// </summary>
        public static string GoodDeployArgs {
            get {
                return ResourceManager.GetString("GoodDeployArgs", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to /SOURCE:C:\Some Folder /APPID:JOBS /BUILD:App_SomeCo.SomeApp_20160304.1 /HOSTS:ABC123,XYZ987.
        /// </summary>
        public static string MissingAlertArg {
            get {
                return ResourceManager.GetString("MissingAlertArg", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to /SOURCE:C:\Some Folder /BUILD:App_SomeCo.SomeApp_20160304.1 /HOSTS:ABC123,XYZ987 /ALERT:somedude@someco.com;otherdude@someco.com.
        /// </summary>
        public static string MissingAppIdArg {
            get {
                return ResourceManager.GetString("MissingAppIdArg", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to /SOURCE:C:\Some Folder /APPID:JOBS /HOSTS:ABC123,XYZ987 /ALERT:somedude@someco.com;otherdude@someco.com.
        /// </summary>
        public static string MissingBuildArg {
            get {
                return ResourceManager.GetString("MissingBuildArg", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to /SOURCE:C:\Some Folder /APPID:JOBS /BUILD:App_SomeCo.SomeApp_20160304.1  /ALERT:somedude@someco.com;otherdude@someco.com.
        /// </summary>
        public static string MissingHostsArgs {
            get {
                return ResourceManager.GetString("MissingHostsArgs", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to /APPID:JOBS /BUILD:App_SomeCo.SomeApp_20160304.1 /HOSTS:ABC123,XYZ987 /ALERT:somedude@someco.com;otherdude@someco.com.
        /// </summary>
        public static string MissingSourceArg {
            get {
                return ResourceManager.GetString("MissingSourceArg", resourceCulture);
            }
        }
    }
}