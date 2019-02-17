using Assets.Scripts;
using System;
using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour,IController
{
    public bool IsAlive;
    public float Speed;
    public float RotationSpeed;
    public GameObject Bullet;
    public Camera PlayerCamera;
    public GameObject Grid;
    public GameObject GameOverCanvas;
    public SimpleGameObject PlayerSimpleGameObject;
    public GameController Controller;

    private WaitForSeconds Delay = new WaitForSeconds(0.5f);
    
    void Start()
    {
        StartCoroutine(Fire());
    }

    void Update()
    {
        if (IsAlive)
        {
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            {
                PlayerSimpleGameObject.OldPosition = transform.localPosition;
                transform.Translate(transform.up * Speed * Time.smoothDeltaTime, Space.World);
                PlayerSimpleGameObject.NewPosition = transform.localPosition;
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                TurnRight();
            }

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                TurnLeft();
            }
        }
    }

    private void TurnRight()
    {
        transform.Rotate(-transform.forward * RotationSpeed * Time.smoothDeltaTime, Space.World);
        PlayerCamera.transform.Rotate(transform.forward * RotationSpeed * Time.smoothDeltaTime, Space.World);
        Grid.transform.Rotate(transform.forward * RotationSpeed * Time.smoothDeltaTime, Space.World);
    }

    private void TurnLeft()
    {
        transform.Rotate(transform.forward * RotationSpeed * Time.smoothDeltaTime, Space.World);
        PlayerCamera.transform.Rotate(-transform.forward * RotationSpeed * Time.smoothDeltaTime, Space.World);
        Grid.transform.Rotate(-transform.forward * RotationSpeed * Time.smoothDeltaTime, Space.World);
    }

    public void Collided()
    {
        Controller.ExecuteOnMainThread.Enqueue(()=> {
            IsAlive = false;
            GetComponent<SpriteRenderer>().enabled = false;
            StopAllCoroutines();

            GameOverCanvas.SetActive(true);

            Time.timeScale = 0;
        });
    }

    private IEnumerator Fire()
    {
        yield return Delay;
        var bullet = Instantiate(Bullet,transform);
        var bController = bullet.GetComponent<BulletController>();
        bController.Controller = Controller;
        bController.BulletSimpleGameObject = new SimpleGameObject(bullet.transform.localPosition, bullet.transform.localPosition,Vector2.up,0.1f,bController.Speed,SimpleGameObjectTypeEnum.Bullet,bController);
        StartCoroutine(Fire());
    }

    int IController.GetType()
    {
        return 1;
    }
}
