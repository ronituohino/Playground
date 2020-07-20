using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

public class DecodeUbyte : MonoBehaviour
{
    public bool trainingData;

    byte[] imageData;
    byte[] labelData;

    // Start is called before the first frame update
    void Start()
    {
        //Image
        try
        {
            using (FileStream fsSource = new FileStream(Environment.CurrentDirectory + "\\Assets\\Scenes\\NN\\" + (trainingData ? "train-images.idx3-ubyte" : "t10k-images.idx3-ubyte") , FileMode.Open, FileAccess.Read))
            {

                // Read the source file into a byte array.
                byte[] bytes = new byte[fsSource.Length];
                int numBytesToRead = (int)fsSource.Length;
                int numBytesRead = 0;
                while (numBytesToRead > 0)
                {
                    // Read may return anything from 0 to numBytesToRead.
                    int n = fsSource.Read(bytes, numBytesRead, numBytesToRead);

                    // Break when the end of the file is reached.
                    if (n == 0)
                        break;

                    numBytesRead += n;
                    numBytesToRead -= n;
                }
                numBytesToRead = bytes.Length;

                imageData = bytes;
            }
        }
        catch (FileNotFoundException ioEx)
        {
            Console.WriteLine(ioEx.Message);
        }

        //Label
        try
        {
            using (FileStream fsSource = new FileStream(Environment.CurrentDirectory + "\\Assets\\Scenes\\NN\\" + (trainingData ? "train-labels.idx1-ubyte" : "t10k-labels.idx1-ubyte"), FileMode.Open, FileAccess.Read))
            {
                // Read the source file into a byte array.
                byte[] bytes = new byte[fsSource.Length];
                int numBytesToRead = (int)fsSource.Length;
                int numBytesRead = 0;
                while (numBytesToRead > 0)
                {
                    // Read may return anything from 0 to numBytesToRead.
                    int n = fsSource.Read(bytes, numBytesRead, numBytesToRead);

                    // Break when the end of the file is reached.
                    if (n == 0)
                        break;

                    numBytesRead += n;
                    numBytesToRead -= n;
                }
                numBytesToRead = bytes.Length;

                labelData = bytes;
            }
        }
        catch (FileNotFoundException ioEx)
        {
            Console.WriteLine(ioEx.Message);
        }

        //Save images
        int imageSize = 28 * 28;

        int len = (trainingData ? 60000 : 10000);
        for (int i = 0; i < len; i++)
        {
            Texture2D img = new Texture2D(28, 28);
            for (int y = 0; y < 28; y++)
            {
                for (int x = 0; x < 28; x++)
                {
                    float value = 1f - NeuralNetwork.Map((int)imageData[16 + x + y * 28 + i * imageSize], 0, 255, 0f, 1f);
                    img.SetPixel(x, 28 - y, new Color(value, value, value));
                }
            }

            SaveTextureToFile(img, (i.ToString() + ".png"));
        }

        //Make a text file of labels, no need to seperate values!
        using (FileStream f = new FileStream(Application.dataPath + "/Scenes/NN/Training Data/labels.txt", FileMode.Create))
        {
            for (int i = 0; i < len; i++)
            {
                byte[] data = Encoding.UTF8.GetBytes(((int)labelData[8+i]).ToString());
                f.Write(data, 0, data.Length);
            }
        }
    }

    void SaveTextureToFile(Texture2D texture, string fileName)
    {
        byte[] bytes = texture.EncodeToPNG();
        FileStream file = File.Open(Application.dataPath + "/Scenes/NN/Training Data/" + fileName, FileMode.Create);
        var binary = new BinaryWriter(file);
        binary.Write(bytes);
        file.Close();
    }
}
