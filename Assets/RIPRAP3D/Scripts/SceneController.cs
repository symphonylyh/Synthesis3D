using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

// Scene control:
// - Scene will freeze at the beginning. Press space key or the "Start/Stop Scene" button to pause/resume the scene.
// - "Multiview Camera" button to move the camera along preset trajectory
// - "Save Point Cloud" button to save the point cloud to disk
public class SceneController : MonoBehaviour
{
    // Program constants
    public SceneControlConstants Consts;
    
    // Display different synthetic information
    public ImageSynthesis synthesis;

    // Camera movement
    public MovementPath cameraPath;

    // Prefabs
    public GameObject[] prefabs;
    private ObjectPool pool;

    // Scene counter for automated scene generation (not yet)
    public int sceneID = 0;
    public string filePath;

    public float timer = 0;

    // Awake is called before Start
    void Awake()
    {
        // Initialize scene control constants
        Consts = SceneControlConstants.Create();
        Consts.Init();

        // Link UI buttons with callbacks
        SetUIButtons();

        // Load prefabs from disk
        LoadPrefabs();
        this.pool = ObjectPool.Init(this.prefabs);

        // Prepare output data path
        filePath = Consts.save_path + "Stockpile_" + sceneID.ToString().PadLeft(3,'0');
        if (!Directory.Exists(filePath))
            Directory.CreateDirectory(filePath);   
    }

    #region Button Setup
    void SetUIButtons()
    {
        switch (Consts.control_type)
        {
        case SceneControlConstants.ControlTypes.manual:
            GameObject.Find("Button_StartStopScene").GetComponent<Button>().onClick.AddListener(StartStopScene);
            GameObject.Find("Button_MultiviewCamera").GetComponent<Button>().onClick.AddListener(MultiviewCamera);
            GameObject.Find("Button_SavePointCloud").GetComponent<Button>().onClick.AddListener(SavePointCloud);
            break;
        case SceneControlConstants.ControlTypes.auto:
            GameObject.Find("Button_StartStopScene").SetActive(false);
            GameObject.Find("Button_MultiviewCamera").SetActive(false);
            GameObject.Find("Button_SavePointCloud").SetActive(false);
            break;
        }
        
    }
    private void StartStopScene()
    {
        if (Time.timeScale == 1)
            Time.timeScale = 0; // pause
        else 
            Time.timeScale = 1; // unpause
    }
    private void MultiviewCamera()
    {
        var cam = Camera.main;
        cam.gameObject.AddComponent<CameraMovement>();
        cam.gameObject.GetComponent<CameraMovement>().Start();
    }
    private void SavePointCloud()
    {
        Debug.Log("Save Point Cloud!");
    }
    #endregion

    #region Resource Load
    // Load all prefabs under the default "/Assets/Resources/Prefabs" path
    void LoadPrefabs()
    {
        this.prefabs = Resources.LoadAll<GameObject>("Prefabs"); 
        
        foreach (var prefab in this.prefabs)
        {
            var rb = prefab.GetComponent<Rigidbody>();
            rb.mass = Consts.rock_mass;
            rb.useGravity = true;
            if (!Consts.rock_motion)
                rb.isKinematic = true; // set kinematic will freeze the object. Kinematic means the motion is controlled by script instead physics engine
                rb.detectCollisions = false;
        }
    }
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        // Set camera motion, lighting, ground plane
        CreatePlane();
        SetCameraAndLight();

        // Instantiate riprap rocks
        GenerateRandom(Consts.density_x, Consts.density_z, Consts.minLayers, Consts.maxLayers);

        Time.timeScale = 0; // pause the scene at start
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        // In Game view, press Space to pause/unpause
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Time.timeScale == 1)
                Time.timeScale = 0; // pause
            else 
                Time.timeScale = 1; // unpause
        }
        if (Time.timeScale == 0)
            return;
        
        RaycastHit hit;
        Ray r = new Ray(Consts.camera_init_pos, Vector3.down);
        if (Physics.Raycast(r, out hit))
        {
            int label = int.Parse(hit.collider.name); // rock entity ID
            Vector3 point = hit.point;
            float distance = hit.distance;
            Vector3 normal = hit.normal;
            Renderer renderer = hit.collider.gameObject.GetComponentInChildren<MeshRenderer>();
            Texture2D texture2D = renderer.material.mainTexture as Texture2D;
            Vector2 pCoord = hit.textureCoord;
            Vector2 tiling = renderer.material.mainTextureScale;
            
            pCoord.x *= texture2D.width;
            pCoord.y *= texture2D.height;
            Color color = texture2D.GetPixel(Mathf.FloorToInt(pCoord.x * tiling.x) , Mathf.FloorToInt(pCoord.y * tiling.y));
            Debug.Log(color);
            Debug.Log(point);
            Debug.Log(point.y);
            Debug.Log(label);
            Debug.DrawRay(Consts.camera_init_pos, Vector3.down*50);
        }
        synthesis.OnSceneChange();
    }

    void OnDrawGizmos()
    {
        this.cameraPath.drawGizmos("Camera Gizmo"); 
    }

    void CreatePlane()
    {
        // plane size
        GameObject plane  = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "Ground Plane";
        plane.transform.position = Consts.ground_pos; 
        plane.transform.localScale = Consts.ground_scale;

        // plane color
        plane.GetComponent<Renderer>().material = Resources.Load<Material>("Materials/Ground"); 
    }

    void SetCameraAndLight()
    {
        // Camera
        var cam = Camera.main;
        cam.transform.position = Consts.camera_init_pos;
        cam.transform.rotation = Consts.camera_init_rot;
        
        SetCameraTrajectory();

        // auto control mode; if manual mode, enabled by clicking the button
        if (Consts.control_type == SceneControlConstants.ControlTypes.auto && Consts.camera_move)
            cam.gameObject.AddComponent<CameraMovement>();

        // Light
        var light = GameObject.Find("Directional Light");
        light.transform.position = Consts.camera_init_pos;
        light.transform.rotation = Consts.camera_init_rot;
    }

    void SetCameraTrajectory()
    {
        // Camera trajectory
        // Option 1: Create an empty gameobject, and attach the script (MovementPath.cs) to it
        // GameObject cam_traj = new GameObject("CameraTrajectory");
        // cam_traj.AddComponent<MovementPath>();
        // Option 2: Use scriptable object. This is better, no need to attach to an empty obj

        float center_x = Consts.canvas_width/2, center_z = Consts.canvas_height/2;
        
        // camera ring of radius R at a specific height H 
        float R = Consts.canvas_width/2, H = Consts.camera_height;
        int N = Consts.camera_num;
        float delta_theta = 2 * Mathf.PI / N;
        GameObject cam_traj = new GameObject("Camera Trajectory");
        this.cameraPath = MovementPath.Init();
        this.cameraPath.PathType = MovementPath.PathTypes.oneway;
        this.cameraPath.PathSequence = new Transform[N];
        this.cameraPath.lookat = Consts.camera_lookat;

        for (int i = 0; i < N; i++)
        {
            GameObject waypoint = new GameObject("cam_" + i.ToString().PadLeft(3,'0')); // empty object
            waypoint.transform.SetParent(cam_traj.transform); // set as child
            float theta = i * delta_theta;
            waypoint.transform.position = new Vector3(center_x + R * Mathf.Cos(theta), H, center_z + R * Mathf.Sin(theta));
            this.cameraPath.PathSequence[i] = waypoint.transform;
        }
       
    }

    // Randomly generate objects in the scene
    void GenerateRandom(
        int density_x, int density_z, 
        int minLayers, int maxLayers)
    {
        GameObject stockpile = new GameObject("Stockpile");

        float width = Consts.canvas_width, height = Consts.canvas_height;

        this.pool.ReclaimAll();
        int layers = Random.Range(minLayers, maxLayers); 

        int ID = 0;
        for (int i = 0; i < layers; i++)
        {
            // (H/4, W/4) to (3H/4, 3W/4) with density
            for (float x = width/4; x <= 3*width/4; x += width/2/density_x)
            {
                for (float z = height/4; z <= 3*height/4; z += height/2/density_z)
                {
                    float y = i * Consts.spacing_y + 1;
                    // randomly pick a prefab & instantiate
                    int prefabID = Random.Range(0, prefabs.Length);
                    var instance = this.pool.CreateObject(prefabID);
                    var newObj = instance.obj;
                    newObj.name = ID.ToString().PadLeft(4,'0');
                    ID++;

                    // set parent
                    newObj.transform.SetParent(stockpile.transform);

                    // set position
                    Vector3 newPos = new Vector3(x, y, z);
                    newObj.transform.position = newPos;

                    // set rotation
                    var newRot = Random.rotation;
                    newObj.transform.rotation = newRot;

                    // set scale
                    float sx = Random.Range(0.5f, 1f);
                    Vector3 newScale = new Vector3(sx, sx, sx);
                    newObj.transform.localScale = newScale;

                    // set color
                    // float newR, newG, newB;
                    // newR = Random.Range(0.0f, 1.0f);
                    // newG = Random.Range(0.0f, 1.0f);
                    // newB = Random.Range(0.0f, 1.0f);
                    // var newColor = new Color(newR, newG, newB);
                    // newObj.GetComponent<Renderer>().material.color = newColor;

                }
            }
        }
    }
}
