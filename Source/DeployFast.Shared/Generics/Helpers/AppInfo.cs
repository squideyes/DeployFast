#region Copyright, Author Details and Related Context  
//<notice lastUpdateOn="4/18/2016">  
//  <solution>DeployFast</solution> 
//  <assembly>DeployFast.Shared.Generics</assembly>  
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
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Text;

namespace DeployFast.Shared.Generics
{
    public class AppInfo
    { 
        public AppInfo(Assembly assembly)
        {
            Contract.Requires(assembly != null, nameof(assembly));

            Product = GetProduct(assembly);
            Version = assembly.GetName().Version;
            Copyright = GetCopyright(assembly).Replace("©", "(c)");
        }

        public string Product { get; private set; }
        public Version Version { get; private set; }
        public string Copyright { get; private set; }

        public string Title
        {
            get
            {
                var sb = new StringBuilder();

                sb.Append(Product);

                sb.Append(" v");
                sb.Append(Version.Major);
                sb.Append('.');
                sb.Append(Version.Minor);

                if ((Version.Build != 0) || (Version.Revision != 0))
                {
                    sb.Append('.');
                    sb.Append(Version.Build);
                }

                if (Version.Revision != 0)
                {
                    sb.Append('.');
                    sb.Append(Version.Revision);
                }

                return sb.ToString();
            }
        }

        private static string GetCopyright(Assembly assembly)
        {
            return assembly.GetAttribute<AssemblyCopyrightAttribute>().Copyright;
        }

        private static string GetProduct(Assembly assembly)
        {
            return assembly.GetAttribute<AssemblyProductAttribute>().Product;
        }
    }
}
