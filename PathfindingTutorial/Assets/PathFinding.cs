using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Play the game
/// 
/// For step by step:
/// 1. StartFindPath()
/// 2. Click to FindPathContiniously() until reach the goal
///
/// For automatic: Just play the game with code in update enable
/// 
/// </summary>
public class PathFinding : MonoBehaviour
{
    [SerializeField]
    private List<Transform> _listTiles = new List<Transform>();

    [SerializeField]
    private List<Tile> _listOpen = new List<Tile>();
    [SerializeField]
    private List<Tile> _listClosed = new List<Tile>();

    [SerializeField]
    private int _directions = 4;

    private Dictionary<int, Vector3> _dictDirections = new Dictionary<int, Vector3>()
    {
        { 0, new Vector3(0f, 0f, 10f) },
        { 1, new Vector3(0f, 0f, -10f) },
        { 2, new Vector3(10f, 0f, 0f) },
        { 3, new Vector3(-10f, 0f, 0f) },
    };

    [SerializeField]
    private Tile _destination;

    [SerializeField]
    private Tile _startPlace;

    [SerializeField]
    private bool _isFoundPath;

    // Start is called before the first frame update
    void Start()
    {
        foreach (var tile in _listTiles)
        {
            var tileComp = tile.GetComponent<Tile>();
            tileComp?.OnTouchTile?.AddListener(Handle_OnTouchTile);
        }

     
    }

    // Update is called once per frame
    void Update()
    {
        if (!_isFoundPath)
            AutoFindPath();
    }

    private void AutoFindPath()
    {
        // add our first position in open list
        _listOpen.Add(_startPlace);

        while (_listOpen.Count > 0)
        {
            FindPathContiniously();
        }
    }

    [SerializeField]
    private List<Tile> _listWalkableTiles = new List<Tile>();
    private void FindPath()
    {
        // add our first position in open list
        _listOpen.Add(_startPlace);

        //while (_listOpen.Count > 0)
        //{
        FindPathContiniously();
        //}

        // TODO: END LOOP HERE
    }

    [SerializeField]
    private Tile currentTile;
    [ContextMenu("FindPathContiniously")]
    private void FindPathContiniously()
    {
        var sortedList = _listOpen.OrderBy(tile => tile.FCost).ToList();
        _listOpen = sortedList;

        currentTile = _listOpen[0];    // get lowest score

        _listClosed.Add(currentTile);   // add lowest score tile to closed list
        _listOpen.Remove(currentTile);  // remove lowest score tile from opened list

        if (currentTile.transform.position == _destination.transform.position)
        {
            // PATH FOUND. End loop
            GetFinalPath();
            return;
            // or break;
        }

        // find adjacent tiles
        //List<Tile> _listWalkableTiles = new List<Tile>();
        for (int directionIndex = 0; directionIndex < _directions; directionIndex++)
        {
            var directionPos = _dictDirections[directionIndex];
            var newPos = currentTile.transform.position + directionPos + Vector3.up * 2f;

            RaycastHit raycastHitInfo;

            Debug.DrawRay(newPos, Vector3.down * 10f, Color.green);

            if (Physics.Raycast(new Ray(newPos, Vector3.down * 10f), out raycastHitInfo))
            {
                if (raycastHitInfo.collider != null && raycastHitInfo.collider.CompareTag("Road"))
                {
                    var tile = raycastHitInfo.collider.transform;
                    var tileComp = tile.GetComponent<Tile>();

                    Debug.Log($"Tile score: {tileComp.FCost}");

                    // calculate score
                    tileComp.HCost = CalculateManhattanDistance(tile.transform.position.x, _destination.transform.position.x,
                        tile.transform.position.z, _destination.transform.position.z);
                    tileComp.GCost = CalculateManhattanDistance(tile.transform.position.x, currentTile.transform.position.x,
                        tile.transform.position.z, currentTile.transform.position.z);

                    // Retrieve all walkable tiles
                    _listWalkableTiles.Add(tileComp);
                }
            }
        }

        foreach (var neighborTile in _listWalkableTiles)
        {
            if (_listClosed.Contains(neighborTile)) // if tile in closed list then ignore it
                continue;   // go to next tile

            //Calculate its score
            var tileComp = neighborTile.GetComponent<Tile>();

            float moveCost = neighborTile.FCost;//Get the F cost of that neighbor

            if (moveCost < neighborTile.GCost || !_listOpen.Contains(neighborTile))
            {
                tileComp.ParentTile = currentTile;

                if (!_listOpen.Contains(neighborTile))  // if not in opened list
                {
                    // add to open list
                    _listOpen.Add(neighborTile);
                }
            }
        }
    }

    private int ComparisonTwoTuples(Tile x, Tile y)
    {
        var part1 = x.FCost;
        var part2 = y.FCost;
        var compareResult = part1.CompareTo(part2);

        // Return the result of the first CompareTo.
        return compareResult;
    }

    [ContextMenu("StartFindPath")]
    private void StartFindPath()
    {
        _listOpen.Clear();
        _listClosed.Clear();
        _listWalkableTiles.Clear();

        FindPath();
    }

    public void Handle_OnTouchTile(Tile tile)
    {
        Debug.Log($"Handle_OnTouchTile({tile.transform.position})");
        _destination = tile;
    }

    /// <summary>
    /// Calculates the Manhattan distance between the two points.
    /// </summary>
    /// <param name="x1">The first x coordinate.</param>
    /// <param name="x2">The second x coordinate.</param>
    /// <param name="y1">The first y coordinate.</param>
    /// <param name="y2">The second y coordinate.</param>
    /// <returns>The Manhattan distance between (x1, y1) and (x2, y2)</returns>
    public float CalculateManhattanDistance(float x1, float x2, float y1, float y2)
    {
        return Math.Abs(x1 - x2) + Math.Abs(y1 - y2);
    }

    private int ComparisonTwoTuples(Tuple<Tile> a, Tuple<Tile> b)
    {
        // Here we sort two times at once, first one the first item, then on the second.
        // ... Compare the first items of each element.
        var part1 = a.Item1.FCost;
        var part2 = b.Item1.FCost;
        var compareResult = part1.CompareTo(part2);

        // Return the result of the first CompareTo.
        return compareResult;
    }

    [SerializeField, Header("Final path")]
    private List<Tile> _listResult = new List<Tile>();
    void GetFinalPath()
    {
        Debug.Log("GetFinalPath()");

        List<Tile> listFinalPath = new List<Tile>();//List to hold the path sequentially
        Tile currentTile = _destination;//Node to store the current node being checked
        while (currentTile != _startPlace)//While loop to work through each node going through the parents to the beginning of the path
        {
            listFinalPath.Add(currentTile);//Add that node to the final path
            currentTile = currentTile.ParentTile;//Move onto its parent node
            currentTile.ChangeToPathColor();
        }

        listFinalPath.Reverse();//Reverse the path to get the correct order
        _listResult = listFinalPath;//Set the final path

        _isFoundPath = true;
    }
}
