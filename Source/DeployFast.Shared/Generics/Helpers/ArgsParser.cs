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
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace DeployFast.Shared.Generics
{
    public class ArgsParser<O> where O : IOptions, new()
    {
        public class Spec
        {
            public PropertyInfo Property { get; set; }
            public Type Type { get; set; }
            public string HelpText { get; set; }
            public OptionKind Kind { get; set; }
        }

        private const int WIDTH = 78;

        public void ShowHelp()
        {
            var options = new List<OptionAttribute>();

            foreach (var property in typeof(O).GetProperties())
            {
                var option = property.GetCustomAttribute<OptionAttribute>();

                if (option == null)
                    continue;

                options.Add(option);
            }

            var appInfo = new AppInfo(typeof(O).Assembly);

            var sb = new StringBuilder();

            sb.AppendLine(new string('=', WIDTH));
            sb.AppendLine($"{appInfo.Title}. {appInfo.Copyright}");
            sb.AppendLine(new string('=', WIDTH));
            sb.AppendLine();

            var cmd = new StringBuilder();

            cmd.Append(appInfo.Product + " ");

            int? lastGroupId = null;

            foreach (var option in options)
            {
                if (option.GroupId != lastGroupId)
                {
                    if (lastGroupId.HasValue)
                        cmd.Remove(cmd.Length - 1, 1).Append("] ");

                    cmd.Append("[");
                }

                switch (option.Kind)
                {
                    case OptionKind.OptionalKeyOnly:
                        cmd.Append($"[/{option.Token}]");
                        break;
                    case OptionKind.OptionalKeyValue:
                        cmd.Append($"[/{option.Token}:]");
                        break;
                    case OptionKind.RequiredKeyOnly:
                        cmd.Append($"</{option.Token}>");
                        break;
                    case OptionKind.RequiredKeyValue:
                        cmd.Append($"</{option.Token}:>");
                        break;
                }

                cmd.Append(' ');

                lastGroupId = option.GroupId;
            }

            cmd.Append("]");

            var cmdString = cmd.ToString();

            var firstLine = cmdString.Wrap(WIDTH)[0];

            sb.AppendLine(firstLine);

            foreach (var line in cmdString
                .Substring(firstLine.Length).Trim().Wrap(WIDTH - 4))
            {
                sb.AppendLine("    " + line);
            }

            sb.AppendLine();

            foreach (var option in options)
            {
                if (option.GroupId != lastGroupId)
                {
                    sb.AppendLine(new string('-', WIDTH));
                    sb.AppendLine();
                }

                ShowOptionHelp(sb, option);

                lastGroupId = option.GroupId;
            }

            sb.AppendLine(new string('=', WIDTH));
            sb.AppendLine();

            Console.Write(sb.ToString());
        }

        private static void ShowOptionHelp(StringBuilder sb, OptionAttribute option)
        {
            var text = option.HelpText;

            var lines = text.Wrap(60);

            for (int i = 0; i < lines.Count; i++)
            {
                if (i == 0)
                {
                    var slug = $"/{option.Token}";

                    if (!string.IsNullOrWhiteSpace(option.ValueName))
                        slug += $":<{option.ValueName}>";

                    sb.Append(slug.PadRight(18));
                }
                else
                {
                    sb.Append(new string(' ', 18));
                }

                sb.AppendLine(lines[i]);
            }

            sb.AppendLine();
        }

        public O Parse(string[] args)
        {
            var cmd = " " + string.Join(" ", args) + " ";

            var specs = new Dictionary<string, Spec>();

            var options = new O();

            var properties = typeof(O).GetProperties(
                BindingFlags.Instance | BindingFlags.Public);

            foreach (var property in typeof(O).
                GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var attrib = property.GetCustomAttribute<OptionAttribute>();

                if (attrib == null)
                    continue;

                if (attrib.Token == null)
                    throw new ArgumentNullException(nameof(attrib.Token));

                var token = attrib.Token.ToUpper();

                var spec = new Spec()
                {
                    Property = property,
                    Type = property.PropertyType,
                    HelpText = attrib.HelpText,
                    Kind = attrib.Kind
                };

                specs.Add(token, spec);
            }

            var chunks = GetChunks(cmd);

            foreach (var chunk in chunks)
            {
                var tv = new TokenValue(chunk);

                Spec spec;

                if (!specs.TryGetValue(tv.Token, out spec))
                    continue;

                if (!tv.HasValue)
                {
                    SetValue(spec.Property, options, true);
                }
                else if (spec.Type.IsEnum)
                {
                    if (Enum.GetNames(spec.Type).Any(
                        e => e.ToUpper() == tv.Value.ToUpper()))
                    {
                        var value = Enum.Parse(spec.Type, tv.Value, true);

                        SetValue(spec.Property, options, value);
                    }
                }
                else
                {
                    if (spec.Type.GetInterface(typeof(IConvertible).FullName) != null)
                    {
                        var value = Convert.ChangeType(tv.Value, spec.Type);

                        SetValue(spec.Property, options, value);
                    }
                    else
                    {
                        if (spec.Type == typeof(List<string>))
                        {
                            var value = (string)Convert.ChangeType(tv.Value, typeof(string));

                            var items = value.Split(new char[] { ',', ';' })
                                .Select(item => item.Trim()).ToList();

                            SetValue(spec.Property, options, items);
                        }
                        else
                        {
                            return default(O);
                        }
                    }
                }
            }

            if (!options.GetIsValid())
                return default(O);

            return options;
        }

        private void SetValue(PropertyInfo info, object instance, object value)
        {
            var targetType = info.PropertyType.IsNullableType()
                 ? Nullable.GetUnderlyingType(info.PropertyType)
                 : info.PropertyType;

            var convertedValue = Convert.ChangeType(value, targetType);

            info.SetValue(instance, convertedValue, null);
        }

        private List<string> GetChunks(string cmd)
        {
            var regex = new Regex(
                @"(?<=\s*?/)[A-Za-z]*?[A-Za-z0-9]\s*?(:|\s)");

            cmd = " " + cmd + " ";

            var chunks = new List<string>();

            var matches = regex.Matches(cmd).Cast<Match>().ToList();

            for (int i = 0; i < matches.Count - 1; i++)
            {
                var chunk = cmd.Substring(matches[i].Index,
                    matches[i + 1].Index - matches[i].Index - 1).Trim();

                chunks.Add(chunk);
            }

            if (matches.Count >= 1)
            {
                chunks.Add(cmd.Substring(
                    matches[matches.Count - 1].Index).Trim());
            }

            return chunks;
        }
    }
}
