using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Storage.Core;
using Xunit;

namespace Storage.Tests
{
    public class SimpleStoreConcurrencyTests
    {
        /*
        * Проверяет работу одного SimpleStore из нескольких задач:
        * 4 задачи записывают данные, 4 читают, 1 удаляет.
        *
        * После завершения проверяем статистику и содержимое хранилища.
        * Подготовительные Set тоже входят в счётчик.
        * Статистику проверяем до итоговых Get, поскольку они её изменяют.
        */

        [Fact]
        public async Task ParallelOperations_PreserveDataAndStatistics()
        {
            using SimpleStore store = new();

            const int readerCount = 4;
            const int writerCount = 4;
            const int operationsPerTask = 1_000;

            byte[] ReadableValue = [10, 20, 30];

            store.Set("readable", ReadableValue);

            for (int i = 0; i < operationsPerTask; i++ )
            {
                store.Set($"delete:{i}", [99]);
            }

            var tasks = new List<Task>();

            for (int writer = 0; writer < writerCount; writer++)
            {
                int writerId = writer;

                tasks.Add(Task.Run(() =>
                {
                    for (int i = 0; i < operationsPerTask; i++)
                    {
                        store.Set($"writer:{writerId}:{i}", BitConverter.GetBytes(i));
                    }
                }));
            }

            for (int reader = 0; reader < readerCount; reader++)
            {
                tasks.Add(Task.Run(() =>
                    {
                        for (int i = 0; i < operationsPerTask; i++)
                        {
                            Assert.Equal(ReadableValue, store.Get("readable"));
                        }

                    }));

            }

            tasks.Add(Task.Run(() =>
            {
                for (int i = 0; i < operationsPerTask; i++)
                {
                    store.Delete($"delete:{i}");
                }
            }));

            await Task.WhenAll(tasks);

            var statistics = store.GetStatistics();

            //  1 ключ "readable" + 1000 ключи для удаления + (4 × 1000) записи писателей
            long expectedSets = 1L + operationsPerTask + writerCount * operationsPerTask;

            //  4000 чтений
            long expectedGets = (long)readerCount * operationsPerTask;

            Assert.Equal(expectedSets, statistics.setCount);                        //  5001 запись  
            Assert.Equal(expectedGets, statistics.getCount);                        //  4000 чтений
            Assert.Equal((long)operationsPerTask, statistics.deleteCount);          //   1000 удалений

            for (int writer = 0; writer < writerCount; writer++)
            {
                for (int i = 0; i < operationsPerTask; i++)
                {
                    Assert.Equal(BitConverter.GetBytes(i), store.Get($"writer:{writer}:{i}"));
                }
            }

            for (int i = 0; i < operationsPerTask; i++)
            {
                Assert.Null(store.Get($"delete:{i}"));
            }

        }

    }
}
