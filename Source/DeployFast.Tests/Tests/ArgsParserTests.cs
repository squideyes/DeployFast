#region Copyright, Author Details and Related Context  
//<notice lastUpdateOn="4/18/2016">  
//  <solution>DeployFast</solution> 
//  <assembly>DeployFast.Tests</assembly>  
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

using DeployFast.App;
using DeployFast.Shared.Generics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using PR = DeployFast.Tests.Properties.Resources;

namespace DeployFast.Tests
{
    [TestClass]
    public class ArgsParserTests
    {
        [TestMethod]
        public void GoodDeployArgsTest() => ParseArgs(PR.GoodDeployArgs);

        [TestMethod]
        public void GoodConnArgsTest() => ParseArgs(PR.GoodConnArgs);

        [TestMethod]
        public void GoodDeleteConnArgsTest() => ParseArgs(@"/DELETECONN");

        [TestMethod]
        public void ArgValuesCanBeProceededBySpacesTest()
        {
            var options = ParseArgs(PR.GoodArgsWithSpaces);

            Assert.IsTrue(options.SourcePath.IsTrimmed());
            Assert.IsTrue(options.AppId.IsDefined());
            Assert.IsTrue(options.BuildName.IsTrimmed());
            Assert.IsTrue(options.HostNames != null &&
                options.HostNames.Count == 2 &&
                options.HostNames.All(hostName => hostName.IsTrimmed()));
            Assert.IsTrue(options.AlertTos != null &&
                options.AlertTos.Count == 2 &&
                options.AlertTos.All(alertTo => alertTo.IsEmail()));
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void BadAppIdDetectedTest()
        {
            var options = ParseArgs(PR.BadAppIdArgs);

            Assert.IsTrue(options.AppId.IsDefined());
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void MissingSourceArgDetected() => ParseArgs(PR.MissingSourceArg);

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void MissingAppIdArgDetected() => ParseArgs(PR.MissingAppIdArg);

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void MissingBuildArgDetected() => ParseArgs(PR.MissingBuildArg);

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void MissingHostsArgDetected() => ParseArgs(PR.MissingHostsArgs);

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void MissingAlertArgDetected() => ParseArgs(PR.MissingAlertArg);

        private Options ParseArgs(string cmd)
        {
            var parser = new ArgsParser<Options>();

            var options = parser.Parse(cmd.Split(' '));

            if ((options == null) || !options.GetIsValid())
                throw new Exception();

            return options;
        }
    }
}
