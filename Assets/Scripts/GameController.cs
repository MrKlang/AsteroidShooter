using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
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
    public bool SaveExists;

    private static int TotalPoints = 0;
    private AsteroidsData AsteroidsData;

    private void Awake()
    {
        Time.timeScale = 1;
        //AllAsteroids = new Transform[FieldSize,FieldSize];
    }

    void Start()
    {
        TryLoadingAsteroidsDataFromFile();

        for (int x = 0; x < FieldSize; x++)
        {
            for (int y = 0; y < FieldSize; y++)
            {
                var objInst = Instantiate(Asteroid, new Vector3Int((x * 3) - 80 * 3, (y * 3) - 80 * 3, 0), Quaternion.identity, GridContainer.transform);
                var asteroidController = objInst.GetComponent<AsteroidController>();
                asteroidController.controller = this;

                if (SaveExists)
                {
                    asteroidController.AsteroidDirection = AsteroidsData.AsteroidDirection[x,y];
                    asteroidController.Speed = AsteroidsData.AsteroidSpeed[x, y];
                }
                else
                {
                    AsteroidsData.AsteroidDirection[x, y] = new Vector2(UnityEngine.Random.Range(RandomMinNumber, RandomMaxNumber), UnityEngine.Random.Range(RandomMinNumber, RandomMaxNumber));
                    AsteroidsData.AsteroidSpeed[x, y] = UnityEngine.Random.Range(RandomSpeedMinNumber, RandomSpeedMaxNumber);

                    asteroidController.AsteroidDirection = AsteroidsData.AsteroidDirection[x, y];
                    asteroidController.Speed = AsteroidsData.AsteroidSpeed[x, y];
                }

                //AllAsteroids[x, y] = objInst.transform;
                //Debug.LogError(string.Format("LocalPosition: x: {0}, y: {1};/n CellPosition: x: {2}, y: {3}", objInst.transform.localPosition.x, objInst.transform.localPosition.y, TilesMap.GetCellCenterLocal(new Vector3Int(x, y, 0)).x, TilesMap.GetCellCenterLocal(new Vector3Int(x, y, 0)).y));
            }
        }

        SaveAsteroidsIfNoSaveFound();

        System.GC.Collect();

        Player.localPosition = new Vector3(160 * 3/2 , 160 * 3/2 , 0);
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
                AsteroidsData.AsteroidDirection = new Vector2[FieldSize, FieldSize];
                AsteroidsData.AsteroidSpeed = new float[FieldSize, FieldSize];
                SaveExists = false;
            }
        }
        else {
            AsteroidsData.AsteroidDirection = new Vector2[FieldSize, FieldSize];
            AsteroidsData.AsteroidSpeed = new float[FieldSize, FieldSize];
            SaveExists = false;
        }
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

[Serializable]
public class AsteroidsData
{
    public Vector2[,] AsteroidDirection;
    public float[,] AsteroidSpeed;
}