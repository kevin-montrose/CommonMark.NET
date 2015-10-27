using CommonMark.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace CommonMark.Tests
{
    [TestClass]
    public class TableTests
    {
        static CommonMarkSettings Settings;

        static TableTests()
        {
            Settings = CommonMarkSettings.Default.Clone();
            Settings.AdditionalFeatures = CommonMarkAdditionalFeatures.GithubStyleTables;
            Settings.TrackSourcePosition = true;
        }

        [TestMethod]
        public void SimpleTable()
        {
            var ast = 
                CommonMarkConverter.Parse(
@"First Header  | Second Header
------------- | -------------
Content Cell  | Content Cell
Content Cell  | Content Cell",
                    Settings
                );

            var firstChild = ast.FirstChild;
            Assert.AreEqual(BlockTag.Table, firstChild.Tag);
        }
    }
}
