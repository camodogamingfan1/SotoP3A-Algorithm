using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Timeline;

public class PathMarker
{
    public MapLocation location;
    public float G;
    public float H;
    public float F;
    public GameObject marker;
    public PathMarker parent;
    public PathMarker(MapLocation l, float g, float h, float f, GameObject marker, PathMarker p)
    {
        location = l;
        G = g;
        H = h;
        F = f;
        this.marker = marker;
        parent = p;
    }
    public override bool Equals(object obj)
    {
        if ((obj == null) || !this.GetType().Equals(obj.GetType()))
        {
            return false;
        }
        else
        {
            return location.Equals(((PathMarker)obj).location);
        }
    }
    public override int GetHashCode()
    {
        // return a proper hash to avoid collisions in hash-based collections
        return location != null ? location.GetHashCode() : 0;
    }
}


public class FindPathAStar : MonoBehaviour
{
    public Maze maze;
    public Material closedmaterial;
    public Material openmaterial;

    List<PathMarker> open = new List<PathMarker>();
    List<PathMarker> closed = new List<PathMarker>();

    public GameObject start;
    public GameObject end;
    public GameObject pathP;

    PathMarker goalNode;
    PathMarker startNode;

    PathMarker lastPosition;
    bool done = false;

    void RemoveAllMarkers()
    {
        GameObject[] markers = GameObject.FindGameObjectsWithTag("marker");
        foreach (GameObject m in markers)
            Destroy(m);
    }

    void BeginSearch()
    {
        done = false;
        RemoveAllMarkers();

        List<MapLocation> locations = new List<MapLocation>();
        for (int z = 1; z < maze.depth - 1; z++)
            for (int x = 1; x < maze.width; x++)
            {
                if (maze.map[x, z] != 1)
                    locations.Add(new MapLocation(x, z));
            }
        locations.Shuffle();

        if (locations.Count == 0) return;

        Vector3 startLocation = new Vector3(locations[0].x * maze.scale, 0, locations[0].z * maze.scale);
        startNode = new PathMarker(new MapLocation(locations[0].x, locations[0].z), 0, 0, 0, Instantiate(start, startLocation, Quaternion.identity), null);

        Vector3 goalLocation = new Vector3(locations[0].x * maze.scale, 0, locations[0].z * maze.scale);
        goalNode = new PathMarker(new MapLocation(locations[0].x, locations[0].z), 0, 0, 0, Instantiate(end, goalLocation, Quaternion.identity), null);

        open.Clear();
        closed.Clear();
        open.Add(startNode);
        lastPosition = startNode;
    }

    void Search(PathMarker thisNode)
    {
        if (thisNode == null) return;
        if (thisNode.Equals(goalNode)) { done = true; return; } // goal has been found

        foreach (MapLocation dir in maze.directions)
        {
            MapLocation neighbour = dir + thisNode.location;

            // bounds check: x and z should be within valid range
            if (neighbour.x < 1 || neighbour.x >= maze.width || neighbour.z < 1 || neighbour.z >= maze.depth) continue;
            if (maze.map[neighbour.x, neighbour.z] == 1) continue;
            if (IsClosed(neighbour)) continue;

            float G = Vector2.Distance(thisNode.location.ToVector(), neighbour.ToVector()) + thisNode.G;
            float H = Vector2.Distance(neighbour.ToVector(), goalNode.location.ToVector());
            float F = G + H;

            GameObject pathBlock = Instantiate(pathP, new Vector3(neighbour.x * maze.scale, 0, neighbour.z * maze.scale), Quaternion.identity);

            TextMesh[] values = pathBlock.GetComponentsInChildren<TextMesh>();
            if (values.Length >= 3)
            {
                values[0].text = "G:" + G.ToString("0.00");
                values[1].text = "H:" + H.ToString("0.00");
                values[2].text = "F:" + F.ToString("0.00");
            }

            if (!UpdateMarker(neighbour, G, H, F, thisNode))
                open.Add(new PathMarker(neighbour, G, H, F, pathBlock, thisNode));
            else
                Destroy(pathBlock); // avoid duplicate marker objects
        }

        if (open.Count == 0) return;

        open = open.OrderBy(p => p.F).ThenBy(n => n.H).ToList();
        PathMarker pm = open.ElementAt(0);
        closed.Add(pm);

        open.RemoveAt(0);
        if (pm.marker != null)
        {
            var renderer = pm.marker.GetComponent<Renderer>();
            if (renderer != null) renderer.material = closedmaterial;
        }

        lastPosition = pm;
    }

    private bool UpdateMarker(MapLocation pos, float g, float h, float f, PathMarker prt)
    {
        for (int i = 0; i < open.Count; i++)
        {
            var p = open[i];
            if (p.location.Equals(pos))
            {
                // update if new G is better
                if (g < p.G)
                {
                    p.G = g;
                    p.H = h;
                    p.F = f;
                    p.parent = prt;

                    if (p.marker != null)
                    {
                        TextMesh[] values = p.marker.GetComponentsInChildren<TextMesh>();
                        if (values.Length >= 3)
                        {
                            values[0].text = "G:" + g.ToString("0.00");
                            values[1].text = "H:" + h.ToString("0.00");
                            values[2].text = "F:" + f.ToString("0.00");
                        }
                        var renderer = p.marker.GetComponent<Renderer>();
                        if (renderer != null) renderer.material = openmaterial;
                    }
                }

                return true; // marker existed (updated or not)
            }
        }
        return false; // marker not found in open list
    }

    private bool IsClosed(MapLocation marker)
    {
        foreach (PathMarker p in closed)
        {
            if (p.location.Equals(marker)) return true;
        }
        return false;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    void GetPath()
    {
        RemoveAllMarkers();
        PathMarker current = lastPosition;

        while (current != null && !startNode.Equals(current))
        {
            Instantiate(pathP, new Vector3(current.location.x * maze.scale, 0, current.location.z * maze.scale), Quaternion.identity);
            current = current.parent;
        }

        if (startNode != null)
            Instantiate(pathP, new Vector3(startNode.location.x * maze.scale, 0, startNode.location.z * maze.scale), Quaternion.identity);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) BeginSearch();
        if (Input.GetKeyDown(KeyCode.C) && !done) Search(lastPosition);
        if (Input.GetKeyDown(KeyCode.M)) GetPath();
    }
}



