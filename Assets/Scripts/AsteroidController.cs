using DG.Tweening;
using System.Collections;
using UnityEngine;
using Unity.Entities;

public class AsteroidController : MonoBehaviour
{
    private float xAxisMovementValue,yAxisMovementValue;
    private float zAxisRotationValue;
    private Sequence AsteroidSequence;
    private Vector3 StartingPosition;
    private WaitForSeconds Delay = new WaitForSeconds(1f);

    public float RandomMinNumber, RandomMaxNumber;
    public float Duration;
    public GameController controller;
    public Collider2D collider;

    void Start()
    {
        StartingPosition = transform.localPosition;

        xAxisMovementValue = Random.Range(RandomMinNumber, RandomMaxNumber);
        yAxisMovementValue = Random.Range(RandomMinNumber, RandomMaxNumber);
        zAxisRotationValue = Random.Range(RandomMinNumber, RandomMaxNumber);

        InitiateAsteroidMovement();
    }

    private void InitiateAsteroidMovement()
    {
        AsteroidSequence = DOTween.Sequence();

        AsteroidSequence.Append(transform.DOLocalMove(new Vector3(transform.localPosition.x + xAxisMovementValue, transform.localPosition.y + yAxisMovementValue, transform.localPosition.z), Duration).SetEase(Ease.Linear));

        AsteroidSequence.SetLoops(-1, LoopType.Incremental);
    }

    private void OnDestroy()
    {
        if (AsteroidSequence != null)
        {
            AsteroidSequence.Kill();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.transform.CompareTag("Player"))
        {
            if(collision.transform.CompareTag("Bullet"))
            {
                controller.AddPoints();
            }
            StartCoroutine(ResetAsteroid(gameObject.GetComponent<Collider2D>(), gameObject.GetComponent<SpriteRenderer>()));
        }
    }

    private IEnumerator ResetAsteroid(Collider2D collider, SpriteRenderer spriteRenderer)
    {
        collider.enabled = false;
        spriteRenderer.enabled = false;

        if (AsteroidSequence != null)
        {
            AsteroidSequence.Kill();
        }

        transform.localPosition = StartingPosition;

        yield return Delay;

        collider.enabled = true;
        spriteRenderer.enabled = true;
        InitiateAsteroidMovement();
    }
}
