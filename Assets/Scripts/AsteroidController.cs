using System.Collections;
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

    void Start()
    {
        parent = transform.parent;
        transform.parent = null;
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
            StartCoroutine(ResetAsteroid(renderer));
        }
    }

    private void OnBecameVisible()
    {
        renderer.enabled = true;
    }

    private void OnBecameInvisible()
    {
        renderer.enabled = false;
    }

    private IEnumerator ResetAsteroid(SpriteRenderer spriteRenderer)
    {
        spriteRenderer.enabled = false;
        transform.parent = parent;
        IsRespawning = true;

        transform.localPosition = controller.SpawnPointsOutsidePlayerFrustrum[UnityEngine.Random.Range(0, controller.SpawnPointsOutsidePlayerFrustrum.Count)];
        VirtualGameObject.OldPosition = transform.localPosition;
        VirtualGameObject.NewPosition = transform.localPosition;

        yield return Delay;

        transform.parent = null;
        spriteRenderer.enabled = true;
        IsRespawning = false;

        VirtualGameObject.HasCollided = false;
    }
}
