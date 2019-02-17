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

    public readonly Queue<Action> ExecuteOnMainThread = new Queue<Action>();

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

        int j = 0;

        for (int x = 0; x < FieldSize; x++)
        {
            for (int y = 0; y < FieldSize; y++)
            {
                var asteroidLocalPosition = new Vector2((x * 3), (y * 3));

                if (!CheckIfPositionIsNotInFrustrum(asteroidLocalPosition))
                {
                    var objInst = Instantiate(Asteroid, new Vector3(asteroidLocalPosition.x, asteroidLocalPosition.y, 0), Quaternion.identity, GridContainer.transform);
                    var asteroidController = objInst.GetComponent<AsteroidController>();
                    asteroidController.controller = this;

                    SetAsteroidDirectionAndSpeedIfSaveDoesNotExist(x, y);

                    asteroidController.VirtualGameObject = new SimpleGameObject(objInst.transform.position, objInst.transform.position, new Vector2(asteroidsData.AsteroidXDirection[x, y], asteroidsData.AsteroidYDirection[x, y]), 0.9f, asteroidsData.AsteroidSpeed[x, y], SimpleGameObjectType.Asteroid, asteroidController);

                    AllAsteroids.Add(asteroidController.VirtualGameObject);

                    spatialHashingInstance.Insert(asteroidController.VirtualGameObject, asteroidController.VirtualGameObject);
                }
                else
                {
                    SpawnPointsOutsidePlayerFrustrum.Add(asteroidLocalPosition);

                    SetAsteroidDirectionAndSpeedIfSaveDoesNotExist(x, y);

                    var newSimpleObject = new SimpleGameObject(asteroidLocalPosition, asteroidLocalPosition, new Vector2(asteroidsData.AsteroidXDirection[x, y], asteroidsData.AsteroidYDirection[x, y]), 0.9f, asteroidsData.AsteroidSpeed[x, y], SimpleGameObjectType.Asteroid, null);

                    AllAsteroids.Add(newSimpleObject);
                    spatialHashingInstance.Insert(newSimpleObject, newSimpleObject);
                }
            }
        }

        for(int i=0;i<AllAsteroids.Count;i++)
        {
            if (ThreadsDictionary.ContainsKey(i % 256))
            {
                if (!ThreadsDictionary[i % 256].Contains(AllAsteroids[i]) && !AllAsteroids[i].HasCollided)
                {
                    ThreadsDictionary[i % 256].Add(AllAsteroids[i]);
                }
            }
            else
            {
                ThreadsDictionary.Add(i % 256, new List<SimpleGameObject> { AllAsteroids[i] });
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
        CalculateCameraFrustrumCorners();

        while(ExecuteOnMainThread != null && ExecuteOnMainThread.Count > 0)
        {
            try
            {
                ExecuteOnMainThread.Dequeue().Invoke(); // Tends to randomly throw NullReferenceException. Why?
            }
            catch (Exception e)
            {
                Debug.LogError(e + " : " + e.StackTrace);
            }
        }

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

                if (!CheckIfPositionIsNotInFrustrum(AllAsteroids[i].NewPosition) && AllAsteroids[i].InstantiatedObjectController == null)
                {
                    var objInst = Instantiate(Asteroid, AllAsteroids[i].NewPosition, Quaternion.identity, GridContainer.transform);
                    var asteroidController = objInst.GetComponent<AsteroidController>();
                    asteroidController.controller = this;
                    AllAsteroids[i].InstantiatedObjectController = asteroidController;
                }

                if (AllAsteroids[i].InstantiatedObjectController != null)
                {
                    AllAsteroids[i].InstantiatedObjectController.Move(AllAsteroids[i].NewPosition - AllAsteroids[i].OldPosition);
                }
                
            }
            Task.Run(async()=> { await RunThreadsAsync(); });
        }
    }

    private async Task RunThreadsAsync()
    {
        for(int i=0;i<ThreadsDictionary.Keys.ToArray().Count();i++)
        {
            await CalculateCollision(ThreadsDictionary[i],i);
        }
    }

    private Task CalculateCollision(List<SimpleGameObject> newPosList,int key)
    {
        return Task.Factory.StartNew(()=> {
            for (int i = 0; i < newPosList.Count; i++)
            {
                if (newPosList[i].HasCollided)
                {
                    continue;
                }

                spatialHashingInstance.UpdateCells(newPosList[i], newPosList[i]);

                var near = spatialHashingInstance.GetNearbyObjectsPosition(newPosList[i]);
                var nearest = near.Where(e => Vector2.Distance(e.NewPosition, newPosList[i].NewPosition) <= 0.8f && newPosList[i] != e).FirstOrDefault();

                if (nearest != null)
                {
                    spatialHashingInstance.Remove(newPosList[i]);

                    newPosList[i].HasCollided = true;
                    nearest.HasCollided = true;

                    if (newPosList[i].InstantiatedObjectController != null)
                    {
                        newPosList[i].InstantiatedObjectController.Collided(); // Collision is rarely detected (although if small amount is created [f.e in camera frustrum] then all collisions work as they should) threading mistake/problem?
                    }

                    if (nearest.InstantiatedObjectController != null)
                    {
                        nearest.InstantiatedObjectController.Collided();
                    }

                    try
                    {
                        DealWithIt(newPosList[i], nearest);
                    }catch(Exception e)
                    {
                        Debug.LogError(e.Message+" "+e.StackTrace);
                    }
                }
            }
        });
    }

    private void DealWithIt(SimpleGameObject newPosList,SimpleGameObject nearest)
    {
            ExecuteOnMainThread.Enqueue(() =>
            {
                StartCoroutine(DealWithAsteroidThatCollided(newPosList));
                StartCoroutine(DealWithAsteroidThatCollided(nearest));
            });
        
    }

    private IEnumerator DealWithAsteroidThatCollided(SimpleGameObject asteroid)
    {
        yield return new WaitForSeconds(1.0f);

        if (asteroid != null)
        {
            asteroid.OldPosition = SpawnPointsOutsidePlayerFrustrum[UnityEngine.Random.Range(0, SpawnPointsOutsidePlayerFrustrum.Count)];
            asteroid.NewPosition = asteroid.OldPosition;

            asteroid.HasCollided = false;
        }
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
    public AsteroidController InstantiatedObjectController;
    public bool HasCollided;

    public SimpleGameObject(Vector2 oldPosition, Vector2 newPosition, Vector2 movementDirection, float radius, float speed, SimpleGameObjectType type, AsteroidController instantiatedObjectController)
    {
        OldPosition = oldPosition;
        NewPosition = newPosition;
        MovementDirection = movementDirection;
        Radius = radius;
        Speed = speed;
        Type = type;
        InstantiatedObjectController = instantiatedObjectController;
    }
}

public enum SimpleGameObjectType
{
    Asteroid,
    Bullet,
    Player
}