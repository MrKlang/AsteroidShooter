using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float Speed;
    public float RotationSpeed;
    public GameObject Bullet;
    public Camera PlayerCamera;
    public GameObject Grid;
    public GameObject GameOverCanvas;

    private bool IsAlive;
    private WaitForSeconds Delay = new WaitForSeconds(0.5f);

    void Start()
    {
        IsAlive = true;
        StartCoroutine(Fire());
    }

    void Update()
    {
        if (IsAlive)
        {
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            {
                transform.Translate(transform.up * Speed * Time.smoothDeltaTime, Space.World);
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                transform.Rotate(-transform.forward * RotationSpeed * Time.smoothDeltaTime, Space.World);
                PlayerCamera.transform.Rotate(transform.forward * RotationSpeed * Time.smoothDeltaTime, Space.World);
                Grid.transform.Rotate(transform.forward * RotationSpeed * Time.smoothDeltaTime, Space.World);
            }

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                transform.Rotate(transform.forward * RotationSpeed * Time.smoothDeltaTime, Space.World);
                PlayerCamera.transform.Rotate(-transform.forward * RotationSpeed * Time.smoothDeltaTime, Space.World);
                Grid.transform.Rotate(-transform.forward * RotationSpeed * Time.smoothDeltaTime, Space.World);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        IsAlive = false;
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;
        StopAllCoroutines();

        GameOverCanvas.SetActive(true);

        Time.timeScale = 0;
    }

    private IEnumerator Fire()
    {
        yield return Delay;
        Instantiate(Bullet,transform);
        StartCoroutine(Fire());
    }
}
