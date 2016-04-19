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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace DeployFast.Shared.Generics
{
    public static class StringExtenders
    {
        private static bool isInvalidEmail = false;

        public static bool IsEmail(this string value)
        {
            isInvalidEmail = false;

            if (string.IsNullOrEmpty(value))
                return false;

            try
            {
                value = Regex.Replace(value, @"(@)(.+)$",
                    DomainMapper, RegexOptions.None,
                    TimeSpan.FromMilliseconds(200));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }

            if (isInvalidEmail)
                return false;

            try
            {
                return Regex.IsMatch(value,
                      @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                      @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                      RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        private static string DomainMapper(Match match)
        {
            var idn = new IdnMapping();

            string domainName = match.Groups[2].Value;

            try
            {
                domainName = idn.GetAscii(domainName);
            }
            catch (ArgumentException)
            {
                isInvalidEmail = true;
            }

            return match.Groups[1].Value + domainName;
        }

        public static string WithSlash(this string value)
        {
            if (!value.EndsWith(Path.DirectorySeparatorChar.ToString()))
                value += Path.DirectorySeparatorChar;

            return value;
        }

        public static string ToSingleLine(this string value, string separator = "; ")
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            return string.Join(separator, value.ToLines());
        }

        private static List<string> ToLines(this string value)
        {
            var reader = new StringReader(value);

            var lines = new List<string>();

            string line;

            while ((line = reader.ReadLine()) != null)
                lines.Add(line.Trim());

            return lines;
        }

        public static List<string> Wrap(this string text, int margin)
        {
            int start = 0;

            int end;

            var lines = new List<string>();

            text = text.Trim();

            while ((end = start + margin) < text.Length)
            {
                while ((text[end]) != ' ' && (end > start))
                    end -= 1;

                if (end == start)
                    end = start + margin;

                lines.Add(text.Substring(start, end - start));

                start = end + 1;
            }

            if (start < text.Length)
                lines.Add(text.Substring(start));

            return lines;
        }
    }
}
