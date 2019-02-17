﻿using System;
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

        //internal List<Vector2> GetNearbyObjects(Vector2 obj)
        //{
        //    List<Vector2> objects = new List<Vector2>();
        //    List<int> bucketIds = GetAllNeighbouring(obj);
        //    foreach (var item in bucketIds)
        //    {
        //        if (CellsDictionary.ContainsKey(item) && CellsDictionary[item].Count > 0)
        //        {
        //            for (int i = 0; i < CellsDictionary[item].Count; i++)
        //            {
        //                if (!objects.Contains(CellsDictionary[item][i]))
        //                {
        //                    objects.Add(CellsDictionary[item][i]);
        //                }
        //            }
        //        }
        //    }
        //    return objects;
        //}

        //private List<int> GetAllNeighbouring(Vector2 obj)
        //{
        //    List<int> neighbours = new List<int>();

        //    Vector2 min = new Vector2(
        //        obj.x - (10f),
        //        obj.y - (10f));
        //    Vector2 max = new Vector2(
        //        obj.x + (10f),
        //        obj.y + (10f));

        //    AddCell(min, neighbours);
        //    AddCell(new Vector2(max.x, min.y), neighbours);
        //    AddCell(max, neighbours);
        //    AddCell(new Vector2(min.x, max.y), neighbours);

        //    return neighbours;
        //}

        //private void AddCell(Vector2 vector, List<int> cellList)
        //{
        //    int cellPosition = Key(vector);

        //    if (!cellList.Contains(cellPosition))
        //        cellList.Add(cellPosition);

        //}
    }
}
