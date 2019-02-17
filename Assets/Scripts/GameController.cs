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
    [HideInInspector]
    public SpatialHashingClass SpatialHashingInstance;

    [HideInInspector]
    public Dictionary<int, List<SimpleGameObject>> ThreadsDictionary;

    [HideInInspector]
    public readonly Queue<Action> ExecuteOnMainThread = new Queue<Action>();

    [HideInInspector]
    public List<Vector2> SpawnPointsOutsidePlayerFrustrum = new List<Vector2>();

    [HideInInspector]
    public bool SaveExists;

    [HideInInspector]
    public List<SimpleGameObject> AllObjects;

    public Text Text;
    public Camera Camera;
    public Transform Player;
    public GameObject GridContainer;
    public GameObject Asteroid;
    public int CellSize;
    public int Columns;
    public int FieldSize;
    public int Points;
    public int TasksMaxAmount;
    public float RandomMinNumber, RandomMaxNumber;
    public float RandomSpeedMinNumber, RandomSpeedMaxNumber;

    private bool Finished;
    private static int TotalPoints = 0;
    private AsteroidsData AsteroidsData;
    private Vector3 CameraRightUpperCornerPosition;
    private Vector3 UpperRightCornerInWorldSpace;
    private Vector3 LowerLeftCornerInWorldSpace;
    private WaitForSeconds Delay = new WaitForSeconds(1f);

    private void Awake()
    {
        Time.timeScale = 1;
    }

    private void Start()
    {
        AllObjects = new List<SimpleGameObject>();

        SpatialHashingInstance = new SpatialHashingClass(FieldSize, Columns);

        ThreadsDictionary = new Dictionary<int, List<SimpleGameObject>>();

        SetPlayerData();

        CalculateCameraFrustrumCorners();

        TryLoadingAsteroidsDataFromFile();

        for (int x = 0; x < FieldSize; x++)
        {
            for (int y = 0; y < FieldSize; y++)
            {
                var asteroidLocalPosition = new Vector2((x * 3), (y * 3));

                if(!(Vector2.Distance(asteroidLocalPosition, new Vector2(Player.localPosition.x, Player.localPosition.y)) > 1.0f)) {
                    continue;
                }
                if (!CheckIfPositionIsNotInFrustrum(asteroidLocalPosition))
                {
                    var objInst = Instantiate(Asteroid, new Vector3(asteroidLocalPosition.x, asteroidLocalPosition.y, 0), Quaternion.identity, GridContainer.transform);
                    var asteroidController = objInst.GetComponent<AsteroidController>();
                    asteroidController.Controller = this;

                    SetAsteroidDirectionAndSpeedIfSaveDoesNotExist(x, y);

                    asteroidController.AsteroidSimpleGameObject = new SimpleGameObject(objInst.transform.position, objInst.transform.position, new Vector2(AsteroidsData.AsteroidXDirection[x, y], AsteroidsData.AsteroidYDirection[x, y]), 0.9f, AsteroidsData.AsteroidSpeed[x, y], SimpleGameObjectTypeEnum.Asteroid, asteroidController);

                    AllObjects.Add(asteroidController.AsteroidSimpleGameObject);

                    SpatialHashingInstance.Insert(asteroidController.AsteroidSimpleGameObject, asteroidController.AsteroidSimpleGameObject);
                }
                else
                {
                    SpawnPointsOutsidePlayerFrustrum.Add(asteroidLocalPosition);

                    SetAsteroidDirectionAndSpeedIfSaveDoesNotExist(x, y);

                    var newSimpleObject = new SimpleGameObject(asteroidLocalPosition, asteroidLocalPosition, new Vector2(AsteroidsData.AsteroidXDirection[x, y], AsteroidsData.AsteroidYDirection[x, y]), 0.9f, AsteroidsData.AsteroidSpeed[x, y], SimpleGameObjectTypeEnum.Asteroid, null);

                    AllObjects.Add(newSimpleObject); //Commenting these two lines will make the app spawn only those asteroids that are visible in player frustrum (only then collision works as it should)
                    SpatialHashingInstance.Insert(newSimpleObject, newSimpleObject); //Commenting these two lines will make the app spawn only those asteroids that are visible in player frustrum (only then collision works as it should)
                }
            }
        }

        SetThreadsDictionary();

        Finished = true;

        Player.GetComponent<PlayerController>().IsAlive = true;

        SaveAsteroidsIfNoSaveFound();
    }

    private void SetThreadsDictionary()
    {
        for (int i = 0; i < AllObjects.Count; i++)
        {
            if (ThreadsDictionary.ContainsKey(i % TasksMaxAmount))
            {
                if (!ThreadsDictionary[i % TasksMaxAmount].Contains(AllObjects[i]) && !AllObjects[i].HasCollided)
                {
                    ThreadsDictionary[i % TasksMaxAmount].Add(AllObjects[i]);
                }
            }
            else
            {
                ThreadsDictionary.Add(i % TasksMaxAmount, new List<SimpleGameObject> { AllObjects[i] });
            }
        }
    }

    private void SetPlayerData()
    {
        Player.localPosition = new Vector3(160 * 3 / 2, 160 * 3 / 2, 0);

        var playerController = Player.GetComponent<PlayerController>();

        playerController.Controller = this;

        playerController.PlayerSimpleGameObject = new SimpleGameObject(Player.localPosition, Player.localPosition, Vector2.up, 0.9f, playerController.Speed, SimpleGameObjectTypeEnum.Player, playerController);
        AllObjects.Add(playerController.PlayerSimpleGameObject);
    }

    private void SetAsteroidDirectionAndSpeedIfSaveDoesNotExist(int x, int y)
    {
        if (!SaveExists)
        {
            AsteroidsData.AsteroidXDirection[x, y] = UnityEngine.Random.Range(RandomMinNumber, RandomMaxNumber);
            AsteroidsData.AsteroidYDirection[x, y] = UnityEngine.Random.Range(RandomMinNumber, RandomMaxNumber);
            AsteroidsData.AsteroidSpeed[x, y] = UnityEngine.Random.Range(RandomSpeedMinNumber, RandomSpeedMaxNumber);
        }
    }

    private void Update()
    {
        CalculateCameraFrustrumCorners();

        while(ExecuteOnMainThread != null && ExecuteOnMainThread.Count > 0)
        {
            try
            {
                ExecuteOnMainThread.Dequeue().Invoke();
            }
            catch (Exception e)
            {
                //Debug.LogError(e + " : " + e.StackTrace); <------ Eats time
            }
        }

        if (Finished)
        {
            for (int i = 0; i < AllObjects.Count; i++)
            {
                if (AllObjects[i].HasCollided)
                {
                    continue;
                }

                if (AllObjects[i].Type == SimpleGameObjectTypeEnum.Asteroid)
                {
                    AllObjects[i].OldPosition = AllObjects[i].NewPosition;
                    AllObjects[i].NewPosition = AllObjects[i].OldPosition + AllObjects[i].MovementDirection * Time.deltaTime * AllObjects[i].Speed;
                }

                if (!CheckIfPositionIsNotInFrustrum(AllObjects[i].NewPosition) && AllObjects[i].SimpleObjectController == null)
                {
                    var objInst = Instantiate(Asteroid, AllObjects[i].NewPosition, Quaternion.identity, GridContainer.transform);
                    var asteroidController = objInst.GetComponent<AsteroidController>();
                    asteroidController.Controller = this;
                    AllObjects[i].SimpleObjectController = asteroidController;
                }

                if (AllObjects[i].SimpleObjectController != null && AllObjects[i].SimpleObjectController.GetType() == 0)
                {
                    (AllObjects[i].SimpleObjectController as AsteroidController).Move(AllObjects[i].NewPosition - AllObjects[i].OldPosition);
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

                SpatialHashingInstance.UpdateCells(newPosList[i], newPosList[i]);

                var near = SpatialHashingInstance.GetNearbyObjectsPosition(newPosList[i]);
                var nearest = near.Where(e => Vector2.Distance(e.NewPosition, newPosList[i].NewPosition) <= 0.8f && newPosList[i] != e).FirstOrDefault();

                if (nearest != null)
                {
                    SpatialHashingInstance.Remove(newPosList[i]);

                    newPosList[i].HasCollided = true;
                    nearest.HasCollided = true;

                    DefineAndStartCollisionBehaviour(newPosList[i],nearest);
                    DefineAndStartCollisionBehaviour(nearest, newPosList[i]);

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

    private void DefineAndStartCollisionBehaviour(SimpleGameObject simpleObject, SimpleGameObject nearestSimpleObject)
    {
        switch (simpleObject.SimpleObjectController.GetType())
        {
            case 0 : //Asteroid
                if (nearestSimpleObject.Type == SimpleGameObjectTypeEnum.Asteroid || nearestSimpleObject.Type == SimpleGameObjectTypeEnum.Bullet)
                {
                    simpleObject.SimpleObjectController.Collided();
                }
                break;
            case 1 : //Player
                if (nearestSimpleObject.Type == SimpleGameObjectTypeEnum.Asteroid)
                {
                    simpleObject.SimpleObjectController.Collided();
                }
                break;
            case 2 : //Bullet
                if (nearestSimpleObject.Type == SimpleGameObjectTypeEnum.Asteroid)
                {
                    simpleObject.SimpleObjectController.Collided();
                }
                break;
            default:
                break;
        }
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
        yield return Delay;

        if (asteroid != null)
        {
            asteroid.OldPosition = SpawnPointsOutsidePlayerFrustrum[UnityEngine.Random.Range(0, SpawnPointsOutsidePlayerFrustrum.Count)];
            asteroid.NewPosition = asteroid.OldPosition;

            asteroid.HasCollided = false;
        }
    }

    private void CalculateCameraFrustrumCorners()
    {
        CameraRightUpperCornerPosition = new Vector3(Screen.width+50, Screen.height+150, 0);

        UpperRightCornerInWorldSpace = Camera.ScreenToWorldPoint(CameraRightUpperCornerPosition);
        LowerLeftCornerInWorldSpace = Camera.ScreenToWorldPoint(new Vector3(-50,-150,0));
    }

    public bool CheckIfPositionIsNotInFrustrum(Vector2 asteroidLocalPosition)
    {
        if(!(asteroidLocalPosition.x < UpperRightCornerInWorldSpace.x && asteroidLocalPosition.x > LowerLeftCornerInWorldSpace.x && asteroidLocalPosition.y < UpperRightCornerInWorldSpace.y && asteroidLocalPosition.y > LowerLeftCornerInWorldSpace.y))
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
                AsteroidsData = (AsteroidsData)binaryFormatter.Deserialize(file);
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
        AsteroidsData = new AsteroidsData();
        AsteroidsData.AsteroidXDirection = new float[FieldSize, FieldSize];
        AsteroidsData.AsteroidYDirection = new float[FieldSize, FieldSize];
        AsteroidsData.AsteroidSpeed = new float[FieldSize, FieldSize];
        SaveExists = false;
    }


    private void SaveAsteroidsIfNoSaveFound()
    {
        if (!SaveExists)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + "/AsteroidsData.dat", FileMode.OpenOrCreate);
            binaryFormatter.Serialize(file, AsteroidsData);
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