using Assets.Scripts;
using System;
using System.Collections.Generic;
using UnityEngine;

public class BulletController : MonoBehaviour,IController
{
    public float Speed;
    public SimpleGameObject BulletSimpleGameObject;
    public GameController Controller;
    private int DictionaryKey;

    void Start()
    {
        try
        {
            Destroy(gameObject,3.0f);
        }
        catch (Exception e)
        {
            //Debug.LogError("Object already destroyed");
        }
        gameObject.transform.SetParent(null);

        AddBulletToAllObjectListAndThreadsDictionary();
    }

    private void AddBulletToAllObjectListAndThreadsDictionary()
    {
        Controller.AllObjects.Add(BulletSimpleGameObject);
        DictionaryKey = Controller.AllObjects.IndexOf(BulletSimpleGameObject) % Controller.TasksMaxAmount;

        if (Controller.ThreadsDictionary.ContainsKey(DictionaryKey))
        {
            Controller.ThreadsDictionary[DictionaryKey].Add(BulletSimpleGameObject);
        }
        else
        {
            Controller.ThreadsDictionary.Add(DictionaryKey, new List<SimpleGameObject> { BulletSimpleGameObject });
        }
    }

    void Update()
    {
        BulletSimpleGameObject.OldPosition = transform.localPosition;
        transform.Translate(transform.up * Speed * Time.smoothDeltaTime, Space.World);
        BulletSimpleGameObject.NewPosition = transform.localPosition;
    }

    private void OnDestroy()
    {
        if (Controller.AllObjects.Contains(BulletSimpleGameObject))
        {
            Controller.AllObjects.Remove(BulletSimpleGameObject);
        }

        if (Controller.ThreadsDictionary[DictionaryKey].Contains(BulletSimpleGameObject))
        {
            Controller.ThreadsDictionary[DictionaryKey].Remove(BulletSimpleGameObject);
        }
    }

    public void Collided()
    {
        Controller.ExecuteOnMainThread.Enqueue(() =>
        {
            Controller.AddPoints();

            try
            {
                Destroy(gameObject);
            }
            catch (Exception e)
            {
                //Debug.LogError("Object already destroyed");
            }
        });
    }

    int IController.GetType()
    {
        return 2;
    }
}
