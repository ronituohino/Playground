using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

public class WavefunctionCollapse : MonoBehaviour
{
    public Tilemap tilemap;
    public Vector2Int outputSize;

    [Space]

    public bool rotatedRelations;
    public bool mirroredRelations;

    Vector2Int gridSize;
    bool readTileMap = true;
    bool exit = false;

    List<TileBase> tiles = new List<TileBase>();

    public TileBase multipleSolutionsTile;
    public TileBase nullTile;

    //Amount of times each tile comes up in the original image
    List<int> tileAmounts = new List<int>();
    float[] weights;

    int[,] tileMap;

    List<Relation> tileRelations = new List<Relation>();
    Vector2Int[] surrounding = new Vector2Int[8]
    {
        new Vector2Int(0,1),
        new Vector2Int(1,1),
        new Vector2Int(1,0),
        new Vector2Int(1,-1),
        new Vector2Int(0,-1),
        new Vector2Int(-1,-1),
        new Vector2Int(-1,0),
        new Vector2Int(-1,1)
    };
    Vector2Int nullCoords = new Vector2Int(-1, -1);

    int[,][] possibleTiles;
    float[,] entropyMap;

    bool advance = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PrintExamples();
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            advance = true;
        }
    }

    public void PrintExamples()
    {
        if (readTileMap)
        {
            gridSize = CalculateGridSize();
            tileMap = BuildIntegerMap();
            weights = CalculateTileProbabilities();

            //Search tile relations from reference
            BuildRelations();

            readTileMap = false;
        }

        //Clear
        for (int y = 0; y < outputSize.y; y++)
        {
            for (int x = 0; x < outputSize.x; x++)
            {
                tilemap.SetTile(new Vector3Int(x - outputSize.x / 2, y - outputSize.y / 2, 0), null);
            }
        }

        //Wavefunction collapse
        //Set all tiles to be possible solutions to each position
        //Set all entropy to maximum
        possibleTiles = new int[outputSize.x, outputSize.y][];
        entropyMap = new float[outputSize.x, outputSize.y];

        int count = tiles.Count;
        int[] allTiles = new int[count];
        for (int i = 0; i < count; i++)
        {
            allTiles[i] = i;
        }

        for (int y = 0; y < outputSize.y; y++)
        {
            for (int x = 0; x < outputSize.x; x++)
            {
                possibleTiles[x, y] = allTiles;
            }
        }

        for (int y = 0; y < outputSize.y; y++)
        {
            for (int x = 0; x < outputSize.x; x++)
            {
                entropyMap[x, y] = float.MaxValue;
            }
        }

        //Collapse
        StartCoroutine(Step());
    }

    #region STEPPING

    IEnumerator Step()
    {
        bool first = true;
        int collapsesDone = 0;

        for (int l = 0; l < 400; l++)
        //while (!IsDone())
        {
            yield return new WaitUntil(TakeStep);
            if (first)
            {
                first = false;
                Collapse(Random.Range(0, outputSize.x), Random.Range(0, outputSize.y));
            }
            else
            {
                Vector2Int v = FindLowestEntropy();
                if (v == nullCoords)
                {
                    break;
                }
                Collapse(v.x, v.y);
            }
            collapsesDone++;

            Output();
        }

        Output();
    }

    bool TakeStep()
    {
        if (advance)
        {
            advance = false;
            return true;
        }
        return false;
    }

    void Output()
    {
        for (int y = 0; y < outputSize.y; y++)
        {
            for (int x = 0; x < outputSize.x; x++)
            {
                int[] solutions = possibleTiles[x, y];
                if (solutions.Length == 1)
                {
                    tilemap.SetTile(new Vector3Int(x - outputSize.x / 2, y - outputSize.y / 2, 0), tiles[possibleTiles[x, y][0]]);
                }
                else if (solutions.Length > 1)
                {
                    tilemap.SetTile(new Vector3Int(x - outputSize.x / 2, y - outputSize.y / 2, 0), multipleSolutionsTile);
                }
                else
                {
                    tilemap.SetTile(new Vector3Int(x - outputSize.x / 2, y - outputSize.y / 2, 0), nullTile);
                    //Debug.Log("Error: " + x + " " + y + " ");
                }
            }
        }
    }

    #endregion
    #region SETUP

    Vector2Int CalculateGridSize()
    {
        int gridWidth;
        int gridHeight;

        int position = 0;
        //X size
        while (true)
        {
            TileBase tb = tilemap.GetTile(new Vector3Int(position, 0, 0));
            if (tb == null)
            {
                gridWidth = position;
                break;
            }
            position++;
        }

        position = 0;
        //Y size
        while (true)
        {
            TileBase tb = tilemap.GetTile(new Vector3Int(0, position, 0));
            if (tb == null)
            {
                gridHeight = position;
                break;
            }
            position++;
        }

        return new Vector2Int(gridWidth * 2, gridHeight * 2);
    }

    //Looks through the tilemap and searches for every kind of tile that is used
    //And assigns each tile an integer to be used in building relations
    int[,] BuildIntegerMap()
    {
        int[,] tileMap = new int[gridSize.x, gridSize.y];

        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                TileBase tb = tilemap.GetTile(new Vector3Int(x - gridSize.x / 2, y - gridSize.y / 2, 0));

                if (tb == null)
                {
                    Debug.Log("Tilemap not symmetrical or it is missing tiles! " + x + " " + y);
                    exit = true;
                    break;
                }

                //If we haven't registered this tile yet, add it to the list
                if (!tiles.Contains(tb))
                {
                    tiles.Add(tb);
                    tileAmounts.Add(0);
                }

                int intRepresentation = GetTileInt(tb);
                tileMap[x, y] = intRepresentation;
                tileAmounts[intRepresentation] += 1;
            }

            if (exit)
            {
                break;
            }
        }

        return tileMap;
    }

    float[] CalculateTileProbabilities()
    {
        int count = tiles.Count;
        int tileCount = gridSize.x * gridSize.y;
        float[] arr = new float[count];

        for (int i = 0; i < count; i++)
        {
            arr[i] = (float)tileAmounts[i] / (float)tileCount;
        }

        return arr;
    }

    //Tile integers are just their positions in the "tiles" -list
    int GetTileInt(TileBase tile)
    {
        int count = tiles.Count;
        for (int i = 0; i < count; i++)
        {
            if (tiles[i] == tile)
            {
                return i;
            }
        }

        Debug.Log("Error fetching tile with name: " + name);
        return -1;
    }



    private struct Relation
    {
        public int tile;
        public int[] surroundingTiles;

        public Relation(int tile, int[] surroundingTiles)
        {
            this.tile = tile;
            this.surroundingTiles = surroundingTiles;
        }

        public string LogSurrroundingTiles()
        {
            return
                surroundingTiles[0].ToString() +
                surroundingTiles[1].ToString() +
                surroundingTiles[2].ToString() +
                surroundingTiles[3].ToString() +
                surroundingTiles[4].ToString() +
                surroundingTiles[5].ToString() +
                surroundingTiles[6].ToString() +
                surroundingTiles[7].ToString();
        }

        public static bool operator ==(Relation a, Relation b)
        {
            if (a.tile == b.tile)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (a.surroundingTiles[i] != b.surroundingTiles[i])
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool operator !=(Relation a, Relation b)
        {
            if (a.tile == b.tile)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (a.surroundingTiles[i] != b.surroundingTiles[i])
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                return true;
            }
        }

        //Omg this is cool, works with List<T>.Contains()
        public override bool Equals(object obj)
        {
            if (obj is Relation)
            {
                Relation r = (Relation)obj;
                return this == r;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                // Choose large primes to avoid hashing collisions
                const int HashingBase = (int)2166136261;
                const int HashingMultiplier = 16777619;

                int hash = HashingBase;
                hash = (hash * HashingMultiplier) ^ (!Object.ReferenceEquals(null, tile) ? tile.GetHashCode() : 0);
                hash = (hash * HashingMultiplier) ^ (!Object.ReferenceEquals(null, surroundingTiles) ? surroundingTiles.GetHashCode() : 0);
                return hash;
            }
        }
    }

    void BuildRelations()
    {
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                int tile = tileMap[x, y];

                int[] surroundingTiles = new int[8]
                {
                    Fetch(x, y + 1),
                    Fetch(x + 1, y + 1),
                    Fetch(x + 1, y),
                    Fetch(x + 1, y - 1),
                    Fetch(x, y - 1),
                    Fetch(x - 1, y - 1),
                    Fetch(x - 1, y),
                    Fetch(x - 1, y + 1),
                };

                Relation r = new Relation(tile, surroundingTiles);

                if (!tileRelations.Contains(r))
                {
                    tileRelations.Add(r);
                }
            }
        }

        if (rotatedRelations)
        {
            for (int i = 0; i < tileRelations.Count; i++)
            {
                Relation r = tileRelations[i];

                Relation r1 = new Relation(r.tile, RotateClockwise(r.surroundingTiles));
                if (!tileRelations.Contains(r1))
                {
                    tileRelations.Add(r1);
                }

                Relation r2 = new Relation(r.tile, RotateClockwise(r1.surroundingTiles));
                if (!tileRelations.Contains(r2))
                {
                    tileRelations.Add(r2);
                }

                Relation r3 = new Relation(r.tile, RotateClockwise(r2.surroundingTiles));
                if (!tileRelations.Contains(r3))
                {
                    tileRelations.Add(r3);
                }
            }
        }

        if (mirroredRelations)
        {
            for (int i = 0; i < tileRelations.Count; i++)
            {
                Relation r = tileRelations[i];

                Relation r1 = new Relation(r.tile, MirrorH(r.surroundingTiles));
                if (!tileRelations.Contains(r1))
                {
                    tileRelations.Add(r1);
                }

                Relation r2 = new Relation(r.tile, MirrorV(r.surroundingTiles));
                if (!tileRelations.Contains(r2))
                {
                    tileRelations.Add(r2);
                }

                Relation r3 = new Relation(r.tile, MirrorV(MirrorH(r.surroundingTiles)));
                if (!tileRelations.Contains(r3))
                {
                    tileRelations.Add(r3);
                }
            }
        }
    }

    //Returns surroundingTiles, rotated 90 degrees
    int[] RotateClockwise(int[] tiles)
    {
        int[] newTiles = new int[8];
        newTiles[0] = tiles[6];
        newTiles[1] = tiles[7];
        newTiles[2] = tiles[0];
        newTiles[3] = tiles[1];
        newTiles[4] = tiles[2];
        newTiles[5] = tiles[3];
        newTiles[6] = tiles[4];
        newTiles[7] = tiles[5];

        return newTiles;
    }

    int[] MirrorH(int[] tiles)
    {
        int[] newTiles = new int[8];
        newTiles[0] = tiles[4];
        newTiles[1] = tiles[3];
        newTiles[2] = tiles[2];
        newTiles[3] = tiles[1];
        newTiles[4] = tiles[0];
        newTiles[5] = tiles[7];
        newTiles[6] = tiles[6];
        newTiles[7] = tiles[5];

        return newTiles;
    }

    int[] MirrorV(int[] tiles)
    {
        int[] newTiles = new int[8];
        newTiles[0] = tiles[0];
        newTiles[1] = tiles[7];
        newTiles[2] = tiles[6];
        newTiles[3] = tiles[5];
        newTiles[4] = tiles[4];
        newTiles[5] = tiles[3];
        newTiles[6] = tiles[2];
        newTiles[7] = tiles[1];

        return newTiles;
    }

    //Returns tilemap values safely
    int Fetch(int x, int y)
    {
        if (!WithinBounds(x, y, gridSize))
        {
            return -1;
        }
        else
        {
            return tileMap[x, y];
        }
    }

    bool WithinBounds(int x, int y, Vector2Int size)
    {
        if (x < 0 || x >= size.x || y < 0 || y >= size.y)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    #endregion
    #region COLLAPSE

    //Is the possibleTiles fully collapsed?
    bool IsDone()
    {
        for (int y = 0; y < outputSize.y; y++)
        {
            for (int x = 0; x < outputSize.x; x++)
            {
                if (entropyMap[x, y] > 0)
                {
                    return false;
                }
            }
        }

        return true;
    }

    //Collapses a tile, updates 8 surrounding tiles with relations, updates entropy
    void Collapse(int x, int y)
    {
        int[] arr = possibleTiles[x, y];

        int len = arr.Length;
        if (len > 0)
        {
            int chosenTile = SelectTileUsingWeights(arr);
            possibleTiles[x, y] = new int[1] { chosenTile };
            entropyMap[x, y] = 0f;

            //Go through the possible relations and wipe out any tiles that can't be next to this tile
            foreach (Vector2Int v in surrounding)
            {
                int tx = x + v.x;
                int ty = y + v.y;

                if (WithinBounds(tx, ty, outputSize) && entropyMap[tx, ty] > 0f)
                {
                    // foreach (int i in possibleTiles[tx, ty])
                    // {
                    //     Debug.Log(v.x + " " + v.y + " Before: " + i);
                    // }
                    ApplyRelations(tx, ty);
                    // foreach (int i in possibleTiles[tx, ty])
                    // {
                    //     Debug.Log(v.x + " " + v.y + " After: " + i);
                    // }


                    //Debug.Log(tx + " " + ty + " :" + entropyMap[tx, ty] + " " + iteration);

                }

            }
        }
        else
        {
            Debug.Log("No solutions left! " + " X: " + x + " Y: " + y);
        }
    }

    int SelectTileUsingWeights(int[] tiles)
    {
        int len = tiles.Length;
        if (len < 1)
        {
            Debug.Log("No solutions left!");
            return -1;
        }
        else if (len == 1)
        {
            return tiles[0];
        }
        else
        {
            float[] specificWeights = GetWeights(tiles);
            float[] scaledWeights = GetProbabilityGradient(specificWeights);
            float random = Random.Range(0f, 1f);

            for (int i = 0; i < len; i++)
            {
                if (random <= scaledWeights[i])
                {
                    return tiles[i];
                }
            }

            Debug.Log("Error finding random weight!");
            return -1;
        }
    }

    //Scales an arry of probabilities into an array from which the points occupy as much space as they have been assigned probability
    public float[] GetProbabilityGradient(float[] arr)
    {
        float sum = 0f;
        foreach (float f in arr)
        {
            sum += f;
        }

        float m = 1 / sum;
        int len = arr.Length;
        float[] newArr = new float[len];
        for (int i = 0; i < len; i++)
        {
            newArr[i] = m * arr[i];
        }

        float[] prob = new float[len];
        prob[0] = newArr[0];
        for (int i = 1; i < len - 1; i++)
        {
            prob[i] = prob[i - 1] + newArr[i];
        }
        prob[len - 1] = 1f;

        return prob;
    }



    //Goes through all listed relations and checks what tiles can be placed here now
    void ApplyRelations(int x, int y)
    {
        int[] possible = possibleTiles[x, y];
        int possibleLen = possible.Length;

        List<int> approvedTiles = new List<int>();

        if (possibleLen > 0)
        {
            int len = tileRelations.Count;
            for (int i = 0; i < len; i++)
            {
                Relation r = tileRelations[i];

                for (int p = 0; p < possibleLen; p++)
                {
                    if (r.tile == possible[p])
                    {
                        if (AreNeighboursValid(x, y, r))
                        {
                            int t = possible[p];
                            if (!approvedTiles.Contains(t))
                            {
                                approvedTiles.Add(t);
                            }
                        }
                    }
                }
            }
        }

        if (approvedTiles.Count < 1)
        {
            Debug.Log("No solutions left! " + " X: " + x + " Y: " + y);
        }

        possibleTiles[x, y] = approvedTiles.ToArray();

        //Set new entropy
        entropyMap[x, y] = Mathf.RoundToInt(ShannonEntropy(GetWeights(possibleTiles[x, y])) * 100000) / 100000f;
    }

    bool AreNeighboursValid(int x, int y, Relation r)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2Int v = surrounding[i];
            if (WithinBounds(x + v.x, y + v.y, outputSize) && !possibleTiles[x + v.x, y + v.y].Contains(r.surroundingTiles[i]) && possibleTiles[x + v.x, y + v.y].Length > 0)
            {
                return false;
            }
        }

        return true;
    }

    int GetVectorIndex(Vector2Int vector)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2Int v = surrounding[i];
            if (v.x == vector.x && v.y == vector.y)
            {
                return i;
            }
        }

        Debug.Log("Error fetching vector index! " + vector.x + " " + vector.y);
        return -1;
    }

    float[] GetWeights(int[] tiles)
    {
        int len = tiles.Length;
        float[] w = new float[len];

        for (int i = 0; i < len; i++)
        {
            w[i] = weights[tiles[i]];
        }

        return w;
    }



    float ShannonEntropy(float[] weights)
    {
        int len = weights.Length;

        float sum = 0f;
        float sumWeightLogWeight = 0f;

        for (int i = 0; i < len; i++)
        {
            float w = weights[i];

            sum += w;
            sumWeightLogWeight += w * Mathf.Log10(w);
        }

        return Mathf.Log10(sum) - sumWeightLogWeight / sum;
    }

    Vector2Int FindLowestEntropy()
    {
        List<Vector2Int> validCoords = new List<Vector2Int>();

        float lowest = float.MaxValue;
        for (int y = 0; y < outputSize.y; y++)
        {
            for (int x = 0; x < outputSize.x; x++)
            {
                float e = entropyMap[x, y];
                if (e > 0 && e < lowest)
                {
                    lowest = e;
                    validCoords.Clear();
                    validCoords.Add(new Vector2Int(x, y));
                }
                else if (e == lowest)
                {
                    validCoords.Add(new Vector2Int(x, y));
                }
            }
        }

        if (validCoords.Count == 0)
        {
            Debug.Log("No valid coordinates left!");
            return nullCoords;
        }
        return validCoords[Random.Range(0, validCoords.Count - 1)];
    }

    #endregion
}
