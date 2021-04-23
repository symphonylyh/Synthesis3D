using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

// Code modified from: https://www.youtube.com/watch?v=-pmDRgp7ECY
public class MovementPath : ScriptableObject
{

    public enum PathTypes //Types of movement paths
    {
        oneway, // one-way
        linear, // back and forth
        loop // loop
    }

    public PathTypes PathType; //Indicates type of path (Oneway, Linear or Looping)
    public int movementDirection = 1; //1 clockwise/forward || -1 counter clockwise/backwards
    public int movingTo = 0; //used to identify point in PathSequence we are moving to
    public Transform[] PathSequence; //Array of all points in the path
    public Transform lookat;
    public bool pathEnds = false;

    public static MovementPath Init()
    {
        var p = ScriptableObject.CreateInstance<MovementPath>();
        return p;
    }

    //OnDrawGizmos will draw lines between our points in the Scene view
    //These lines will allow us to easily see the path that
    //our moving object will follow in the game
    // Icon list: https://unitylist.com/p/5c3/Unity-editor-icons
    public void drawGizmos(string icon)
    {
        //Make sure that your sequence has points in it
        //and that there are at least two points to constitute a path
        if( PathSequence == null || PathSequence.Length < 2)
            return;

        //Loop through all of the points in the sequence of points
        for (int i = 0; i < PathSequence.Length; i++)
            //Draw icon
            Gizmos.DrawIcon(PathSequence[i].position, icon, true);
        for(int i = 0; i < PathSequence.Length - 1; i++)
            //Draw a line between the points
            Gizmos.DrawLine(PathSequence[i].position, PathSequence[i+1].position);
         Gizmos.DrawLine(PathSequence[PathSequence.Length-1].position, PathSequence[0].position);
    }

    //GetNextPathPoint() returns the transform component of the next point in our path
    //FollowPath.cs script will inturn move the object it is on to that point in the game
    public IEnumerator<Transform> GetNextPathPoint()
    {
        //Make sure that your sequence has points in it
        //and that there are at least two points to constitute a path
        if (PathSequence == null || PathSequence.Length < 2)
            yield break; //Exits the Coroutine sequence length check fails

        while(true) //Does not infinite loop due to yield return!!
        {
            //Return the current point in PathSequence
            //and wait for next call of enumerator (Prevents infinite loop)
            yield return PathSequence[movingTo]; 
//*********************************PAUSES HERE******************************************************//
            //If there is only one point exit the coroutine
            if(PathSequence.Length == 1)
            {
                continue;
            }

            //If Linear path move from start to end then end to start then repeat
            if (PathType == PathTypes.linear)
            {
                //If you are at the begining of the path
                if (movingTo <= 0)
                {
                    movementDirection = 1; //Seting to 1 moves forward
                }
                //Else if you are at the end of your path
                else if (movingTo >= PathSequence.Length - 1)
                {
                    movementDirection = -1; //Seting to -1 moves backwards
                }
            }

            movingTo = movingTo + movementDirection;  
            //movementDirection should always be either 1 or -1
            //We add direction to the index to move us to the
            //next point in the sequence of points in our path

            if (PathType == PathTypes.oneway)
            {
                if (movingTo >= PathSequence.Length || movingTo < 0)
                {
                    pathEnds = true;
                    yield break;
                }
            }

            //For Looping path you must move the index when you reach 
            //the begining or end of the PathSequence to loop the path
            if(PathType == PathTypes.loop)
            {
                //If you just moved past the last point(moving forward)
                if (movingTo >= PathSequence.Length)
                {
                    //Set the next point to move to as the first point in sequence
                    movingTo = 0;
                }
                //If you just moved past the first point(moving backwards)
                if (movingTo < 0)
                {
                    //Set the next point to move to as the last point in sequence
                    movingTo = PathSequence.Length - 1;
                }
            }
        }
    }
}
