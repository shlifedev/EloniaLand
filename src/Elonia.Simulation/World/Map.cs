using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Elonia.Simulation
{
    public sealed class Map
    {
        private readonly TileType[] _tiles;
        private readonly List<Actor> _actors = new List<Actor>();
        private readonly List<Item> _items = new List<Item>();

        public int Width { get; }
        public int Height { get; }

        public IReadOnlyList<Actor> Actors { get; }

        public IReadOnlyList<Item> Items { get; }

        public Map(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "A map must have a positive size.");
            }

            Width = width;
            Height = height;
            _tiles = new TileType[width * height];
            for (int index = 0; index < _tiles.Length; index++)
            {
                _tiles[index] = TileType.GROUND;
            }

            Actors = new ReadOnlyCollection<Actor>(_actors);
            Items = new ReadOnlyCollection<Item>(_items);
        }

        public bool IsInside(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public TileType GetTile(int x, int y)
        {
            ThrowIfOutside(x, y);
            return _tiles[y * Width + x];
        }

        public void SetTile(int x, int y, TileType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            ThrowIfOutside(x, y);
            _tiles[y * Width + x] = type;
        }

        public bool IsWalkable(int x, int y)
        {
            return IsInside(x, y) && _tiles[y * Width + x].IsWalkable;
        }

        public bool CanEnter(int x, int y)
        {
            return IsWalkable(x, y) && GetLivingActorAtOrNull(x, y) == null;
        }

        public Actor? GetLivingActorAtOrNull(int x, int y)
        {
            for (int index = 0; index < _actors.Count; index++)
            {
                Actor actor = _actors[index];
                if (actor.IsAlive && actor.X == x && actor.Y == y)
                {
                    return actor;
                }
            }

            return null;
        }

        public Actor AddActor(Actor actor)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            if (!IsWalkable(actor.X, actor.Y))
            {
                throw new ArgumentException("An actor must be placed on a walkable tile inside the map.", nameof(actor));
            }

            if (_actors.Contains(actor))
            {
                throw new ArgumentException("This actor is already in the map.", nameof(actor));
            }

            if (GetLivingActorAtOrNull(actor.X, actor.Y) != null)
            {
                throw new ArgumentException("A tile can hold only one living actor.", nameof(actor));
            }

            _actors.Add(actor);
            return actor;
        }

        public bool Contains(Actor actor)
        {
            return _actors.Contains(actor);
        }

        public Item AddItem(Item item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (!IsWalkable(item.X, item.Y))
            {
                throw new ArgumentException("An item must be placed on a walkable tile inside the map.", nameof(item));
            }

            if (GetItemAtOrNull(item.X, item.Y) != null)
            {
                throw new ArgumentException("A tile can hold only one item.", nameof(item));
            }

            _items.Add(item);
            return item;
        }

        public Item? GetItemAtOrNull(int x, int y)
        {
            for (int index = 0; index < _items.Count; index++)
            {
                Item item = _items[index];
                if (item.X == x && item.Y == y)
                {
                    return item;
                }
            }

            return null;
        }

        internal void RemoveItem(Item item)
        {
            _items.Remove(item);
        }

        private void ThrowIfOutside(int x, int y)
        {
            if (!IsInside(x, y))
            {
                throw new ArgumentOutOfRangeException(nameof(x), $"({x}, {y}) is outside the map.");
            }
        }
    }
}
