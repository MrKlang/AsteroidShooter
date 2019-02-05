using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
//using Unity.Entities;

public class GameController : MonoBehaviour
{
    public Text Text;
    public Tilemap TilesMap;
    public GameObject Asteroid;
    public int FieldSize;

    private static int TotalPoints = 0;

    private int KURWA = 0;

    private void Awake()
    {
        Time.timeScale = 1;
    }

    void Start()
    {
        for (int x = 0; x < FieldSize; x++)
        {
            for (int y = 0; y < FieldSize; y++)
            {
                if (KURWA < 320)
                {
                    var objInst = Instantiate(Asteroid, TilesMap.transform);
                    objInst.transform.localPosition = TilesMap.GetCellCenterLocal(new Vector3Int(x, y, 0));
                    objInst.transform.localPosition = new Vector3(objInst.transform.localPosition.x + TilesMap.cellSize.x / 2, objInst.transform.localPosition.y + TilesMap.cellSize.y / 2, objInst.transform.localPosition.z);
                    objInst.GetComponent<AsteroidController>().controller = this;
                    //KURWA++;
                }
                //Debug.LogError(string.Format("LocalPosition: x: {0}, y: {1};/n CellPosition: x: {2}, y: {3}", objInst.transform.localPosition.x, objInst.transform.localPosition.y, TilesMap.GetCellCenterLocal(new Vector3Int(x, y, 0)).x, TilesMap.GetCellCenterLocal(new Vector3Int(x, y, 0)).y));
            }
        }

        System.GC.Collect();
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


//public class AsteroidSystem : ComponentSystem{

//    protected override void OnUpdate()
//    {

//    }

//}