using DG.Tweening;
using System.Collections;
using UnityEngine;

public class AsteroidController : MonoBehaviour
{
    private float zAxisRotationValue;
    private Sequence AsteroidSequence;
    private Vector3 StartingPosition;
    private WaitForSeconds Delay = new WaitForSeconds(1f);
    private Transform parent;
    private bool IsRespawning;

    public float RandomMinNumber, RandomMaxNumber;
    public float Duration;
    public GameController controller;
    public Rigidbody2D rigid;
    public float xAxisMovementValue, yAxisMovementValue;

    void Start() // Note to self. Physics make everything waaaay worse. Like 1000ms+ worse... FIND A WAY TO DEAL WITH THEM! Seriously. Without active colliders there was hope. After colliders were switched on... Only despair remained.
    {
        StartingPosition = transform.localPosition;

        parent = transform.parent;
        transform.parent = null;

        xAxisMovementValue = Random.Range(RandomMinNumber, RandomMaxNumber);
        yAxisMovementValue = Random.Range(RandomMinNumber, RandomMaxNumber);
        zAxisRotationValue = Random.Range(RandomMinNumber, RandomMaxNumber);

        //InitiateAsteroidMovement();
        //StartCoroutine(CheckDistance());
    }

    //private void InitiateAsteroidMovement()  //<---------------------------- Not a solution. Kinda works. Very laggy 40-65 ms per player loop (without collisions) 
    //{
    //    AsteroidSequence = DOTween.Sequence();

    //    AsteroidSequence.Append(transform.DOLocalMove(new Vector3(transform.localPosition.x + xAxisMovementValue, transform.localPosition.y + yAxisMovementValue, transform.localPosition.z), Duration).SetEase(Ease.Linear));

    //    AsteroidSequence.SetLoops(-1, LoopType.Incremental);
    //}

    //private void FixedUpdate() //<---------------------------- Not a solution. 500+ ms per player loop and almost killed my laptop(without collisions)
    //{
    //    rigid.MovePosition(rigid.position + new Vector2(transform.localPosition.x + xAxisMovementValue, transform.localPosition.y + yAxisMovementValue) * Time.fixedDeltaTime);
    //}

    //private void FixedUpdate() //<---------------------------- Not a solution. Whole player loop takes 700 - 900 ms (using rigidbody.MovePosition it takes longer than using DoTween Sequence).
    //{
    //    RaycastHit2D hit = Physics2D.Raycast(new Vector2(transform.localPosition.x,transform.localPosition.y), transform.TransformDirection(transform.localPosition), 5.0f);
    //    Debug.DrawRay(transform.localPosition, transform.TransformDirection(transform.localPosition), Color.green);

    //    if (hit.collider != null)
    //    {
    //        float distance = Mathf.Abs(hit.point.y - transform.position.y);

    //        if(distance < 1f)
    //        {
    //            if (!hit.transform.CompareTag("Player"))
    //            {
    //                if (hit.transform.CompareTag("Bullet"))
    //                {
    //                    controller.AddPoints();
    //                }
    //                //StartCoroutine(ResetAsteroid(gameObject.GetComponent<Collider2D>(), gameObject.GetComponent<SpriteRenderer>()));
    //            }
    //        }
    //    }
    //}

    private void OnDestroy()
    {
        if (AsteroidSequence != null)
        {
            AsteroidSequence.Kill();
        }
    }

    //private IEnumerator CheckDistance() //<---------------------------- Not a solution. Killed my laptop...
    //{
    //    for (int i = 0; i < controller.FieldSize; i++)
    //    {
    //        for (int j = 0; j < controller.FieldSize; j++)
    //        {
    //            if (controller.AllAsteroids[i,j] != transform && Vector3.Distance(transform.localPosition, controller.AllAsteroids[i, j].localPosition) < 5)
    //            {
    //                StartCoroutine(ResetAsteroid(gameObject.GetComponent<Collider2D>(), gameObject.GetComponent<SpriteRenderer>()));
    //            }
    //        }
    //    }
    //    yield return new WaitForSeconds(10);
    //    StartCoroutine(CheckDistance());
    //}

    private void Update() //<---------------------------- Not a solution. 800 - 1600 ms (11-37 ms without collisions) Most optimal ATM
    {
        if (!IsRespawning)
        {
            transform.Translate(new Vector3(xAxisMovementValue, yAxisMovementValue, 0) * Time.deltaTime / 100, Space.Self);
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
        transform.parent = parent;
        IsRespawning = true;

        if (AsteroidSequence != null)
        {
            AsteroidSequence.Kill();
        }

        transform.localPosition = StartingPosition;

        yield return Delay;

        transform.parent = null;
        collider.enabled = true;
        spriteRenderer.enabled = true;
        IsRespawning = false;
        //InitiateAsteroidMovement();
    }
}
