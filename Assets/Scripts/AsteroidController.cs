//using DG.Tweening; //For Sequence Only
using System.Collections;
using UnityEngine;
using System.Linq;

public class AsteroidController : MonoBehaviour
{
    //private Sequence AsteroidSequence;
    private Vector3 StartingPosition;
    private WaitForSeconds Delay = new WaitForSeconds(1f);
    private Transform parent;
    private bool IsRespawning;

    //public float Duration; // Uncomment for sequence usage
    //public Rigidbody2D rigid; // Uncomment for physics usage in FixedUpdate

    [HideInInspector]
    public Vector2 AsteroidDirection;

    [HideInInspector]
    public float Speed;

    [HideInInspector]
    public GameController controller;

    public SpriteRenderer renderer;

    void Start()
    {
        StartingPosition = transform.localPosition;

        parent = transform.parent;
        transform.parent = null;

        controller.spatialHashingInstance.Insert(transform.position, transform.position);
        //InitiateAsteroidMovement(); // uncomment to use sequence
        //StartCoroutine(CheckDistance()); //U*ncomment to use mathematic way to check collision (not recommended)
    }

    //private void InitiateAsteroidMovement()  //<---------------------------- Not a solution. Kinda works. Very laggy 40-65 ms per player loop (without collisions) UNCOMMIT SEQUENCE TU USE (DON'T FORGET ABOUT OnDestroy METHOD)
    //{
    //    AsteroidSequence = DOTween.Sequence();

    //    AsteroidSequence.Append(transform.DOLocalMove(new Vector3(transform.localPosition.x + AsteroidDirection.x, transform.localPosition.y + AsteroidDirection.y, transform.localPosition.z), Duration).SetEase(Ease.Linear));

    //    AsteroidSequence.SetLoops(-1, LoopType.Incremental);
    //}

    //private void FixedUpdate() //<---------------------------- Not a solution. 500+ ms per player loop and almost killed my laptop(without collisions)
    //{
    //    rigid.MovePosition(rigid.position + new Vector2(transform.localPosition.x + AsteroidDirection.x, transform.localPosition.y + AsteroidDirection.y) * Time.fixedDeltaTime);
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

    //private void OnDestroy()
    //{
    //    if (AsteroidSequence != null)
    //    {
    //        AsteroidSequence.Kill();
    //    }
    //}

    //private IEnumerator CheckDistance() //<---------------------------- Not a solution. Killed my laptop...
    //{
    //    StartCoroutine(CheckDistance());
    //}

    private void Update() //<---------------------------- Not a solution. 800 - 1600 ms (11-37 ms without collisions) Most optimal ATM
    {
        if (!IsRespawning)
        {
            if (transform != null)
            {
                Vector2 currentPosition = new Vector2(transform.localPosition.x, transform.localPosition.y);
                controller.spatialHashingInstance.UpdateCells(currentPosition + AsteroidDirection * Time.deltaTime * Speed, currentPosition);
                transform.Translate(new Vector3(AsteroidDirection.x, AsteroidDirection.y, 0) * Time.deltaTime * Speed, Space.Self);
                currentPosition = new Vector2(transform.localPosition.x, transform.localPosition.y);

                var near = controller.spatialHashingInstance.GetNearbyObjects(currentPosition);
                var nearest = near.Where(e=>Vector2.Distance(e, currentPosition) <=1.0f && e != currentPosition).FirstOrDefault();

                if (nearest != Vector2.zero)
                {
                    controller.spatialHashingInstance.Remove(currentPosition);
                    //nearest.gameObject.GetComponent<AsteroidController>().Collided();
                    Collided();
                }
            }
        }
    }

    public void Collided()
    {
        IsRespawning = true;
        //Destroy(gameObject);
        StartCoroutine(ResetAsteroid(renderer));
    }

    private void OnBecameVisible()
    {
        renderer.enabled = true;
    }

    private void OnBecameInvisible()
    {
        renderer.enabled = false;
    }

    //private void OnCollisionEnter2D(Collision2D collision)
    //{
    //    if (!collision.transform.CompareTag("Player"))
    //    {
    //        if(collision.transform.CompareTag("Bullet"))
    //        {
    //            controller.AddPoints();
    //        }
    //        StartCoroutine(ResetAsteroid(gameObject.GetComponent<SpriteRenderer>()));
    //    }
    //}

    private IEnumerator ResetAsteroid(SpriteRenderer spriteRenderer)
    {
        spriteRenderer.enabled = false;
        transform.parent = parent;
        IsRespawning = true;

        //if (AsteroidSequence != null)
        //{
        //    AsteroidSequence.Kill();
        //}

        transform.localPosition = controller.SpawnPointsOutsidePlayerFrustrum[UnityEngine.Random.Range(0, controller.SpawnPointsOutsidePlayerFrustrum.Count)];

        yield return Delay;

        transform.parent = null;
        spriteRenderer.enabled = true;
        IsRespawning = false;
        //InitiateAsteroidMovement();
    }
}
