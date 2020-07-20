using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Unity.Editor;
using TMPro;

public class PhoneScripts : MonoBehaviour
{
    Gyroscope gyroscope;
    Quaternion rotation;

    DatabaseReference reference;

    //public TextMeshProUGUI codeText;
    //bool connected = false;

    //string code;

    // Start is called before the first frame update
    void Start()
    {
        gyroscope = Input.gyro;
        gyroscope.enabled = true;
        gyroscope.updateInterval = 0.01f;

        // Set up the Editor before calling into the realtime database.
        FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://unity-testing-3e5fd.firebaseio.com/");

        // Get the root reference location of the database.
        reference = FirebaseDatabase.DefaultInstance.RootReference;
    }

    // Update is called once per frame
    void Update()
    {
        rotation = gyroscope.attitude;
        Rotation r = new Rotation(rotation.w, rotation.x, rotation.y, rotation.z);
        //Rotation r = new Rotation(0.1f, -1.2f, 3.1f, -0.1f);
        string jsonRotation = JsonUtility.ToJson(r);

        reference.Child("Connections").Child("123456").SetRawJsonValueAsync(jsonRotation);
    }

    struct Rotation
    {
        public float w;
        public float x;
        public float y;
        public float z;

        public Rotation(float w, float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }
}
