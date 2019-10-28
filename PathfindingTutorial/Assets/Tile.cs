using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public class Tile : MonoBehaviour
{
    [SerializeField]
    private TileType _tileType;

    public UnityEvent<Tile> OnTouchTile;

    [SerializeField]
    private float _fCost = 0;

    public float FCost
    {
        get
        {
            _fCost = GCost + HCost;
            return _fCost;
        }
    }

    /// <summary>
    ///G is the movement cost from the start point A to the current square.
    ///So for a square adjacent to the start point A, this would be 1,
    ///but this will increase as we get farther away from the start point.
    /// -> The cost of moving to the next square.
    /// </summary>
    public float GCost = 0;

    /// <summary>
    ///H is the estimated movement cost from the current square to the destination point
    ///(we’ll call this point B for Bone!)
    ///This is often called the heuristic because we don’t really know the cost yet – it’s just an estimate.
    /// -> The distance to the goal from this node.
    /// </summary>
    public float HCost = 0;

    public Tile ParentTile; //For the AStar algoritm, will store what node it previously came from so it cn trace the shortest path.

    [SerializeField]
    private Material _pathMat;

    private MeshRenderer _meshRender;

    // Start is called before the first frame update
    void Start()
    {
        _meshRender = GetComponent<MeshRenderer>();
    }

    private void OnMouseDown()
    {
        if (_tileType == TileType.Block)
        {
            Debug.Log("It's a block");
            return;
        }

        Debug.Log($"{name} OnMouseDown() {_tileType}");
        OnTouchTile?.Invoke(this);
    }

    private void OnDrawGizmos()
    {
        Handles.Label(transform.position, $"Score: {FCost}");

        Vector3 namePos = transform.position;
        namePos.y = 1f;
        Handles.Label(namePos, $"{name}");
    }

    public void ChangeToPathColor()
    {
        _meshRender.material = _pathMat;
    }
}

public enum TileType
{
    None        = -1,
    Road        = 0,
    Block       = 1,
}

