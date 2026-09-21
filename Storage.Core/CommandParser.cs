using System;
using System.Collections.Generic;
using System.Text;

namespace Storage.Core
{
    public static class CommandParser
    {
        private const byte Space = (byte)' ';


        public static Command Parse(ReadOnlySpan<byte> input)
        {

            //Удалим пробелы в начале и в конце
            input = TrimSpaces(input);

            if (input.IsEmpty)
            {
                return default;
            }

            //ищеи пробелы после команды
            int commandEndIndex = input.IndexOf(Space);

            //пробела нет - згачит есть только команда, но отсутствует ключ
            if(commandEndIndex < 0)
            {
                return default;
            }

            ReadOnlySpan<byte> command = input.Slice(start: 0, length: commandEndIndex);

            //нашли команду
            input = input.Slice(commandEndIndex);

            //пропускаем проблелы между командой и ключом
            input = TrimStartSpaces(input);

            //если после команды ничего нет, то ключ отсутствует 
            if (input.IsEmpty)
            {
                return default;
            }

            //ищем пробел после ключа
            int keyEndIndex = input.IndexOf(Space);

            //пробела после ключа нет, значит содержится только команда и ключ
            if (keyEndIndex < 0)
            {
                return new Command(
                    command,
                    input,
                    ReadOnlySpan<byte>.Empty);
            }

            ReadOnlySpan<byte> key = input.Slice(start: 0, length: keyEndIndex);

            //отбрасываем ключ
            input = input.Slice(keyEndIndex);

            //пропускаем пробелы между ключом и значением 
            ReadOnlySpan<byte> value = TrimStartSpaces(input);

            return new Command(command, key, value);

        }



        private static ReadOnlySpan<byte> TrimSpaces(ReadOnlySpan<byte> input)
        {
            input = TrimStartSpaces(input);
            input = TrimEndSpaces(input);

            return input;
        }

        private static ReadOnlySpan<byte> TrimStartSpaces(ReadOnlySpan<byte> input)
        {
            int startIndex = 0;

            while (startIndex < input.Length && input[startIndex] == Space)
            {
                startIndex++;
            }

            return input.Slice(startIndex);
        }

        private static ReadOnlySpan<byte> TrimEndSpaces(ReadOnlySpan<byte> input)
        {
            int length = input.Length;

            while (length > 0 && input[length - 1] == Space)
            {
                length--;
            }

            return input.Slice(0, length);
        }

    }


}
