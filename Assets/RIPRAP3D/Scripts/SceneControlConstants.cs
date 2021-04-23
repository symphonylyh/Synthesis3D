using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneControlConstants : ScriptableObject
{
    #region Fields
    public enum ControlTypes
    {
        manual, // use UI buttons
        auto // automatic
    }

    public ControlTypes control_type;

    // ground scale
    public float canvas_width, canvas_height; 
    
    // number of multiview cameras
    public int camera_num; 
    public float camera_height;
    public Vector3 camera_init_pos;
    public Quaternion camera_init_rot;

    // camera lookat point
    public Transform camera_lookat;

    // camera movement switch
    public bool camera_move;

    // time delay before camera starts moving (after the rocks settle down)
    public float camera_move_delay;

    public float camera_speed; 
    // How close does it have to be to the point to be considered at that point
    public float camera_reach_threshold = .1f; 
    
    // camera snapshot switch
    public bool camera_save_snapshot;
    public int camera_snapshot_width;
    public int camera_snapshot_height;

    // ground plane
    public Vector3 ground_pos;
    public Vector3 ground_scale;

    // rock spacing density along the XZ plane
    public int density_x;
    public int density_z;
    public float spacing_y;
    // number of rock layers along the Y direction
    public int minLayers;
    public int maxLayers;
    public float rock_mass;
    public bool rock_motion;

    public string save_path;

    #endregion

    public static SceneControlConstants Create()
    {
        var p = ScriptableObject.CreateInstance<SceneControlConstants>();
        return p;
    }

    public void Init()
    {
        control_type = ControlTypes.manual;

        canvas_width = 100;
        canvas_height = 100;
        
        camera_num = 36;
        camera_height = 5;
        camera_init_pos = new Vector3(canvas_width/2, 50, canvas_height/2);
        camera_init_rot = Quaternion.Euler(90, 0, 0);
        camera_lookat = new GameObject("Camera LookAt").transform;
        camera_lookat.position = new Vector3(canvas_width/2, 0, canvas_height/2);
        camera_move = true;
        switch (control_type)
        {
        case ControlTypes.manual:
            camera_move_delay = 0;
            break;
        case ControlTypes.auto:
            camera_move_delay = 5;
            break;
        }
        camera_speed = 10;
        camera_reach_threshold = .1f;

        camera_save_snapshot = true;
        camera_snapshot_width = 1024;
        camera_snapshot_height = 768;

        ground_pos = new Vector3(this.canvas_width/2,0,this.canvas_height/2); // Y is typically upward
        ground_scale = new Vector3(this.canvas_width/10, this.canvas_width/10, this.canvas_width/10); // original scale 10x10

        density_x = 8; //2;
        density_z = 8; //2;
        spacing_y = 5;
        minLayers = 20;//1;
        maxLayers = 25;//3;
        rock_mass = 1f;
        rock_motion = true;

        save_path = Directory.GetParent(Application.dataPath) + "/SyntheticData/"; // same level as /Assets
    }

}
