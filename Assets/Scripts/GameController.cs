using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;

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

    [HideInInspector]
    public List<Vector2> SpawnPointsOutsidePlayerFrustrum = new List<Vector2>();

    [HideInInspector]
    public bool SaveExists;

    public List<Transform> AllInstantiatedAsteroids;
    public List<Vector2> AllNotInstantiatedAsteroidsPositions;
    public List<Vector2> AllNotInstantiatedAsteroidsVectors;
    public List<float> AllNotInstantiatedAsteroidsSpeed;


    private static int TotalPoints = 0;
    private AsteroidsData asteroidsData;
    private Vector3 cameraRightUpperCornerPosition;
    private Vector3 upperRightCornerInWorldSpace;
    private Vector3 lowerLeftCornerInWorldSpace;
    private Vector3 localRightUpperCornerPosition;
    private Vector3 localLeftLowerCornerPosition;
    private bool finished;

    private void Awake()
    {
        Time.timeScale = 1;
    }

    void Start()
    {
        AllInstantiatedAsteroids = new List<Transform>();

        CalculateCameraFrustrumCorners();

        TryLoadingAsteroidsDataFromFile();

        for (int x = 0; x < FieldSize; x++)
        {
            for (int y = 0; y < FieldSize; y++)
            {
                var asteroidLocalPosition = new Vector2Int((x * 3) - 80 * 3, (y * 3) - 80 * 3);

                if(!CheckIfPositionIsNotInFrustrum(asteroidLocalPosition))
                {
                    var objInst = Instantiate(Asteroid, new Vector3(asteroidLocalPosition.x,asteroidLocalPosition.y,0), Quaternion.identity, GridContainer.transform);
                    var asteroidController = objInst.GetComponent<AsteroidController>();
                    asteroidController.controller = this;

                    AllInstantiatedAsteroids.Add(objInst.transform);

                    if (SaveExists)
                    {
                        asteroidController.AsteroidDirection = new Vector2(asteroidsData.AsteroidXDirection[x, y], asteroidsData.AsteroidYDirection[x, y]);
                        asteroidController.Speed = asteroidsData.AsteroidSpeed[x, y];
                    }
                    else
                    {
                        asteroidsData.AsteroidXDirection[x, y] = UnityEngine.Random.Range(RandomMinNumber, RandomMaxNumber);
                        asteroidsData.AsteroidYDirection[x, y] = UnityEngine.Random.Range(RandomMinNumber, RandomMaxNumber);
                        asteroidsData.AsteroidSpeed[x, y] = UnityEngine.Random.Range(RandomSpeedMinNumber, RandomSpeedMaxNumber);

                        asteroidController.AsteroidDirection = new Vector2(asteroidsData.AsteroidXDirection[x, y], asteroidsData.AsteroidYDirection[x, y]);
                        asteroidController.Speed = asteroidsData.AsteroidSpeed[x, y];
                    }
                }
                else
                {
                    SpawnPointsOutsidePlayerFrustrum.Add(asteroidLocalPosition);
                    AllNotInstantiatedAsteroidsPositions.Add(asteroidLocalPosition);
                    if (SaveExists)
                    {
                        AllNotInstantiatedAsteroidsSpeed.Add(asteroidsData.AsteroidSpeed[x, y]);
                        AllNotInstantiatedAsteroidsVectors.Add(new Vector2(asteroidsData.AsteroidXDirection[x, y], asteroidsData.AsteroidYDirection[x, y]));
                    }
                    else
                    {
                        asteroidsData.AsteroidXDirection[x, y] = UnityEngine.Random.Range(RandomMinNumber, RandomMaxNumber);
                        asteroidsData.AsteroidYDirection[x, y] = UnityEngine.Random.Range(RandomMinNumber, RandomMaxNumber);
                        asteroidsData.AsteroidSpeed[x, y] = UnityEngine.Random.Range(RandomSpeedMinNumber, RandomSpeedMaxNumber);

                        AllNotInstantiatedAsteroidsSpeed.Add(asteroidsData.AsteroidSpeed[x, y]);
                        AllNotInstantiatedAsteroidsVectors.Add(new Vector2(asteroidsData.AsteroidXDirection[x, y], asteroidsData.AsteroidYDirection[x, y]));
                    }
                }
            }
        }

        finished = true;

        SaveAsteroidsIfNoSaveFound();

        System.GC.Collect();

        Player.localPosition = new Vector3(160 * 3/2 , 160 * 3/2 , 0);
    }

    private void Update()
    {
        if (finished)
        {
            for(int i = 0; i < AllNotInstantiatedAsteroidsPositions.Count; i++)
            {
                AllNotInstantiatedAsteroidsPositions[i] += AllNotInstantiatedAsteroidsVectors[i] * Time.deltaTime * AllNotInstantiatedAsteroidsSpeed[i];
                if (i < 100)
                {
                    var nearest = AllNotInstantiatedAsteroidsPositions.Where(e => e != null && Vector2.Distance(AllNotInstantiatedAsteroidsPositions[i], e) <= 0.8f && e != AllNotInstantiatedAsteroidsPositions[i]).FirstOrDefault();

                    if (nearest != null)
                    {
                        AllNotInstantiatedAsteroidsPositions[i] = SpawnPointsOutsidePlayerFrustrum[UnityEngine.Random.Range(0,SpawnPointsOutsidePlayerFrustrum.Count)];
                    }
                }
            }

            System.GC.Collect();
        }
    }

    private void CalculateCameraFrustrumCorners()
    {
        cameraRightUpperCornerPosition = new Vector3(Screen.width, Screen.height, 0);

        upperRightCornerInWorldSpace = camera.ScreenToWorldPoint(cameraRightUpperCornerPosition);
        lowerLeftCornerInWorldSpace = camera.ScreenToWorldPoint(Vector3.zero);

        localRightUpperCornerPosition = camera.transform.InverseTransformPoint(upperRightCornerInWorldSpace);
        localLeftLowerCornerPosition = camera.transform.InverseTransformPoint(lowerLeftCornerInWorldSpace);
    }

    public bool CheckIfPositionIsNotInFrustrum(Vector2Int asteroidLocalPosition)
    {
        if(!(asteroidLocalPosition.x < localRightUpperCornerPosition.x && asteroidLocalPosition.x > localLeftLowerCornerPosition.x && asteroidLocalPosition.y < localRightUpperCornerPosition.y && asteroidLocalPosition.y > localLeftLowerCornerPosition.y))
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