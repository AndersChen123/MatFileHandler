using NUnit.Framework;
using System.IO;

namespace MatFileHandler.Tests
{
    [TestFixture]
    public class MatFileReaderHdfTests
    {
        private const string TestDirectory = "test-data";

        /// <summary>
        /// Test reading an ASCII-encoded string.
        /// </summary>
        [Test]
        public void TestAscii()
        {
            var matFile = ReadHdfTestFile("ascii");
            var arrayAscii = matFile["s"].Value as ICharArray;
            Assert.That(arrayAscii, Is.Not.Null);
            Assert.That(arrayAscii.Dimensions, Is.EqualTo(new[] { 1, 3 }));
            Assert.That(arrayAscii.String, Is.EqualTo("abc"));
            Assert.That(arrayAscii[2], Is.EqualTo('c'));
        }

        /// <summary>
        /// Test reading a Unicode string.
        /// </summary>
        [Test]
        public void TestUnicode()
        {
            var matFile = ReadHdfTestFile("unicode");
            var arrayUnicode = matFile["s"].Value as ICharArray;
            Assert.That(arrayUnicode, Is.Not.Null);
            Assert.That(arrayUnicode.Dimensions, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(arrayUnicode.String, Is.EqualTo("必フ"));
            Assert.That(arrayUnicode[0], Is.EqualTo('必'));
            Assert.That(arrayUnicode[1], Is.EqualTo('フ'));
        }

        /// <summary>
        /// Test reading a wide Unicode string.
        /// </summary>
        [Test]
        public void TestUnicodeWide()
        {
            var matFile = ReadHdfTestFile("unicode-wide");
            var arrayUnicodeWide = matFile["s"].Value as ICharArray;
            Assert.That(arrayUnicodeWide, Is.Not.Null);
            Assert.That(arrayUnicodeWide.Dimensions, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(arrayUnicodeWide.String, Is.EqualTo("🍆"));
        }

        private static AbstractTestDataFactory<IMatFile> GetTests(string factoryName) =>
            new MatTestDataFactory(Path.Combine(TestDirectory, factoryName));

        private IMatFile ReadHdfTestFile(string testName)
        {
            return GetTests("hdf")[testName];
        }
    }
}
