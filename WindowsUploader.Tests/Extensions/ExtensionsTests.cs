using ContourNextLink24Manager;
using FluentAssertions;
using NUnit.Framework;

namespace WindowsUploader.Tests.Extensions
{
    [TestFixture]
    public class ExtensionsTests
    {
        private MessageBuffer buffer;

        [SetUp]
        public void Setup()
        {
             buffer = new MedtronicMessageBuffer(64);
        }

        [Test]
        public void ShouldPutAtIndex0()
        {
            // arrange
            byte b = 255;

            // act
            buffer.Put(b);
            var result = buffer.ToArray();
            
            // assert
            result[0].Should().Be(b);
            result[1].Should().Be(0);
            result[2].Should().Be(0);
            result[3].Should().Be(0);
        }

        public void ShouldPutAt0()
        {
            // arrange
            byte b = 255;

            // act
            buffer.Put(b);
            var result = buffer.ToArray();

            // assert
            result[0].Should().Be(b);
            result[1].Should().Be(0);
            result[2].Should().Be(0);
            result[3].Should().Be(0);
        }

        [Test]
        public void ShouldPutAllItemsOfRange()
        {
            // arrange
            var b = new byte[] { 64, 127, 255 };

            // act
            buffer.Put(b);
            var result = buffer.ToArray();

            // assert
            result[0].Should().Be(64);
            result[1].Should().Be(127);
            result[2].Should().Be(255);
            result[3].Should().Be(0);
        }

        [Test]
        public void ShouldPut2ItemsOfTheRange()
        {
            // arrange
            var b = new byte[] { 64, 127, 255 };

            // act
            buffer.Put(b, 1, 2);
            var result = buffer.ToArray();

            // assert
            result[0].Should().Be(127);
            result[1].Should().Be(255);
            result[2].Should().Be(0);
            result[3].Should().Be(0);
        }


        [Test]
        public void ShouldPutAllItemsAfterEachother()
        {
            // arrange
            byte a = 32;
            var b = new byte[] { 64, 127, 255 };

            // act
            buffer.Put(a);
            buffer.Put(b, 0, 1);
            buffer.Put(b, 2, 1);
            var result = buffer.ToArray();

            // assert
            result[0].Should().Be(32);
            result[1].Should().Be(64);
            result[2].Should().Be(255);
            result[3].Should().Be(0);
        }
    }
}
