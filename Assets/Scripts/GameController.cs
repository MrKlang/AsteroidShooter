using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;
using System;

public class GameController : MonoBehaviour
{
    public Text Text;
    public GameObject GridContainer;
    public GameObject Asteroid;
    public int FieldSize;
    public Transform Player;

    private static int TotalPoints = 0;

    private void Awake()
    {
        Time.timeScale = 1;
        //AllAsteroids = new Transform[FieldSize,FieldSize];
    }

    void Start()
    {
        for (int x = 0; x < FieldSize; x++)
        {
            for (int y = 0; y < FieldSize; y++)
            {
                var objInst = Instantiate(Asteroid, new Vector3Int((x * 3) - 80 * 3, (y * 3) - 80 * 3, 0), Quaternion.identity, GridContainer.transform);
                objInst.GetComponent<AsteroidController>().controller = this;
                //AllAsteroids[x, y] = objInst.transform;
                //Debug.LogError(string.Format("LocalPosition: x: {0}, y: {1};/n CellPosition: x: {2}, y: {3}", objInst.transform.localPosition.x, objInst.transform.localPosition.y, TilesMap.GetCellCenterLocal(new Vector3Int(x, y, 0)).x, TilesMap.GetCellCenterLocal(new Vector3Int(x, y, 0)).y));
            }
        }

        System.GC.Collect();

        Player.localPosition = new Vector3(160 * 3/2 , 160 * 3/2 , 0);
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