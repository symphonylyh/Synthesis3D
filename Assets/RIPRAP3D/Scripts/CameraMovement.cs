using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

// Code modified from FollowPath.cs: https://www.youtube.com/watch?v=-pmDRgp7ECY
public class CameraMovement : MonoBehaviour
{
    public enum MovementType  //Type of Movement
    {
        MoveTowards,
        LerpTowards
    }

    public SceneController sc; // handler to SceneController class
    public SceneControlConstants Consts; // handler to constants

    public MovementType Type = MovementType.MoveTowards; // Movement type used
    public MovementPath MyPath; // Reference to Movement Path Used
    
    private IEnumerator<Transform> pointInPath; //Used to reference points returned from MyPath.GetNextPathPoint

    public void Start()
    {
        // Get the MovementPath scriptable object maintined in SceneControl class
        sc = GameObject.Find("SceneControl").GetComponent<SceneController>();
        Consts = sc.Consts;
        MyPath = sc.cameraPath;

        //Make sure there is a path assigned
        if (MyPath == null)
        {
            Debug.LogError("Movement Path cannot be null, I must have a path to follow.", gameObject);
            return;
        }

        //Sets up a reference to an instance of the coroutine GetNextPathPoint
        pointInPath = MyPath.GetNextPathPoint();
        //Get the next point in the path to move to (Gets the Default 1st value)
        pointInPath.MoveNext();

        //Make sure there is a point to move to
        if (pointInPath.Current == null)
        {
            Debug.LogError("A path must have points in it to follow", gameObject);
            return; //Exit Start() if there is no point to move to
        }

        //Set the position of this object to the position of our starting point
        transform.position = pointInPath.Current.position;
        transform.LookAt(MyPath.lookat);
    }
     
    //Update is called by Unity every frame
    public void Update()
    {
        if (sc.timer < Consts.camera_move_delay)
            return;

        //Validate there is a path with a point in it
        if (pointInPath == null || pointInPath.Current == null || MyPath.pathEnds)
        {
            Debug.Log("Path ends");
            return; //Exit if no path is found
        }

        // Time.deltaTime is the elasped time between previous & current frame (in seconds), Speed is # of units per second
        if (Type == MovementType.MoveTowards) //If you are using MoveTowards movement type
        {
            //Move to the next point in path using MoveTowards
            transform.position =
                Vector3.MoveTowards(transform.position,
                                    pointInPath.Current.position,
                                    Time.deltaTime * Consts.camera_speed);
        }
        else if (Type == MovementType.LerpTowards) //If you are using LerpTowards movement type
        {
            //Move towards the next point in path using Lerp
            transform.position = 
                Vector3.Lerp(transform.position,
                             pointInPath.Current.position,
                             Time.deltaTime * Consts.camera_speed);
        }

        //set lookat
        transform.LookAt(MyPath.lookat);

        //Check to see if you are close enough to the next point to start moving to the following one
        //Using Pythagorean Theorem
        //per unity squaring a number is faster than the square root of a number
        //Using .sqrMagnitude 
        var distanceSquared = (transform.position - pointInPath.Current.position).sqrMagnitude;
        if (distanceSquared < Consts.camera_reach_threshold * Consts.camera_reach_threshold) //If you are close enough
        {
            Debug.Log("At " + pointInPath.Current.name);
            if (Consts.camera_save_snapshot) {
                string dirname = sc.filePath;
                string filename = pointInPath.Current.name;
                sc.synthesis.Save(filename, Consts.camera_snapshot_width, Consts.camera_snapshot_height, dirname, 0);
            }
            pointInPath.MoveNext(); //Get next point in MovementPath
            if (MyPath.pathEnds)
            {   // when camera path finishes, prepare the info for next scene
                sc.sceneID += 1;
                string dirname = Consts.save_path + "Stockpile_" + sc.sceneID.ToString().PadLeft(3,'0'); 
                if (!Directory.Exists(dirname))
                    Directory.CreateDirectory(dirname);
            }
        }
        //The version below uses Vector3.Distance same as Vector3.Magnitude which includes (square root)
        /*
        var distanceSquared = Vector3.Distance(transform.position, pointInPath.Current.position);
        if (distanceSquared < Consts.camera_reach_threshold) //If you are close enough
        {
            pointInPath.MoveNext(); //Get next point in MovementPath
        }
        */

    }
}
