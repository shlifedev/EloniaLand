using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Elonia.Simulation
{
    public sealed class Map
    {
        private readonly Cell[] _cells;
        private readonly List<Actor> _actors = new List<Actor>();
        private readonly List<Thing> _things = new List<Thing>();

        private int _nextEntityID = 1;

        public int Width { get; }
        public int Height { get; }

        public IReadOnlyList<Actor> Actors { get; }

        public IReadOnlyList<Thing> Things { get; }

        public Map(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "A map must have a positive size.");
            }

            Width = width;
            Height = height;
            _cells = new Cell[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    _cells[y * width + x] = new Cell(x, y, FloorType.GROUND);
                }
            }

            Actors = new ReadOnlyCollection<Actor>(_actors);
            Things = new ReadOnlyCollection<Thing>(_things);
        }

        public bool IsInside(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public Cell GetCell(int x, int y)
        {
            ThrowIfOutside(x, y);
            return _cells[y * Width + x];
        }

        public FloorType GetFloor(int x, int y)
        {
            return GetCell(x, y).Floor;
        }

        public void SetFloor(int x, int y, FloorType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            GetCell(x, y).Floor = type;
        }

        public PropType? GetPropOrNull(int x, int y)
        {
            return GetCell(x, y).PropOrNull;
        }

        public void SetProp(int x, int y, PropType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            GetCell(x, y).PropOrNull = type;
        }

        internal void RemoveProp(int x, int y)
        {
            GetCell(x, y).PropOrNull = null;
        }

        public bool IsWalkable(int x, int y)
        {
            return IsInside(x, y) && _cells[y * Width + x].IsWalkable;
        }

        public bool CanEnter(int x, int y)
        {
            return IsInside(x, y) && _cells[y * Width + x].CanEnter;
        }

        public Actor? GetLivingActorAtOrNull(int x, int y)
        {
            return GetCell(x, y).LivingActorOrNull;
        }

        public int CreateEntityID()
        {
            int id = _nextEntityID;
            _nextEntityID++;
            return id;
        }

        public T AddEntity<T>(T entity) where T : Entity
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (!IsWalkable(entity.X, entity.Y))
            {
                throw new ArgumentException("An entity must be placed on a walkable tile inside the map.", nameof(entity));
            }

            if (FindEntityOrNull(entity.ID) != null)
            {
                throw new ArgumentException($"Entity ID {entity.ID} is already used in this map.", nameof(entity));
            }

            switch (entity)
            {
                case Actor actor:
                    if (GetLivingActorAtOrNull(actor.X, actor.Y) != null)
                    {
                        throw new ArgumentException("A tile can hold only one living actor.", nameof(entity));
                    }

                    _actors.Add(actor);
                    GetCell(actor.X, actor.Y).EnterActor(actor);
                    break;
                case Thing thing:
                    if (thing.Type.GetPartOrNull<ItemPart>() != null && GetItemAtOrNull(thing.X, thing.Y) != null)
                    {
                        throw new ArgumentException("A tile can hold only one item.", nameof(entity));
                    }

                    _things.Add(thing);
                    GetCell(thing.X, thing.Y).AddThing(thing);
                    break;
                default:
                    throw new ArgumentException($"Unknown entity type {entity.GetType().Name}.", nameof(entity));
            }

            _nextEntityID = Math.Max(_nextEntityID, entity.ID + 1);
            return entity;
        }

        public bool Contains(Entity entity)
        {
            return FindEntityOrNull(entity.ID) == entity;
        }

        public Thing? GetItemAtOrNull(int x, int y)
        {
            return GetCell(x, y).GetItemOrNull();
        }

        internal void MoveActor(Actor actor, int x, int y)
        {
            Debug.Assert(Contains(actor));
            Debug.Assert(CanEnter(x, y));
            GetCell(actor.X, actor.Y).LeaveActor(actor);
            actor.MoveTo(x, y);
            GetCell(x, y).EnterActor(actor);
        }

        internal void RemoveThing(Thing thing)
        {
            if (_things.Remove(thing))
            {
                GetCell(thing.X, thing.Y).RemoveThing(thing);
            }
        }

        private Entity? FindEntityOrNull(int id)
        {
            for (int index = 0; index < _actors.Count; index++)
            {
                if (_actors[index].ID == id)
                {
                    return _actors[index];
                }
            }

            for (int index = 0; index < _things.Count; index++)
            {
                if (_things[index].ID == id)
                {
                    return _things[index];
                }
            }

            return null;
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
