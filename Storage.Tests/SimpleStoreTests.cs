using System;
using System.Collections.Generic;
using System.Text;
using Storage.Core;
using Xunit;

namespace Storage.Tests
{
    public class SimpleStoreTests
    {
        [Fact]
        public void Set_ThenGet_ReturnsSavedValue()
        {
            //Arrange
            using SimpleStore store = new();

            UserProfile profile = new UserProfile()
            {
                Id = 1,
                UserName = "Alex",
                CreatedAt = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc)
            };

            //Act
            store.Set("user:1", profile);
            UserProfile? result = store.Get("user:1");

            //Assert
            Assert.NotNull(result);
            Assert.Equal(profile.Id, result.Id);
            Assert.Equal(profile.UserName, result.UserName);
            Assert.Equal(profile.CreatedAt, result.CreatedAt);

        }

        [Fact]
        public void Get_WhenKeyDoesNotExist_ReturnsNull()
        {
            // Arrange
            using SimpleStore store = new();

            // Act
            UserProfile? result = store.Get("unknown");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Set_WhenKeyAlreadyExists_UpdatesValue()
        {
            // Arrange
            using SimpleStore store = new();

            UserProfile oldProfile = new()
            {
                Id = 1,
                UserName = "Old",
                CreatedAt = DateTime.UtcNow
            };

            UserProfile newProfile = new()
            {
                Id = 2,
                UserName = "New",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            store.Set("user:1", oldProfile);
            store.Set("user:1", newProfile);

            UserProfile? result = store.Get("user:1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newProfile.Id, result.Id);
            Assert.Equal(newProfile.UserName, result.UserName);
            Assert.Equal(newProfile.CreatedAt, result.CreatedAt);
        }

        [Fact]
        public void Delete_WhenKeyExists_RemovesValue()
        {
            // Arrange
            using SimpleStore store = new();

            UserProfile profile = new()
            {
                Id = 1,
                UserName = "Alex",
                CreatedAt = DateTime.UtcNow
            };

            store.Set("user:1", profile);

            // Act
            store.Delete("user:1");

            // Assert
            Assert.Null(store.Get("user:1"));
        }

        [Fact]
        public void Delete_WhenKeyDoesNotExist_DoesNotThrow()
        {
            // Arrange
            using SimpleStore store = new();

            // Act
            Exception? exception = Record.Exception(() =>
            {
                store.Delete("unknown");
            });

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void GetStatistics_ReturnsCompletedOperationCounts()
        {
            // Arrange
            using SimpleStore store = new();

            UserProfile profile = new()
            {
                Id = 1,
                UserName = "Alex",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            store.Set("user:1", profile);
            store.Set("user:1", profile);

            store.Get("user:1");
            store.Get("unknown");

            store.Delete("user:1");
            store.Delete("unknown");

            var statistics = store.GetStatistics();

            // Assert
            Assert.Equal(2L, statistics.setCount);
            Assert.Equal(2L, statistics.getCount);
            Assert.Equal(2L, statistics.deleteCount);   

        }

    }
}
