using System;
using System.Collections.Generic;
using System.Text;

namespace Storage.Core
{
    public readonly ref struct Command
    {
        public ReadOnlySpan<byte> CommandName { get; }
        public ReadOnlySpan<byte> Key { get; }
        public ReadOnlySpan<byte> Value { get; }

        public Command(
            ReadOnlySpan<byte> commandName,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> value)
        {
            CommandName = commandName;
            Key = key;
            Value = value;

        }
    }
}
