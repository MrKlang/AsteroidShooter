using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidController : MonoBehaviour
{
    private WaitForSeconds Delay = new WaitForSeconds(1f);
    private Transform parent;
    private bool IsRespawning;

    [HideInInspector]
    public GameController controller;

    [HideInInspector]
    public SimpleGameObject VirtualGameObject;

    public SpriteRenderer renderer;

    public readonly Queue<Action> ExecuteOnMainThread = new Queue<Action>();

    void Start()
    {
        parent = transform.parent;
        transform.parent = null;
    }

    private void Update()
    {
        if (ExecuteOnMainThread.Count > 0)
        {
            ExecuteOnMainThread.Dequeue().Invoke();
        }
    }

    public void Move(Vector2 newPosition)
    {
        if (transform != null && !IsRespawning)
        {
            transform.Translate(new Vector3(newPosition.x, newPosition.y, 0), Space.Self);
        }
    }

    public void Collided()
    {
        if (VirtualGameObject.HasCollided && !IsRespawning)
        {
            try
            {
                ExecuteOnMainThread.Enqueue(() => {
                    //StartCoroutine(ResetAsteroid(renderer)); <---------- Object pooling... Kinda wrong to use it here
                    Destroy(gameObject);
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

    //private IEnumerator ResetAsteroid(SpriteRenderer spriteRenderer)
    //{
    //    spriteRenderer.enabled = false;
    //    transform.parent = parent;
    //    IsRespawning = true;

    //    transform.localPosition = controller.SpawnPointsOutsidePlayerFrustrum[UnityEngine.Random.Range(0, controller.SpawnPointsOutsidePlayerFrustrum.Count)];
    //    VirtualGameObject.OldPosition = transform.localPosition;
    //    VirtualGameObject.NewPosition = transform.localPosition;

    //    yield return Delay;

    //    transform.parent = null;
    //    spriteRenderer.enabled = true;
    //    IsRespawning = false;

    //    VirtualGameObject.HasCollided = false;
    //}
}
