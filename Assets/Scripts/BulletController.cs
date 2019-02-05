using UnityEngine;

public class BulletController : MonoBehaviour
{
    public float Speed;

    void Start()
    {
        Destroy(gameObject, 3.0f);
        gameObject.transform.SetParent(null);
    }

    void Update()
    {
        transform.Translate(transform.up * Speed * Time.smoothDeltaTime, Space.World);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Destroy(gameObject);
    }
}
