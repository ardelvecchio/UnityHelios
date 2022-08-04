 
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Obj;
using System.Globalization;
using System.Collections.Generic;
using TMPro;
using System.IO;
using System.Linq;
using Pcx;
using Application = UnityEngine.Application;
using Button = UnityEngine.UI.Button;
using SimpleFileBrowser;
using Unity.VisualScripting;

public class Executable : MonoBehaviour {
    //these also need to be attached to a game object
    [SerializeField] InputField pathInputField;
    [SerializeField] InputField scaleInputField;
    [SerializeField] Text status;
    private List<GameObject> loaded = new List<GameObject>();
    private List<Pcx.PointCloudRenderer> pointCloud = new List<PointCloudRenderer>();
    private Vector3 rotation;
    private string[] minvalues;
    private string[] maxvalues;
    [SerializeField] Slider axisSlider;
    [SerializeField] private TMP_Text axistText;
    [SerializeField] Slider slider;
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private GameObject panel;
    [SerializeField] private Button quit;
    [SerializeField] private Button menu;
    [SerializeField] private TMP_InputField minvalue;
    [SerializeField] private TMP_InputField maxvalue;
    [SerializeField] private Button applyScale;
    [SerializeField] private GameObject range;
    [SerializeField] private RawImage gradient;
    [SerializeField] private Slider pointSize;

    private PointCloudRenderer pclRenderer;
    private string activeModel;
    private GameObject model;
    //[SerializeField] private TMP_Text pointValue;
    
    /*
    async public void Load()
    {
        status.text = "Loading new model...";
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        //string path = pathInputField.text;
        string path = "C:/Users/Alden/Documents/Helios/samples/radiation_selftest/build/walnut.obj";
        float scale = float.Parse(scaleInputField.text, CultureInfo.InvariantCulture);

        // This line is all you need to load a model from file. Synchronous loading is also available with ObjParser.Parse()
        var model = await ObjParser.ParseAsync(path, scale);

        stopwatch.Stop();
        status.text = $"Model loaded in {stopwatch.Elapsed}";

        if (model != null)
        {
            loaded.Add(model);
            var combinedBounds = BoundsUtils.CalculateCombinedBounds(model);
            Camera.main.transform.position = combinedBounds.center + Vector3.back * combinedBounds.size.magnitude;
        }
    }
    */
    
    //Most important function in the project. Chooses which type of model to load and calls all those functions which
    //make it happen.
    private void Load(string path, string extension)
    {
        
        Clear();
        rotation = new Vector3();
        axisSlider.value = 0;
        axistText.text = "X";
        status.text = "Loading new model...";
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        //set panel to false doesn't make it dissapear for some reason, so we make it infinitessimally small
        panel.gameObject.transform.localScale = new Vector3(0,0,0);
        float scale = float.Parse(scaleInputField.text, CultureInfo.InvariantCulture);
        quit.gameObject.SetActive(true);
        menu.gameObject.SetActive(true);
        axisSlider.gameObject.SetActive(true);
        
        if (extension == ".obj")
        {
            // This line is all you need to load a model from file. Synchronous loading is also available with ObjParser.Parse()
            model = ObjParser.Parse(path, scale);
            activeModel = "obj";
            stopwatch.Stop();
            status.text = $"Model loaded in {stopwatch.Elapsed}";
            loaded.Add(model);
            
            if (ObjParser.dataNames.Count > 0)
            {
                minvalues = new string[ObjParser.dataNames.Count];
                maxvalues = new string[ObjParser.dataNames.Count];
                dropdown.gameObject.SetActive(true);
                slider.gameObject.SetActive(true);
                range.gameObject.SetActive(true);
                applyScale.gameObject.SetActive(true);
                gradient.gameObject.SetActive(true);
                SetDropDownMenu();
                SetModelColor();
            }
            
            Camera.main.transform.position = new Vector3(0, 1.5f, -10);
        }
        else if (extension == ".xml")
        {
            activeModel = "xml";
            FileReader fileReader = new FileReader();
            fileReader.processXML(path);
            string[] filepaths = fileReader.filepaths.ToArray();
            string[][] filedata = fileReader.asciiParams.ToArray();
            pclRenderer = gameObject.GetComponent<PointCloudRenderer>();
            var importer = new PlyImporter();
            //var cloud = importer.ImportAsPointCloudData("C:/Users/Alden/Documents/Helios/PLY/StanfordDragon.ply");
            var cloud = importer.ImportAsPointCloudDataXYZ(filepaths, filedata);
            pointSize.gameObject.SetActive(true);
            pclRenderer.sourceData = cloud;
            pclRenderer.pointSize = 0.01f;
            pointCloud.Add(pclRenderer);
            
            Camera.main.transform.position = new Vector3(fileReader.origins[0], fileReader.origins[1], fileReader.origins[2] - 10);
            pointSize.value = pclRenderer.pointSize * 50;
            
            
            /* this loads as mesh instead of point cloud buffer. its slower.
            var importer = new PlyImporter();
            //var cloud = importer.ImportAsMesh("C:/Users/Alden/Documents/Helios/PLY/StanfordDragon.ply");
            var cloud = importer.ImportXYZAsMesh(filepaths, filedata);
            var meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = cloud;

            var meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = importer.GetDefaultMaterial();
            //meshRenderer.sharedMaterial.shader = pointshader;
            */
        }
    }
    
    //This function is attached to the button for choosing a file, and it calls the file loader function
    public void chooseFromDirectory()
    {
        StartCoroutine( ShowLoadDialogCoroutine() );
    }
    
    //This function checks user input, whether it be from the file picker, or the url written directly into the path box.
    public void checkValidPath()
    {
        string path = pathInputField.text;
        
        var extension = Path.GetExtension(path);
        
        if (path.Length == 0)
        {
            status.text = "Input Path to OBJ or XML file";
            
        }
        else if (!File.Exists(path))
        {
            status.text = $"File does not exist";
            
        }
        else if (extension != ".obj" && extension != ".xml")
        {
            status.text = $"Not valid {extension} file";
            
        }
        else
        {
            Load(path, extension);
        }
    }
    //We call Clear after returning to the menu to unload models and ui features associated with a certain type of model
    public void Clear()
    {
        foreach (var model in loaded)
        {
            Destroy(model);
        }

        foreach (var cloud in pointCloud)
        {
            cloud.sourceData = null;
        }
        
        loaded.Clear();
        pointCloud.Clear();
        rotation = new Vector3();
        ObjParser.dataNames.Clear();
        ObjParser.colorList.Clear();
        pathInputField.text = "";
    }

    public void Quit()
    {
        Application.Quit();
    }
    
    //this lets user use the slider to apply radiation model, etc.
    private void ValueChangeCheck()
    {
        foreach (var model in loaded)
        {
            foreach (var child in model.GetComponentsInChildren<Renderer>())
            {
                for (int i = 0; i < child.sharedMaterials.Length; i++)
                {
                    child.sharedMaterials[i].SetFloat("_Gradient", slider.value);
                }
            }
            
        }
    }

    public void ChangeAxis()
    {
        if (axistText.text == "X")
        {
            axistText.text = "Y";
            axisSlider.value = rotation.y;
            return;
        }
        if (axistText.text == "Y")
        {
            axistText.text = "Z";
            axisSlider.value = rotation.z;
            return;
        }
        if (axistText.text == "Z")
        {
            axistText.text = "X";
            axisSlider.value = rotation.x;
            return;
        }
    }

    private void rotateObject()
    {
        if (axistText.text == "X")
        {
            rotation.x = axisSlider.value;
        }
        if (axistText.text == "Y")
        {
            rotation.y = axisSlider.value;
        }
        if (axistText.text == "Z")
        {
            rotation.z = axisSlider.value;
        }

        if (activeModel == "xml")
        {
            pclRenderer.transform.rotation = Quaternion.Euler(new Vector3(rotation.x * 360, rotation.y * 360, rotation.z * 360));
        }

        if (activeModel == "obj")
        {
            model.transform.rotation = Quaternion.Euler(new Vector3(rotation.x * 360, rotation.y * 360, rotation.z * 360));
        }
    }

    //use this function to access slider to change point size of point cloud
    private void ValueChangeCheckPointCloud()
    {
        foreach (var model in pointCloud)
        {
            model.pointSize = pointSize.value/50f;
        }
    }

    //if obj model comes with plugins, this will load them all into a drop down menu to select. This function is 
    //executed when user hits load button from main menu and model includes plugins.
    private void SetDropDownMenu()
    {
        List<string> names = ObjParser.dataNames;
        dropdown.AddOptions(names);
        //store default values for each model in list to be displayed
        for(int i=0; i<names.Count; i++)
        {
            minvalues[i] = "0";
            maxvalues[i] = $"{DataProcess.maxDatList[i]}";
        }
        
        SetModelColor();
    }

    //user can rescale plugins, this will rescale color data for that particular model. When user hits "Apply" button
    //this function will execute.
    public void ScaleDropDown()
    {
        var minVal = float.Parse(minvalue.text, CultureInfo.InvariantCulture.NumberFormat);
        var maxVal = float.Parse(maxvalue.text, CultureInfo.InvariantCulture.NumberFormat);
        
        //save new values in array
        minvalues[dropdown.value] = minvalue.text;
        maxvalues[dropdown.value] = maxvalue.text;
        
        DataProcess.ReScaleDat(minVal, maxVal, dropdown.value);

        SetModelColor();
    }
    
    //Function that changes the colors of a particular model based on scale values. Function is called when
    //user switches between models in the dropdown.
    private void SetModelColor()
    {
        //changing dropdown value means changing display
        minvalue.text = minvalues[dropdown.value];
        maxvalue.text = maxvalues[dropdown.value];
        
        foreach (var model in loaded)
        {
            foreach (var child in model.GetComponentsInChildren<MeshFilter>())
            {
                
                child.mesh.colors = ObjGeometryProcessor.ResetColor(dropdown.value).ToArray();
            }
            
        }
        
    }
    
    //added this function because could not figure out how to toggle panel on and off
    //using code so I just make it small and then bring it back to scale
    public void bringBackScale()
    {
        panel.gameObject.transform.localScale  = new Vector3(1,1,1);
        dropdown.ClearOptions();
        range.gameObject.SetActive(false);
        applyScale.gameObject.SetActive(false);
        gradient.gameObject.SetActive(false);
        pointSize.gameObject.SetActive(false);
        axisSlider.gameObject.SetActive(false);
        minvalue.text = "";
        maxvalue.text = "";
        slider.value = 0;
    }

    //This function loads the file finder option on the menu
    IEnumerator ShowLoadDialogCoroutine()
    {
        //FileBrowser.SetFilters(true, new FileBrowser.Filter("Images", ".jpg", ".png"),
            //new FileBrowser.Filter("Text Files", ".txt", ".pdf"));
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, true, null, null,
            "Load Files and Folders", "Load");
        if (FileBrowser.Success)
        {
            // Print paths of the selected files (FileBrowser.Result) (null, if FileBrowser.Success is false)
            pathInputField.text = FileBrowser.Result[0];
            //FileBrowserHelpers.CopyFile( FileBrowser.Result[0], destinationPath );
        }
    }
}