using Storage.Core;
using System.Text;
using Xunit;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Storage.Tests
{
    public class CommandParserTests
    {

        [Fact]
        public void Parse_SetCommandWithThreeArguments_ReturnsCommandKeyAndValue()
        {
            // Arrange
            byte[] input = Encoding.UTF8.GetBytes("SET user:1 data");

            // Act
            ParsedCommand result = CommandParser.Parse(input);

            // Assert
            Assert.Equal("SET", ToText(result.Command));
            Assert.Equal("user:1", ToText(result.Key));
            Assert.Equal("data", ToText(result.Value));
        }

        [Fact]
        public void Parse_GetCommandWithTwoArguments_ReturnsCommandAndKeyWithEmptyValue()
        {
            // Arrange
            byte[] input = Encoding.UTF8.GetBytes("GET user:1");

            // Act
            ParsedCommand result = CommandParser.Parse(input);

            // Assert
            Assert.Equal("GET", ToText(result.Command));
            Assert.Equal("user:1", ToText(result.Key));
            Assert.True(result.Value.IsEmpty);
        }

        [Fact]
        public void Parse_InvalidCommandWithoutKey_ReturnsDefault()
        {
            // Arrange
            byte[] input = Encoding.UTF8.GetBytes("GET");

            // Act
            ParsedCommand result = CommandParser.Parse(input);

            // Assert
            Assert.True(result.Command.IsEmpty);
            Assert.True(result.Key.IsEmpty);
            Assert.True(result.Value.IsEmpty);
        }

        [Fact]
        public void Parse_CommandWithExtraSpaces_ReturnsCorrectParts()
        {
            // Arrange
            byte[] input = Encoding.UTF8.GetBytes("SET    user:1     data");

            // Act
            ParsedCommand result = CommandParser.Parse(input);

            // Assert
            Assert.Equal("SET", ToText(result.Command));
            Assert.Equal("user:1", ToText(result.Key));
            Assert.Equal("data", ToText(result.Value));
        }

        private static string ToText(ReadOnlySpan<byte> value)
        {
            return Encoding.UTF8.GetString(value);
        }


    }
}
