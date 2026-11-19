using System;

namespace Elonia.Simulation
{
    public sealed class ThingType
    {
        private readonly IThingPart[] _parts;

        public string Id { get; }
        public string Name { get; }

        public ThingType(string name, params IThingPart[] parts)
            : this(name, name, parts)
        {
        }

        public ThingType(string id, string name, IThingPart[] parts)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("종류 ID가 필요합니다.", nameof(id));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("A thing type needs a name.", nameof(name));
            }

            if (parts == null)
            {
                throw new ArgumentNullException(nameof(parts));
            }

            for (int index = 0; index < parts.Length; index++)
            {
                if (parts[index] == null)
                {
                    throw new ArgumentException($"Thing type {name} has a null part.", nameof(parts));
                }

                for (int previous = 0; previous < index; previous++)
                {
                    if (parts[previous].GetType() == parts[index].GetType())
                    {
                        throw new ArgumentException(
                            $"Thing type {name} has two {parts[index].GetType().Name} parts.", nameof(parts));
                    }
                }
            }

            Id = id;
            Name = name;
            _parts = (IThingPart[])parts.Clone();
        }

        public T? GetPartOrNull<T>() where T : class, IThingPart
        {
            for (int index = 0; index < _parts.Length; index++)
            {
                if (_parts[index] is T part)
                {
                    return part;
                }
            }

            return null;
        }
    }
}
