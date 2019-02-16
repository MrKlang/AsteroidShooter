using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using Assets.Scripts;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

public class GameController : MonoBehaviour
{
    public Text Text;
    public GameObject GridContainer;
    public GameObject Asteroid;
    public int FieldSize;
    public Transform Player;
    public float RandomMinNumber, RandomMaxNumber;
    public float RandomSpeedMinNumber, RandomSpeedMaxNumber;
    public Camera camera;
    
    public int CellSize;
    public int Columns;

    [HideInInspector]
    public List<Vector2> SpawnPointsOutsidePlayerFrustrum = new List<Vector2>();

    [HideInInspector]
    public bool SaveExists;

    [HideInInspector]
    public List<SimpleGameObject> AllAsteroids;

    private static int TotalPoints = 0;
    private AsteroidsData asteroidsData;
    private Vector3 cameraRightUpperCornerPosition;
    private Vector3 upperRightCornerInWorldSpace;
    private Vector3 lowerLeftCornerInWorldSpace;
    private bool finished;

    private Dictionary<int, List<SimpleGameObject>> ThreadsDictionary;

    public SpatialHashingClass spatialHashingInstance;

    private void Awake()
    {
        Time.timeScale = 1;
    }

    void Start()
    {
        Player.localPosition = new Vector3(160 * 3 / 2, 160 * 3 / 2, 0);

        spatialHashingInstance = new SpatialHashingClass(FieldSize, Columns);

        AllAsteroids = new List<SimpleGameObject>();

        ThreadsDictionary = new Dictionary<int, List<SimpleGameObject>>();

        CalculateCameraFrustrumCorners();

        TryLoadingAsteroidsDataFromFile();

        for (int x = 0; x < FieldSize; x++)
        {
            for (int y = 0; y < FieldSize; y++)
            {
                var asteroidLocalPosition = new Vector2((x * 3) , (y * 3));

                if (!CheckIfPositionIsNotInFrustrum(asteroidLocalPosition))
                {
                    var objInst = Instantiate(Asteroid, new Vector3(asteroidLocalPosition.x,asteroidLocalPosition.y,0), Quaternion.identity, GridContainer.transform);
                    var asteroidController = objInst.GetComponent<AsteroidController>();
                    asteroidController.controller = this;

                    SetAsteroidDirectionAndSpeedIfSaveDoesNotExist(x, y);

                    asteroidController.VirtualGameObject = new SimpleGameObject(objInst.transform.position, objInst.transform.position, new Vector2(asteroidsData.AsteroidXDirection[x, y], asteroidsData.AsteroidYDirection[x, y]), 0.9f, asteroidsData.AsteroidSpeed[x, y], SimpleGameObjectType.Asteroid, objInst);

                    asteroidController.VirtualGameObject.OldPosition = objInst.transform.TransformPoint(objInst.transform.position);
                    asteroidController.VirtualGameObject.NewPosition = objInst.transform.position;

                    AllAsteroids.Add(asteroidController.VirtualGameObject);
                }
                else
                {
                    SpawnPointsOutsidePlayerFrustrum.Add(asteroidLocalPosition);
                    spatialHashingInstance.Insert(asteroidLocalPosition, asteroidLocalPosition);

                    SetAsteroidDirectionAndSpeedIfSaveDoesNotExist(x, y);

                    //AllAsteroids.Add(new SimpleGameObject(asteroidLocalPosition, asteroidLocalPosition, new Vector2(asteroidsData.AsteroidXDirection[x, y], asteroidsData.AsteroidYDirection[x, y]),0.9f,asteroidsData.AsteroidSpeed[x,y],SimpleGameObjectType.Asteroid,null));
                }
            }
        }

        finished = true;

        SaveAsteroidsIfNoSaveFound();
    }

    private void SetAsteroidDirectionAndSpeedIfSaveDoesNotExist(int x, int y)
    {
        if (!SaveExists)
        {
            asteroidsData.AsteroidXDirection[x, y] = UnityEngine.Random.Range(RandomMinNumber, RandomMaxNumber);
            asteroidsData.AsteroidYDirection[x, y] = UnityEngine.Random.Range(RandomMinNumber, RandomMaxNumber);
            asteroidsData.AsteroidSpeed[x, y] = UnityEngine.Random.Range(RandomSpeedMinNumber, RandomSpeedMaxNumber);
        }
    }

    private void Update()
    {
        if (finished)
        {
            for (int i = 0; i < AllAsteroids.Count; i++)
            {
                if (AllAsteroids[i].HasCollided)
                {
                    continue;
                }

                AllAsteroids[i].OldPosition = AllAsteroids[i].NewPosition;
                AllAsteroids[i].NewPosition = AllAsteroids[i].OldPosition + AllAsteroids[i].MovementDirection * Time.deltaTime * AllAsteroids[i].Speed;

                if (AllAsteroids[i].InstantiatedObject != null)
                {
                    AllAsteroids[i].InstantiatedObject.GetComponent<AsteroidController>().Move(AllAsteroids[i].NewPosition - AllAsteroids[i].OldPosition);
                }

                if (ThreadsDictionary.ContainsKey(0))
                {
                    if (ThreadsDictionary[0].Contains(AllAsteroids[i]) && !AllAsteroids[i].HasCollided)
                    {
                        ThreadsDictionary[0].Add(AllAsteroids[i]);
                    }
                }
                else
                {
                    ThreadsDictionary.Add(0, new List<SimpleGameObject> { AllAsteroids[i] });
                }
            }

            RunThreadsAsync();
        }
    }

    private async Task RunThreadsAsync()
    {
        for(int i=0;i<ThreadsDictionary.Keys.ToArray().Count();i++)
        {
            await CalculateCollision(ThreadsDictionary[i],i);
        }
        ThreadsDictionary.Clear();
    }

    private Task CalculateCollision(List<SimpleGameObject> newPosList,int key)
    {
        return Task.Factory.StartNew(()=> {
            for (int i = 0; i < newPosList.Count; i++)
            {
                spatialHashingInstance.UpdateCells(newPosList[i].OldPosition, newPosList[i].NewPosition);

                var near = spatialHashingInstance.GetNearbyObjectsPosition(newPosList[i].NewPosition);
                var nearest = near.Where(e => Vector2.Distance(e, newPosList[i].NewPosition) <= 0.5f && e != newPosList[i].NewPosition).FirstOrDefault();

                if (nearest != Vector2.zero)
                {
                    spatialHashingInstance.Remove(newPosList[i].NewPosition); //Colliding with old self?

                    newPosList[i].InstantiatedObject.GetComponent<AsteroidController>().Collided(); //Cannot access AsteroidController?

                    newPosList[i].HasCollided = true;
                }

                if (ThreadsDictionary[key].Contains(newPosList[i]))
                {
                    ThreadsDictionary[key].Remove(newPosList[i]);
                }
            }
        });
    }

    private void CalculateCameraFrustrumCorners()
    {
        cameraRightUpperCornerPosition = new Vector3(Screen.width+50, Screen.height+150, 0);

        upperRightCornerInWorldSpace = camera.ScreenToWorldPoint(cameraRightUpperCornerPosition);
        lowerLeftCornerInWorldSpace = camera.ScreenToWorldPoint(new Vector3(-50,-150,0));
    }

    public bool CheckIfPositionIsNotInFrustrum(Vector2 asteroidLocalPosition)
    {
        if(!(asteroidLocalPosition.x < upperRightCornerInWorldSpace.x && asteroidLocalPosition.x > lowerLeftCornerInWorldSpace.x && asteroidLocalPosition.y < upperRightCornerInWorldSpace.y && asteroidLocalPosition.y > lowerLeftCornerInWorldSpace.y))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void TryLoadingAsteroidsDataFromFile()
    {
        if (File.Exists(Application.persistentDataPath + "/AsteroidsData.dat")) {
            if (new FileInfo(Application.persistentDataPath + "/AsteroidsData.dat").Length == 0)
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                FileStream file = File.Open(Application.persistentDataPath + "/AsteroidsData.dat", FileMode.Open);
                asteroidsData = (AsteroidsData)binaryFormatter.Deserialize(file);
                file.Close();
                SaveExists = true;
            }
            else
            {
                SetEmptyAsteroidsData();
            }
        }
        else {
            SetEmptyAsteroidsData();
        }
    }

    private void SetEmptyAsteroidsData()
    {
        asteroidsData = new AsteroidsData();
        asteroidsData.AsteroidXDirection = new float[FieldSize, FieldSize];
        asteroidsData.AsteroidYDirection = new float[FieldSize, FieldSize];
        asteroidsData.AsteroidSpeed = new float[FieldSize, FieldSize];
        SaveExists = false;
    }


    private void SaveAsteroidsIfNoSaveFound()
    {
        if (!SaveExists)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + "/AsteroidsData.dat", FileMode.OpenOrCreate);
            binaryFormatter.Serialize(file, asteroidsData);
            file.Close();
        }
    }

    public void AddPoints()
    {
        TotalPoints += 1;
        Text.text = string.Format("Score: {0}", TotalPoints);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}

[Serializable]
public class AsteroidsData
{
    public float[,] AsteroidXDirection;
    public float[,] AsteroidYDirection;
    public float[,] AsteroidSpeed;
}

public class SimpleGameObject
{
    public Vector2 OldPosition;
    public Vector2 NewPosition;
    public Vector2 MovementDirection;
    public float Radius;
    public float Speed;
    public SimpleGameObjectType Type;
    public GameObject InstantiatedObject;
    public bool HasCollided;

    public SimpleGameObject(Vector2 oldPosition, Vector2 newPosition, Vector2 movementDirection, float radius, float speed, SimpleGameObjectType type, GameObject instantiatedObject)
    {
        OldPosition = oldPosition;
        NewPosition = newPosition;
        MovementDirection = movementDirection;
        Radius = radius;
        Speed = speed;
        Type = type;
        InstantiatedObject = instantiatedObject;
    }
}

public enum SimpleGameObjectType
{
    Asteroid,
    Bullet,
    Player
}