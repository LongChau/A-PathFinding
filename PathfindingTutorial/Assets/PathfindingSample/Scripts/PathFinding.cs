using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;

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
    private int curWaypointIndex = 0;
    [SerializeField]
    private float speed = 10f;

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
        // Fire emblem style movement
        { 0, new Vector3(0f, 0f, 10f) },
        { 1, new Vector3(0f, 0f, -10f) },
        { 2, new Vector3(10f, 0f, 0f) },
        { 3, new Vector3(-10f, 0f, 0f) },

        // 8 path movement
        { 4, new Vector3(-10f, 0f, 10f) },
        { 5, new Vector3(10f, 0f, 10f) },
        { 6, new Vector3(-10f, 0f, -10f) },
        { 7, new Vector3(10f, 0f, -10f) },
    };

    [SerializeField]
    private Tile _destination;

    [SerializeField]
    private Tile _startPlace;

    [SerializeField]
    private bool _isFoundPath;

    [SerializeField]
    private Tile currentTile;

    [SerializeField]
    private Transform _parentTransform;

    [SerializeField, Header("Final path")]
    private List<Tile> _listResult = new List<Tile>();

    public Tile Destination { get => _destination; set => _destination = value; }

    // Start is called before the first frame update
    void Start()
    {
        foreach (var tile in _listTiles)
        {
            var tileComp = tile.GetComponent<Tile>();
            tileComp.OnTouchTile = Handle_OnTouchTile;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (CanFindPath())
        {
            AutoFindPath();
        }

        MoveCharacter();
    }


    private void MoveCharacter()
    {
        if (_listResult.Count != 0)
        {
            if (curWaypointIndex < _listResult.Count)
            {
                var curPointPos = _listResult[curWaypointIndex].transform.position;
                _parentTransform.Translate((curPointPos - _parentTransform.position).normalized * speed * Time.deltaTime);

                if (Vector3.Distance(_listResult[curWaypointIndex].transform.position, _parentTransform.position) <= 0.5f)
                {
                    //Debug.Log("Next waypoint");
                    _parentTransform.position = curPointPos;
                    curWaypointIndex++;
                }
            }
        }
    }

    private bool CanFindPath()
    {
        return !_isFoundPath && _destination != null;
    }

    private void FindStartPlace()
    {
        if (_startPlace == null)
        {
            var newPos = transform.position;

            RaycastHit raycastHitInfo;

            if (Physics.Raycast(new Ray(newPos, Vector3.down * 10f), out raycastHitInfo))
            {
                if (raycastHitInfo.collider != null && raycastHitInfo.collider.CompareTag("Road"))
                {
                    var tile = raycastHitInfo.collider.transform;
                    var tileComp = tile.GetComponent<Tile>();

                    _startPlace = tileComp;
                }
            }
        }
    }

    private void AutoFindPath()
    {
        FindStartPlace();

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
        FindStartPlace();

        // add our first position in open list
        _listOpen.Add(_startPlace);

        FindPathContiniously();
    }

    [ContextMenu("FindPathContiniously")]
    private void FindPathContiniously()
    {
        var sortedList = _listOpen.OrderBy(tile => tile.FCost).ToList();
        _listOpen = sortedList;

        currentTile = _listOpen[0];    // get lowest score

        if (!_listClosed.Contains(currentTile))
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
        for (int directionIndex = 0; directionIndex < _directions; directionIndex++)
        {
            var directionPos = _dictDirections[directionIndex];
            var newPos = currentTile.transform.position + directionPos + Vector3.up * 2f;

            Debug.DrawRay(newPos, Vector3.down * 10f, Color.green);

            RaycastHit raycastHitInfo;

            if (Physics.Raycast(new Ray(newPos, Vector3.down * 10f), out raycastHitInfo))
            {
                if (raycastHitInfo.collider != null && raycastHitInfo.collider.CompareTag("Road"))
                {
                    var tile = raycastHitInfo.collider.transform;
                    var tileComp = tile.GetComponent<Tile>();

                    //Debug.Log($"Tile score: {tileComp.FCost}");

                    // calculate score
                    tileComp.HCost = CalculateManhattanDistance(tile.transform.position.x, _destination.transform.position.x,
                        tile.transform.position.z, _destination.transform.position.z);
                    tileComp.GCost = CalculateManhattanDistance(tile.transform.position.x, currentTile.transform.position.x,
                        tile.transform.position.z, currentTile.transform.position.z);

                    // ignore if duplicate
                    if (!_listWalkableTiles.Contains(tileComp))
                    {
                        // Retrieve all walkable tiles
                        _listWalkableTiles.Add(tileComp);
                    }
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
        ResetPath();
        FindPath();
    }

    private void ResetPath()
    {
        _listOpen.Clear();
        _listClosed.Clear();
        _listWalkableTiles.Clear();

        currentTile = null;
        _startPlace = null;

        _isFoundPath = false;

        curWaypointIndex = 0;

        foreach (var tile in _listTiles)
        {
            var tileComp = tile.GetComponent<Tile>();
            tileComp.ResetMat();
        }
    }

    public void Handle_OnTouchTile(Tile tile)
    {
        Debug.Log($"Handle_OnTouchTile({tile.transform.position})");
        _destination = tile;

        ResetPath();
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

    void GetFinalPath()
    {
        Debug.Log("GetFinalPath()");

        List<Tile> listFinalPath = new List<Tile>();//List to hold the path sequentially
        Tile currentTile = _destination;//Node to store the current node being checked

        currentTile.ChangeToDestinationColor();

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
