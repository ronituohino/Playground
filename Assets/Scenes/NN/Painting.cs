using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Painting : MonoBehaviour
{
    public int gridSize;
    public float brushStrength;
    Image[] images;

    int side;

    public NeuralNetwork nn;
    NeuralNetwork.Results results;

    bool doingNeuralNetworkThings = false;
    bool ready = false;

    // Start is called before the first frame update
    void Start()
    {
        results = new NeuralNetwork.Results(-1, 0f);
        images = new Image[gridSize * gridSize];

        side = Mathf.RoundToInt(Screen.height / gridSize);
        Vector2 size = new Vector2(side, side);

        int i = 0;
        for(int y = 0; y < gridSize; y++)
        {
            for(int x = 0; x < gridSize; x++)
            {
                GameObject g = new GameObject(i.ToString(), new System.Type[] { typeof(Image) });
                g.transform.SetParent(transform);
                g.layer = 5;
                images[i] = g.GetComponent<Image>();

                RectTransform rt = g.GetComponent<RectTransform>();
                rt.sizeDelta = size;
                rt.anchoredPosition = new Vector2((-gridSize / 2 * side) + x * side, (-gridSize / 2 * side) + y * side + (side / 2));

                i++;
            }
        }
    }

    Vector3 lastPos = Vector3.zero;

    // Update is called once per frame
    void Update()
    {
        //Painting
        if(Input.GetMouseButton(0) && lastPos != Input.mousePosition)
        {
            float x = Input.mousePosition.x;
            float y = Input.mousePosition.y;

            float distFromSide = (Screen.width / 2f) - (side * (gridSize / 2)) - side / 2f;

            if (x > distFromSide && x < distFromSide + gridSize * side && y > 0 && y < Screen.height)
            {
                int xPos = Mathf.RoundToInt((x - distFromSide - side / 2f) / side);
                int yPos = Mathf.RoundToInt(y / side) - 1;

                if(yPos >= 0)
                {
                    Paint(xPos, yPos);
                }
            }
        }

        //Clear
        if (Input.GetKey(KeyCode.Space))
        {
            for(int y = 0; y < gridSize; y++)
            {
                for(int x  = 0; x < gridSize; x++)
                {
                    images[y * gridSize + x].color = Color.white;
                }
            }
        }

        //Call the neural network for answers!
        if (Input.GetKeyDown(KeyCode.E) && !doingNeuralNetworkThings)
        {
            doingNeuralNetworkThings = true;
            StopAllCoroutines();
            StartCoroutine(GetNumberOnNeuralNetwork());
        }
        //Write results!
        if(ready)
        {
            Debug.Log("Interpreted number: " + results.number + " --- At a certainty of: " + results.certainty * 100 + "%");
            ready = false;
        }

        lastPos = Input.mousePosition;
    }

    void Paint(int x, int y)
    {
        Color c;
        int index = y * gridSize + x;

        images[index].color = Color.black;

        if(y + 1 <= gridSize - 1)
        {
            index = (y + 1) * gridSize + x;
            c = images[index].color;
            images[index].color = c.Darken(brushStrength);
        }
        if (y - 1 >= 0)
        {
            index = (y - 1) * gridSize + x;
            c = images[index].color;
            images[index].color = c.Darken(brushStrength);
        }
        if (x + 1 <= gridSize)
        {
            index = y * gridSize + (x + 1);
            c = images[index].color;
            images[index].color = c.Darken(brushStrength);
        }
        if (y - 1 >= 0)
        {
            index = y * gridSize + (x - 1);
            c = images[index].color;
            images[index].color = c.Darken(brushStrength);
        }
    }

    //Hook to neural network
    IEnumerator GetNumberOnNeuralNetwork()
    {
        doingNeuralNetworkThings = false;
        results = nn.GetNumber(CompileTextureFromImages(images));
        ready = true;
        yield return null;
    }

    Texture2D CompileTextureFromImages(Image[] images)
    {
        Texture2D texture = new Texture2D(gridSize, gridSize);

        int len = images.Length;
        for(int i = 0; i < len; i++)
        {
            texture.SetPixel(i, Mathf.FloorToInt(i / gridSize), images[i].color);
        }

        return texture;
    }
}

public static class ExtensionMethods
{
    public static Color Darken(this Color c, float amount)
    {
        return new Color(c.r - amount, c.g - amount, c.b - amount);
    }

    public static void Save(this Texture2D t)
    {
        FileStream f = new FileStream(Application.dataPath + "/file.png", FileMode.Create);
        byte[] bytes = t.EncodeToPNG();
        f.Write(bytes, 0, bytes.Length);
    }
}
