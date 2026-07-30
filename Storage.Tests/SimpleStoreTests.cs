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
            SimpleStore store = new SimpleStore();
            byte[] value = [1, 2, 3];

            //Act
            store.Set("user:1", value);
            byte[]? result = store.Get("user:1");

            //Assert
            Assert.NotNull(result);
            Assert.Equal(value, result);

        }

        [Fact]
        public void Get_WhenKeyDoesNotExist_ReturnsNull()
        {
            // Arrange
            SimpleStore store = new SimpleStore();

            // Act
            byte[]? result = store.Get("unknown");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Set_WhenKeyAlreadyExists_UpdatesValue()
        {
            // Arrange
            SimpleStore store = new SimpleStore();

            byte[] oldValue = [1, 2, 3];
            byte[] newValue = [4, 5, 6];

            // Act
            store.Set("user:1", oldValue);
            store.Set("user:1", newValue);

            byte[]? result = store.Get("user:1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newValue, result);
        }

        [Fact]
        public void Delete_WhenKeyExists_RemovesValue()
        {
            // Arrange
            SimpleStore store = new SimpleStore();
            byte[] value = [1, 2, 3];

            store.Set("user:1", value);

            // Act
            store.Delete("user:1");
            byte[]? result = store.Get("user:1");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Delete_WhenKeyDoesNotExist_DoesNotThrow()
        {
            // Arrange
            SimpleStore store = new();

            // Act
            Exception? exception = Record.Exception(() =>
            {
                store.Delete("unknown");
            });

            // Assert
            Assert.Null(exception);
        }


    }
}
