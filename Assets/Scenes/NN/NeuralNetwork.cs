using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;

public class NeuralNetwork : MonoBehaviour
{
    public struct Layer
    {
        public Neuron[] neurons;

        public Layer(Neuron[] neurons)
        {
            this.neurons = neurons;
        }
    }

    public struct Neuron
    {
        public float value;
        public float[] weights; //Holds the weigths connecting this to the previous neurons

        public float error;
        public float total;

        public Neuron(float value, float[] weights, float error, float total)
        {
            this.value = value;
            this.weights = weights;
            this.error = error;
            this.total = total;
        }
    }

    public struct Results
    {
        public int number;
        public float certainty;

        public Results(int number, float certainty)
        {
            this.number = number;
            this.certainty = certainty;
        }

        public static readonly Results empty = new Results(-1, 0f);
    }

    Layer hiddenLayer1;
    public int layer1NeuronAmount;
    Layer hiddenLayer2;
    public int layer2NeuronAmount;

    Layer outputLayer;

    public Painting p;

    float[] inputs;
    int inputAmount;

    public TextMeshProUGUI text;

    const float Neper = 2.718281f;

    public bool randomize = false;
    float[] weightValues;

    public bool trainNetwork = false;
    public int imageAmount = 1000;
    float[] trainingValues;
    int[] labels;
    int trainingImageIterator = 0;

    public float adjustingVariable = 0.1f;

    //Randomize to, or load weights from, weigths.txt file
    private void Start()
    {
        inputAmount = p.gridSize * p.gridSize;

        //Load weights from file if it exists
        weightValues = LoadWeights();

        if (randomize)
        {
            weightValues = new float[layer1NeuronAmount * inputAmount + layer2NeuronAmount * layer1NeuronAmount + 10 * layer2NeuronAmount];
        }

        int lineIndex = 0;

        //Layer 1
        Neuron[] layer1Neurons = new Neuron[layer1NeuronAmount];
        for (int i = 0; i < layer1NeuronAmount; i++)
        {
            float[] weights = new float[inputAmount];
            for (int w = 0; w < inputAmount; w++)
            {
                if (randomize)
                {
                    weightValues[lineIndex] = UnityEngine.Random.Range(0f, 1f);
                }

                weights[w] = weightValues[lineIndex];
                lineIndex++;
            }

            layer1Neurons[i] = new Neuron(0f, weights, 0f, 0f);
        }
        hiddenLayer1 = new Layer(layer1Neurons);

        //Layer 2
        Neuron[] layer2Neurons = new Neuron[layer2NeuronAmount];
        for (int i = 0; i < layer2NeuronAmount; i++)
        {
            float[] weights = new float[layer1NeuronAmount];
            for (int w = 0; w < layer1NeuronAmount; w++)
            {
                if (randomize)
                {
                    weightValues[lineIndex] = UnityEngine.Random.Range(0f, 1f);
                }

                weights[w] = weightValues[lineIndex];
                lineIndex++;
            }

            layer2Neurons[i] = new Neuron(0f, weights, 0f, 0f);
        }
        hiddenLayer2 = new Layer(layer2Neurons);

        //Output
        Neuron[] output = new Neuron[10];
        for (int i = 0; i < 10; i++)
        {
            float[] weights = new float[layer2NeuronAmount];
            for (int w = 0; w < layer2NeuronAmount; w++)
            {
                if (randomize)
                {
                    weightValues[lineIndex] = UnityEngine.Random.Range(0f, 1f);
                }

                weights[w] = weightValues[lineIndex];
                lineIndex++;
            }

            output[i] = new Neuron(0f, weights, 0f, 0f);
        }
        outputLayer = new Layer(output);

        if (randomize)
        {
            SaveWeights(false);
        }

        if (trainNetwork)
        {
            trainingValues = new float[10 * imageAmount]; //Output * imageAmount
            labels = new int[imageAmount];

            //Load labels
            string path = Application.dataPath + "/Scenes/NN/Training Data/labels.txt";
            string text = File.ReadAllText(path);

            for (int i = 0; i < imageAmount; i++)
            {
                labels[i] = int.Parse(text[i].ToString());
            }

            StartCoroutine(TrainNetwork());
        }
    }



    //Weight IO
    void SaveWeights(bool updateList)
    {
        int len = weightValues.Length;
        string[] weights = new string[len];

        if (updateList)
        {
            int lineIndex = 0;

            //Layer 1
            for (int i = 0; i < layer1NeuronAmount; i++)
            {
                for (int w = 0; w < inputAmount; w++)
                {
                    weightValues[lineIndex] = hiddenLayer1.neurons[i].weights[w];
                    lineIndex++;
                }
            }

            //Layer 2
            for (int i = 0; i < layer2NeuronAmount; i++)
            {
                for (int w = 0; w < layer1NeuronAmount; w++)
                {
                    weightValues[lineIndex] = hiddenLayer2.neurons[i].weights[w];
                    lineIndex++;
                }
            }

            //Output
            for (int i = 0; i < 10; i++)
            {
                for (int w = 0; w < layer2NeuronAmount; w++)
                {
                    weightValues[lineIndex] = outputLayer.neurons[i].weights[w];
                    lineIndex++;
                }
            }
        }

        for (int i = 0; i < len; i++)
        {
            weights[i] = weightValues[i].ToString();
        }


        File.WriteAllLines(Application.dataPath + "/Scenes/NN/Training Results/settings.txt", weights);
    }

    float[] LoadWeights()
    {
        string[] fileStrings = new string[0];
        float[] weights = new float[0];

        bool exists = File.Exists(Application.dataPath + "/Scenes/NN/Training Results/settings.txt");
        if (!randomize && exists)
        {
            fileStrings = File.ReadAllLines(Application.dataPath + "/Scenes/NN/Training Results/settings.txt");

            int len = fileStrings.Length;
            weights = new float[len];

            for (int i = 0; i < len; i++)
            {
                weights[i] = float.Parse(fileStrings[i]);
            }
        }

        return weights;
    }



    //Training
    IEnumerator TrainNetwork()
    {
        //Process the images through the algorithm
        float overAllPerformance = 0f;
        for (int i = 0; i < imageAmount; i++)
        {
            Texture2D texture = new Texture2D(2, 2);
            string path = Application.dataPath + "/Scenes/NN/Training Data/" + i.ToString() + ".png";

            using (FileStream f = new FileStream(path, FileMode.Open))
            {
                FileInfo info = new FileInfo(path);
                byte[] data = new byte[info.Length];
                f.Read(data, 0, data.Length);
                texture.LoadImage(data);
            };

            //Process the image through the network, now neurons hold these values
            GetNumber(texture);

            //Evaluate and improve
            float[] errors = new float[10];

            for (int r = 0; r < 10; r++)
            {
                errors[r] = Error(trainingValues[r + i * 10], labels[i], r);
                overAllPerformance += errors[r];
            }

            Backpropagate(errors);

            //Debug.Log(overAllPerformance / i);

            text.text = "--Training in progress-- \n" + (i + 1).ToString() + "/" + imageAmount.ToString();
            yield return null;
        }
        SaveWeights(true);

        trainingImageIterator = 0;
    }

    void Backpropagate(float[] errors)
    {
        //Calculate layer 2 errors
        for (int n = 0; n < layer2NeuronAmount; n++)
        {
            float neuronError = 0f;
            for (int w = 0; w < 10; w++)
            {
                neuronError += outputLayer.neurons[w].weights[n] * errors[w];
            }

            hiddenLayer2.neurons[n].error = neuronError;
        }

        //Calculate layer 1 errors
        for (int n = 0; n < layer1NeuronAmount; n++)
        {
            float neuronError = 0f;
            for (int w = 0; w < layer2NeuronAmount; w++)
            {
                neuronError += hiddenLayer2.neurons[w].weights[n] * hiddenLayer2.neurons[w].error;
            }

            hiddenLayer1.neurons[n].error = neuronError;
        }



        //Adjust weights
        //Layer 1
        for (int i = 0; i < inputAmount; i++)
        {
            for (int n = 0; n < layer1NeuronAmount; n++)
            {
                Neuron neuron = hiddenLayer1.neurons[n];
                neuron.weights[i] += adjustingVariable * neuron.error * SigmoidDx(neuron.total) * inputs[i];
            }
        }

        //Layer 2
        for (int i = 0; i < layer1NeuronAmount; i++)
        {
            for (int n = 0; n < layer2NeuronAmount; n++)
            {
                Neuron neuron = hiddenLayer2.neurons[n];
                neuron.weights[i] += adjustingVariable * neuron.error * SigmoidDx(neuron.total) * hiddenLayer1.neurons[i].value;
            }
        }

        //Output
        for (int i = 0; i < layer2NeuronAmount; i++)
        {
            for (int n = 0; n < 10; n++)
            {
                Neuron neuron = outputLayer.neurons[n];
                neuron.weights[i] += adjustingVariable * errors[n] * SigmoidDx(neuron.total) * hiddenLayer2.neurons[i].value;
            }
        }
    }



    //Main function
    public Results GetNumber(Texture2D image)
    {
        //Read values
        inputs = new float[inputAmount];

        for (int y = 0; y < p.gridSize; y++)
        {
            for (int x = 0; x < p.gridSize; x++)
            {
                inputs[x + y * p.gridSize] = image.GetPixel(x,  p.gridSize - y).grayscale;
            }
        }

        //DisplayValues(inputs);

        //Process hidden layers
        //Layer 1
        for (int i = 0; i < layer1NeuronAmount; i++)
        {
            float total = 0f;
            for (int w = 0; w < inputAmount; w++)
            {
                total += inputs[w] * hiddenLayer1.neurons[i].weights[w];
            }
            hiddenLayer1.neurons[i].value = Sigmoid(total) + 1; //+1 being the bias
            hiddenLayer1.neurons[i].total = total;
        }
        //Layer 2
        for (int i = 0; i < layer2NeuronAmount; i++)
        {
            float total = 0f;
            for (int w = 0; w < layer1NeuronAmount; w++)
            {
                total += hiddenLayer1.neurons[w].value * hiddenLayer2.neurons[i].weights[w];
            }
            hiddenLayer2.neurons[i].value = Sigmoid(total) + 1;
            hiddenLayer2.neurons[i].total = total;
        }
        //Output
        for (int i = 0; i < 10; i++)
        {
            float total = 0f;
            for (int w = 0; w < layer2NeuronAmount; w++)
            {
                total += hiddenLayer2.neurons[w].value * outputLayer.neurons[i].weights[w];
            }
            outputLayer.neurons[i].value = Sigmoid(total) + 1;
            outputLayer.neurons[i].total = total;
        }


        if (trainNetwork) //Read outputs, calculate errors and adjust weights
        {
            for (int i = 0; i < 10; i++)
            {
                trainingValues[i + (trainingImageIterator * 10)] = outputLayer.neurons[i].value;
            }

            trainingImageIterator++;
            return Results.empty;
        }
        else //Return 1 value
        {
            //Read final layer results
            float certainty = 0f;
            int finalNeuron = -1;

            for (int i = 0; i < 10; i++)
            {
                //Debug.Log(outputLayer.neurons[i].value);
                if (outputLayer.neurons[i].value > certainty)
                {
                    certainty = outputLayer.neurons[i].value;
                    finalNeuron = i;
                }
            }

            return new Results(finalNeuron, certainty);
        }
    }

    void DisplayValues(float[] vals)
    {
        Debug.Log(vals.Length +" " + inputAmount);
        FileStream fs = File.Create(Application.dataPath + "/imagetext.txt");
        StreamWriter sw = new StreamWriter(fs);

        for(int i = 0; i < inputAmount; i++)
        {
            if (i > 0 && i % p.gridSize == 0)
            {
                sw.Write("\n");
            }

            float val = vals[i];
            Debug.Log(i + " " + val);
            if (val == 1f)
            {
                sw.Write('1');
            } else
            {
                sw.Write('0');
            }
        }

        sw.Close();
        fs.Close();
    }



    //Functions and their derivates to remap calculated neuron values to 0 - 1
    float ReLU(float v)
    {
        if (v < 0)
        {
            return 0f;
        }
        else
        {
            return v;
        }
    }

    float ReLUDx(float v)
    {
        if (v < 0)
        {
            return 0f;
        }
        else
        {
            return 1f;
        }
    }



    float Sigmoid(float v)
    {
        return 1f / (1 + Mathf.Pow(Neper, -v));
    }

    float SigmoidDx(float v)
    {
        return Sigmoid(v) * (1 - Sigmoid(v));
    }




    //Calculate the differences between outputs and the ideal values we are looking for (error, 0 is best)
    float Error(float output, int correctNumber, int outputNeuron)
    {
        //return Mathf.Pow(output - (correctNumber == outputNeuron ? 1f : 0f), 2);
        return (correctNumber == outputNeuron ? 1f : 0f) - output;
    }

    public static float Map(float s, float a1, float a2, float b1, float b2)
    {
        return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
    }
}
