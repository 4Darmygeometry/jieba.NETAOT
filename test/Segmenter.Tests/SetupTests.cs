using System;
using System.IO;
using NUnit.Framework;

namespace JiebaNet.Segmenter.Tests
{
    [SetUpFixture]
    public class SetUpClass
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            var dir = AppContext.BaseDirectory;
            Directory.SetCurrentDirectory(dir);
        }

        [OneTimeTearDown]
        public void RunAfterAnyTests()
        {
            Console.WriteLine("Job Done");
        }
    }
}
