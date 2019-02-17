using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts
{
    public class SpatialHashingClass
    {
        private Dictionary<int, List<SimpleGameObject>> CellsDictionary;
        private Dictionary<SimpleGameObject, List<int>> ObjectsDictionary;
        private int CellSize;
        private int BoundSize;

        public SpatialHashingClass(int fieldSize,int columns)
        {
            BoundSize = fieldSize;
            CellSize = BoundSize / columns;
            CellsDictionary = new Dictionary<int, List<SimpleGameObject>>();
            ObjectsDictionary = new Dictionary<SimpleGameObject, List<int>>();
        }

        public void Insert(SimpleGameObject vector, SimpleGameObject obj)
        {
            var key = Key(vector.NewPosition);
            if (CellsDictionary.ContainsKey(key) )
            {
                if (!CellsDictionary[key].Contains(obj))
                {
                    CellsDictionary[key].Add(obj);
                }
            }
            else
            {
                CellsDictionary.Add(key, new List<SimpleGameObject> { obj });
            }

            if (ObjectsDictionary.ContainsKey(obj))
            {
                if (!ObjectsDictionary[obj].Contains(key))
                {
                    ObjectsDictionary[obj].Add(key);
                }
            }
            else
            {
                ObjectsDictionary.Add(obj, new List<int> { key });
            }
        }

        public void UpdateCells(SimpleGameObject vector, SimpleGameObject obj)
        {
            if (ObjectsDictionary.ContainsKey(obj))
            {
                for (int i = 0; i < ObjectsDictionary[obj].Count; i++) {
                    if (CellsDictionary.ContainsKey(ObjectsDictionary[obj][i]))
                    {
                        var std = CellsDictionary[ObjectsDictionary[obj][i]].Where(e=>e.NewPosition == vector.OldPosition).FirstOrDefault();
                        if (std != null)
                        {
                            CellsDictionary[ObjectsDictionary[obj][i]].Remove(std);
                        }
                    }
                }
            }
            Insert(vector, vector);
        }

        public void Remove(SimpleGameObject vec)
        {
            var key = Key(vec.OldPosition);
            if (ObjectsDictionary.ContainsKey(vec))
            {
                for (int i = 0; i < ObjectsDictionary[vec].Count; i++)
                {
                    if (CellsDictionary.ContainsKey(ObjectsDictionary[vec][i]))
                    {
                        CellsDictionary[ObjectsDictionary[vec][i]].Remove(vec);
                    }
                }
            }

            if (CellsDictionary.ContainsKey(key))
            {
                CellsDictionary.Remove(key);
            }
        }

        public void Reset()
        {
            CellsDictionary.Clear();
            ObjectsDictionary.Clear();
        }

        private int Key(Vector2 v)
        {
            return (int)((Math.Floor(v.x / CellSize)) +
                    (Math.Floor(v.y / CellSize)) * (BoundSize/CellSize));
        }

        public List<SimpleGameObject> GetNearbyObjectsPosition(SimpleGameObject vector)
        {
            var key = Key(vector.NewPosition);
            return CellsDictionary.ContainsKey(key) ? CellsDictionary[key] : new List<SimpleGameObject>();
        }
    }
}
