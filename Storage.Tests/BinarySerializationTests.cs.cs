using System;
using System.IO;
using Storage.Core;
using Xunit;

namespace Storage.Tests
{
    public class BinarySerializationTests
    {
        [Theory]
        [InlineData("Алексей")]
        [InlineData("")]
        [InlineData(null)]
        public void SerializeThenDeserialize_PreservesProfile(string? userName)
        {
            var original = new UserProfile
            {
                Id = 42,
                UserName = userName!,
                CreatedAt = new DateTime(2026, 9, 26, 12, 30, 0, DateTimeKind.Utc)
            };

            using var stream = new MemoryStream();

            original.SerializeToBinary(stream);

            //  после записи позиция находится в конце, возвращаем её в начало для чтения
            stream.Position = 0;

            UserProfile restored = UserProfile.DeserializeFromBinary(stream);

            Assert.Equal(original.Id, restored.Id);
            Assert.Equal(original.UserName, restored.UserName);
            Assert.Equal(original.CreatedAt.Ticks, restored.CreatedAt.Ticks);
            Assert.Equal(original.CreatedAt.Kind, restored.CreatedAt.Kind);

            Assert.True(stream.CanRead);
            Assert.Equal(stream.Length, stream.Position);
        }
    }
}