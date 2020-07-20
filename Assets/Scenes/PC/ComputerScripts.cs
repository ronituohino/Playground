using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Unity.Editor;
using TMPro;

public class ComputerScripts : MonoBehaviour
{
    //public TextMeshProUGUI codeText;
    //string code = "";

    DatabaseReference reference;

    Quaternion rotation = Quaternion.identity;
    public GameObject obj;

    // Start is called before the first frame update
    void Start()
    {
        //Generate instance code
        /*code = "";
        for(int i = 0; i < 6; i++)
        {
            code += Random.Range(0, 10).ToString();
        }
        codeText.text = "Code: " + code;*/

        // Set up the Editor before calling into the realtime database.
        FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://unity-testing-3e5fd.firebaseio.com/");

        // Get the root reference location of the database.
        reference = FirebaseDatabase.DefaultInstance.RootReference;

        SetupServer();
    }

    void SetupServer()
    {
        //Query q = reference.Child("Connections").OrderByValue(); //Get all values
        reference.Child("Connections").Child("123456").ValueChanged += UpdateFetch;
    }


    //(IEnumerator GetValues()
    //{

    //}


    void UpdateFetch(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        Rotation rot = JsonUtility.FromJson<Rotation>(args.Snapshot.GetRawJsonValue());
        rotation = new Quaternion(rot.x, rot.y, rot.z, rot.w);

        obj.transform.rotation = rotation;
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
