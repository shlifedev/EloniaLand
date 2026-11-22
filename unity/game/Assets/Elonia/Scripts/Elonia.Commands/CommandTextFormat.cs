using System;
using System.Collections.Generic;

namespace Elonia.Commands
{
    public sealed class CommandTextFormat<TContext>
    {
        private readonly List<CommandParser> _parsers = new List<CommandParser>();

        public CommandTextFormat<TContext> Add<TCommand>() where TCommand : struct, ICommand<TContext>, ICommandReader<TCommand>
        {
            _parsers.Add(new CommandParser<TCommand>());
            return this;
        }

        public CommandRecord<TContext> Parse(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            CommandRecord<TContext> record = new CommandRecord<TContext>();
            CommandTextReader reader = new CommandTextReader(text);
            reader.SkipWhitespace();
            while (!reader.IsEnd)
            {
                if (!TryReadCommand(reader, record))
                {
                    throw new FormatException($"Unknown command '{text[reader.Position]}' at position {reader.Position} in \"{text}\".");
                }

                reader.SkipWhitespace();
            }

            return record;
        }

        private bool TryReadCommand(CommandTextReader reader, CommandRecord<TContext> record)
        {
            int start = reader.Position;
            for (int index = 0; index < _parsers.Count; index++)
            {
                if (_parsers[index].TryRead(reader, record))
                {
                    return true;
                }

                reader.Position = start;
            }

            return false;
        }

        private abstract class CommandParser
        {
            public abstract bool TryRead(CommandTextReader reader, CommandRecord<TContext> record);
        }

        private sealed class CommandParser<TCommand> : CommandParser where TCommand : struct, ICommand<TContext>, ICommandReader<TCommand>
        {
            public override bool TryRead(CommandTextReader reader, CommandRecord<TContext> record)
            {
                if (!default(TCommand).TryRead(reader, out TCommand command))
                {
                    return false;
                }

                record.Add(command);
                return true;
            }
        }
    }
}
