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

namespace DeployFast.Shared.Generics
{
    [AttributeUsage(AttributeTargets.Property,
        Inherited = false, AllowMultiple = false)]
    public class OptionAttribute : Attribute
    {
        public OptionAttribute(string token, string valueName,
            int groupId, bool valueRequired, string helpText)
        {
            Token = token.ToUpper();
            ValueName = valueName?.ToLower();
            GroupId = groupId;
            HelpText = helpText;

            if (valueRequired)
            {
                if (string.IsNullOrWhiteSpace(valueName))
                    Kind = OptionKind.RequiredKeyOnly;
                else
                    Kind = OptionKind.RequiredKeyValue;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(valueName))
                    Kind = OptionKind.OptionalKeyOnly;
                else
                    Kind = OptionKind.OptionalKeyValue;
            }
        }

        public string Token { get; }
        public int GroupId { get; }
        public string ValueName { get; }
        public OptionKind Kind { get; }
        public string HelpText { get; }
    }
}
