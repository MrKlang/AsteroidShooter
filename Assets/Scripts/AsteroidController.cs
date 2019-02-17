using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidController : MonoBehaviour,IController
{
    [HideInInspector]
    public GameController Controller;

    [HideInInspector]
    public SimpleGameObject AsteroidSimpleGameObject;

    private Transform Parent;

    void Start()
    {
        Parent = transform.parent;
        transform.parent = null;
    }

    public void Move(Vector2 newPosition)
    {
        try
        {
            if (transform != null)
            {
                transform.Translate(new Vector3(newPosition.x, newPosition.y, 0), Space.Self);
            }
        }
        catch (Exception e)
        {
            //Debug.LogError("Object already destroyed"); <------ Eats time
        }
    }

    public void Collided()
    {
        if (AsteroidSimpleGameObject.HasCollided)
        {
            try
            {
                Controller.ExecuteOnMainThread.Enqueue(() => {
                    AsteroidSimpleGameObject.SimpleObjectController = null;
                    try
                    {
                        Destroy(gameObject);
                    }catch(Exception e)
                    {
                        //Debug.LogError("Object already destroyed"); <------ Eats time
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogError("Exception :" + e+ " happended: "+e.StackTrace);
            }
        }
    }

    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }

    int IController.GetType()
    {
        return 0;
    }
}
