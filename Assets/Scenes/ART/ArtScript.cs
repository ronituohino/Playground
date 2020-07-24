using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using Unity.Entities.UniversalDelegates;

public class ArtScript : MonoBehaviour
{
    [SerializeField] int gridSize;
    int actualGridSize;
    [SerializeField] float moveSpeed;

    float cellWidth;
    float cellHeight;

    Vector3 cellUnitWorld;
    [SerializeField] Color[] palette;

    [SerializeField] Camera mainCam;
    [SerializeField] Material baseMat;
    Vector3[,] vertexArray;

    // Start is called before the first frame update
    void Start()
    {
        cellWidth = Screen.width / gridSize;
        cellHeight = Screen.height / gridSize;

        //+1 so that when the vertices are moved, there is no visible background
        actualGridSize = gridSize + 1;
        int vertGridSize = actualGridSize + 1;

        cellUnitWorld = mainCam.ScreenToWorldPoint(new Vector3(0, 0, 0)) * -1 - mainCam.ScreenToWorldPoint(new Vector3(cellWidth, cellHeight, 0)) * -1 ;
        mainCam.transform.position = new Vector3(cellUnitWorld.x * actualGridSize * 0.5f, cellUnitWorld.y * actualGridSize * 0.5f, -1);

        //Create the vertices that are going to be connected
        vertexArray = new Vector3[vertGridSize, vertGridSize];
        for (int y = 0; y < vertGridSize; y++)
        {
            for (int x = 0; x < vertGridSize; x++)
            {
                vertexArray[x, y] = new Vector3(x * cellUnitWorld.x, y * cellUnitWorld.y, 0) + 
                    new Vector3(Random.Range(-cellUnitWorld.x * 0.33f, cellUnitWorld.x * 0.33f), Random.Range(-cellUnitWorld.y * 0.33f, cellUnitWorld.y * 0.33f), 0);
            }
        }

        //Create triangles that connect vertices
        for (int y = 0; y < actualGridSize; y++)
        {
            for (int x = 0; x < actualGridSize; x++)
            {
                CreateTriangle(x, y, true);
                CreateTriangle(x, y, false);
            }
        }
    }

    void CreateTriangle(int x, int y, bool upper)
    {
        GameObject g = new GameObject((y * actualGridSize + x).ToString() + (upper ? "U" : "D"));

        MeshRenderer mr = g.AddComponent<MeshRenderer>();
        MeshFilter mf = g.AddComponent<MeshFilter>();

        Mesh m = new Mesh();

        Vector3[] vertices = new Vector3[3];
        if(upper)
        {
            vertices[0] = vertexArray[x, y];
            vertices[1] = vertexArray[x, y + 1];
            vertices[2] = vertexArray[x + 1, y + 1];
        } 
        else
        {
            vertices[0] = vertexArray[x, y];
            vertices[1] = vertexArray[x + 1, y + 1];
            vertices[2] = vertexArray[x + 1, y];

        }
        

        m.vertices = vertices;
        m.triangles = new int[3] { 0, 1, 2 };

        mf.mesh = m;

        Material mat = new Material(baseMat);
        mat.SetColor("_BaseColor", palette[Random.Range(0, palette.Length)]);
        mr.material = mat;
    }
}
