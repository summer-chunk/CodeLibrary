using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.SceneManagement;



public class CameraBoundingBox : MonoBehaviour
{


    public GameObject firstPerson;

    public Transform[] cameraPositions;
    public GameObject[] startPositionParent;
    public GameObject[] endPositionParent;
    public Transform[] UAVpath1;

    int currentUAVIndex;
    float UAVMovespeed = 5f;

    bool isUAV;

    public bool isWalkCircle = false;

    public int currentCameraIndex = 0;
    int maxCameraCount = 8;

    GameObject[] scenePersons = new GameObject[0];

    Dictionary<int, List<List<string>>> modelIndiciesMap;
    Dictionary<int, int> modelMapIndicies;

    List<string> groupIndicies;

    //int personCount;
    int group1PersonCount = 0;
    int group2PersonCount = 0;
    int currentFramePersonCount = 0;
    int currentFramePersonCount2 = 0;
    int group1TimeSpan;
    int group2TimeSpan;
    //int currentFbxIndex = 1;
    List<int> currentFbxIndices;
    List<int> currentFbxIndices2;

    [EnumFlag]
    public PersonCountEnum personCountChosen;

    List<int> personCountList;
    Dictionary<int, int[]> fbxUsingRange;
    Dictionary<int, int> startGroupIds;

    int groupIndex = 0;
    int screenShotCount = 0;

    int[] limitedPersonCount;
    int[] limitedPersonCount2;

    static int tolerance = 0;


    GameObject fbxModelPrefab;
    public RuntimeAnimatorController walkController;

    Camera mainCamera;

    RenderTexture captureRT;

    Vector2 screenPosition;

    float personHeight = 0.43f;
    float personWidth = 0.15f;
    float personThick = 0.064f;

    float heightOffset = 0.02f;

    Vector3[] cubeEdgePositions;
    Vector3[] cubeEdgeScreenPositions;

    int[] startPositionIndices;
    int[] endPositionIndices;

    //const string datasetImagesPath = "D:/DATA/datasets/City1M_test";
    const string datasetImagesPath = "E:/MegaGroup_continue1";
    string trainTestStatus;

    [HideInInspector]
    public static float runtimeRatio = 3f;

    int screenShotIndex;
    float shotTimeInterval;
    float nextShotTime = 1f;
    float accumulateTime;

    string currentTextLine = "";
    string currentWriteTxt = "";
    string currentTextLine2 = "";
    string currentWriteTxt2 = "";

    List<Rect> currentBoundings;
    List<float> overlapAreas;

    bool currentIsNight = false;

    public LightingSettings nightLightingSetting;
    LightingSettings dayLightingSetting;

    SineCurve lightHCurve;
    SineCurve lightSCurve;
    SineCurve lightVCurve;

    public GameObject dayLight;
    public GameObject nightLight;
    public GameObject roadLights;
    public Material defaultSkyboxMaterial;

    [HideInInspector]
    public bool gameIsPaused;


    float preLogTime;
    List<float> timeCostList;

    List<System.Tuple<string, byte[]>> saveJpgBuffer;

    [HideInInspector]
    public float circleRadius;

    private void Awake()
    {
        startPositionIndices = new int[6];
        endPositionIndices = new int[6];
        for (int i = 0; i < 6; i++)
        {
            startPositionIndices[i] = i;
            endPositionIndices[i] = 5 - i;
        }
        ShuffleStartEndPositions();

        isWalkCircle = true;
        circleRadius = 0.4f; // 0.6f;

        runtimeRatio = 5f;
        shotTimeInterval = .05f / runtimeRatio;

        isUAV = false;
        if (!isUAV)
        {
            transform.position = cameraPositions[currentCameraIndex].position;
            transform.rotation = cameraPositions[currentCameraIndex].rotation;
        }
        else
        {
            transform.position = UAVpath1[currentUAVIndex].position;
            transform.rotation = UAVpath1[currentUAVIndex].rotation;
        }
        currentUAVIndex = 0;

        trainTestStatus = "train";
        groupIndicies = File.ReadAllLines("Assets/Resources/person_models/groupSplits/" + string.Format("{0}Group_part1", trainTestStatus) + ".txt").ToList();

    }

    private void Start()
    {

        if (!Directory.Exists(datasetImagesPath + "/Cam"))
        {
            for (int i = 1; i <= 8; i++)
            {
                Directory.CreateDirectory(datasetImagesPath + "/Cam_" + i.ToString());
            }
        }

        saveJpgBuffer = new List<System.Tuple<string, byte[]>>();
        Thread saveJpgReader = new Thread(SaveJpgThreadFunction);
        saveJpgReader.Start();

        SetPersonCountList();
        //if (personCount == 2) {
        //    currentFbxIndex = 12125;
        //}
        //currentFbxIndex = 1;

        #region �������Ϣ�ļ���ȡ����
        //modelIndiciesMap = new Dictionary<int, List<List<string>>>();
        //modelMapIndicies = new Dictionary<int, int>();

        //for (int pcount = 2; pcount <= 6; pcount++) { 
        //    string[] lines = File.ReadAllLines("Assets/Resources/person_models/groupSplits/" + trainTestStatus + "Group_" + pcount.ToString() + ".txt");
        //    modelIndiciesMap[pcount] = new List<List<string>>();
        //    foreach (string line in lines) {
        //        var words = line.Split(',');
        //        List<string> curGroup = new List<string>();
        //        foreach (var word in words) {
        //            curGroup.Add(word);
        //        }
        //        modelIndiciesMap[pcount].Add(curGroup);
        //    }
        //    modelMapIndicies[pcount] = 0;
        //}
        #endregion

        mainCamera = GetComponent<Camera>();
        captureRT = new RenderTexture(Screen.width, Screen.height, 0);
        //RenderTexture.active = captureRT;
        //mainCamera.targetTexture = RenderTexture.active;
        //dayLightingSetting = UnityEditor.Lightmapping.lightingSettings;

        cubeEdgePositions = new Vector3[12];
        cubeEdgeScreenPositions = new Vector3[12];
        currentBoundings = new List<Rect>();
        overlapAreas = new List<float>();

        currentFbxIndices = new List<int>();
        currentFbxIndices2 = new List<int>();

        SwitchPersons();
        SwitchPersons2();

        //groupIndex = 9209;
        //screenShotIndex = 737440;

        timeCostList = new List<float>();
        preLogTime = Time.time;

        lightHCurve = new SineCurve(45, 45, runtimeRatio); // SineCurve(55, 55, runtimeRatio);
        lightSCurve = new SineCurve(15, 15, runtimeRatio); // SineCurve(20, 20, runtimeRatio);
        lightVCurve = new SineCurve(15, 70, runtimeRatio); // SineCurve(20, 80, runtimeRatio);

    }


    private void Update()
    {

        HandleGlobalLight();

        //Debug.Log(RenderSettings.ambientLight);

        if (accumulateTime < 0.05f)
        {
            accumulateTime += Time.deltaTime;
            return;
        }
        accumulateTime = 0f;

        if (isUAV)
        {
            HandleUAVActions();
        }
        else
        {
            HandleCameraShot();
        }


    }

    void ShuffleStartEndPositions()
    {

        if (isWalkCircle)
        {
            startPositionIndices = new int[6] { 0, 1, 2, 3, 4, 5 };
            endPositionIndices = new int[6] { 0, 1, 2, 3, 4, 5 };
            return;
        }

        byte[] keys = new byte[startPositionIndices.Length];
        new System.Random().NextBytes(keys);
        System.Array.Sort(keys, startPositionIndices);
        new System.Random().NextBytes(keys);
        System.Array.Sort(keys, endPositionIndices);

        for (int i = 0; i < startPositionIndices.Length; i++)
        {
            if (startPositionIndices[i] == 0)
            {
                Swap(ref startPositionIndices[i], ref startPositionIndices[0]);
                break;
            }
        }
        for (int i = 0; i < endPositionIndices.Length; i++)
        {
            if (endPositionIndices[i] == 0)
            {
                Swap(ref endPositionIndices[i], ref endPositionIndices[0]);
                break;
            }
        }

        if (currentCameraIndex == 3)
        {

            startPositionIndices = new int[6] { 0, 1, 2, 3, 4, 5 };
            endPositionIndices = new int[6] { 0, 1, 2, 3, 4, 5 };
        }
        if (currentCameraIndex == 4 || currentCameraIndex == 6)
        {
            for (int i = 0; i < startPositionIndices.Length; i++)
            {
                if (startPositionIndices[i] == 1)
                {
                    Swap(ref startPositionIndices[i], ref startPositionIndices[1]);
                    break;
                }
            }
            for (int i = 0; i < endPositionIndices.Length; i++)
            {
                if (endPositionIndices[i] == 1)
                {
                    Swap(ref endPositionIndices[i], ref endPositionIndices[1]);
                    break;
                }
            }
        }
    }

    void HandleCameraShot()
    {

        if (Time.time > nextShotTime)
        {

            if (gameIsPaused)
            {
                nextShotTime = Time.time + shotTimeInterval;
                return;
            }

            currentFramePersonCount = 0;
            currentFramePersonCount2 = 0;
            currentTextLine = "";
            currentTextLine2 = "";
            currentBoundings.Clear();
            overlapAreas.Clear();

            bool group1Valid = true, group2Valid = true;
            for (int i = 0; i < group1PersonCount; i++)
            {
                if (!Get12EdgePositions(i))
                {
                    //Debug.LogError("Invalid" + Time.time);
                    group1Valid = false;
                    break;
                }
            }
            if (group2PersonCount == 0)
            {
                group2Valid = false;
            }
            for (int i = group1PersonCount; i < scenePersons.Length; i++)
            {
                if (!Get12EdgePositions(i))
                {
                    //Debug.LogError("Invalid group 2  " + i.ToString());
                    group2Valid = false;
                    break;
                }
            }
            if (group1Valid && group2Valid)
            {
                CaptureCameraShot(2);
            }
            //else if(group1Valid) {
            //    CaptureCameraShot(0);
            //} 
            //else if(group2Valid) 
            //{
            //    CaptureCameraShot(1);
            //}

            HandleSceneSwitch();
        }

    }

    bool Get12EdgePositions(int personi)
    {

        firstPerson = scenePersons[personi];

        Vector3 personBottomPosition = firstPerson.transform.position;
        cubeEdgePositions[0] = personBottomPosition + firstPerson.transform.forward * personThick / 2;
        cubeEdgePositions[1] = personBottomPosition + firstPerson.transform.right * personWidth / 2;
        cubeEdgePositions[2] = personBottomPosition - firstPerson.transform.forward * personThick / 2;
        cubeEdgePositions[3] = personBottomPosition - firstPerson.transform.right * personWidth / 2;

        Vector3 personCenterPosition = personBottomPosition + firstPerson.transform.up * personHeight / 2;

        cubeEdgePositions[4] = personCenterPosition + firstPerson.transform.forward * personThick / 2 + firstPerson.transform.right * personWidth / 2;
        cubeEdgePositions[5] = personCenterPosition - firstPerson.transform.forward * personThick / 2 + firstPerson.transform.right * personWidth / 2;
        cubeEdgePositions[6] = personCenterPosition - firstPerson.transform.forward * personThick / 2 - firstPerson.transform.right * personWidth / 2;
        cubeEdgePositions[7] = personCenterPosition + firstPerson.transform.forward * personThick / 2 - firstPerson.transform.right * personWidth / 2;

        cubeEdgePositions[8] = cubeEdgePositions[0] + firstPerson.transform.up * personHeight;
        cubeEdgePositions[9] = cubeEdgePositions[1] + firstPerson.transform.up * personHeight;
        cubeEdgePositions[10] = cubeEdgePositions[2] + firstPerson.transform.up * personHeight;
        cubeEdgePositions[11] = cubeEdgePositions[3] + firstPerson.transform.up * personHeight;

        for (int i = 0; i < cubeEdgePositions.Length; i++)
        {
            cubeEdgePositions[i].y -= heightOffset;
        }

        //anchorMax.transform.position = cubeEdgePositions[0];
        //anchorMin.transform.position = cubeEdgePositions[1];

        for (int i = 0; i < 12; i++)
        {
            cubeEdgeScreenPositions[i] = mainCamera.WorldToScreenPoint(cubeEdgePositions[i]);
            if (cubeEdgeScreenPositions[i].x < float.Epsilon || cubeEdgeScreenPositions[i].y < float.Epsilon
                || cubeEdgeScreenPositions[i].x > Screen.width || cubeEdgeScreenPositions[i].y > Screen.height)
            {
                return true;
            }
            cubeEdgeScreenPositions[i].y = Screen.height - cubeEdgeScreenPositions[i].y;
        }

        float top = cubeEdgeScreenPositions[0].y, bottom = cubeEdgeScreenPositions[0].y, left = cubeEdgeScreenPositions[0].x, right = cubeEdgeScreenPositions[0].x;

        for (int i = 1; i < 12; i++)
        {
            top = Mathf.Min(top, cubeEdgeScreenPositions[i].y);
            bottom = Mathf.Max(bottom, cubeEdgeScreenPositions[i].y);
            left = Mathf.Min(left, cubeEdgeScreenPositions[i].x);
            right = Mathf.Max(right, cubeEdgeScreenPositions[i].x);
        }

        //GUI.Box(new Rect(new Vector2(left, bottom), new Vector2(right - left, top - bottom)), "");
        if (!IsValidBoundings(new Rect(left, top, right - left, bottom - top)))
        {
            return false;
        }

        if (personi < group1PersonCount)
        {
            //currentTextLine += ((currentFbxIndex -1 - group1PersonCount + personi).ToString() + "," + Round4String(left) + "," + Round4String(top) +
            //"," + Round4String(right - left) + "," + Round4String(bottom - top) + "," +
            //Round4String(Vector3.Distance(transform.position, scenePersons[personi].transform.position)) + " ");
            currentFramePersonCount++;
            currentTextLine += (currentFbxIndices[personi].ToString() + "," + Round4String(left) + "," + Round4String(top) +
            "," + Round4String(right - left) + "," + Round4String(bottom - top) + "," +
            Round4String(Vector3.Distance(transform.position, scenePersons[personi].transform.position)) + " ");
        }
        else
        {
            currentFramePersonCount2++;
            currentTextLine2 += (currentFbxIndices2[personi-group1PersonCount].ToString() + "," + Round4String(left) + "," + Round4String(top) +
            "," + Round4String(right - left) + "," + Round4String(bottom - top) + "," +
            Round4String(Vector3.Distance(transform.position, scenePersons[personi].transform.position)) + " ");
        }

        return true;
    }

    void CaptureCameraShot(int whichGroup)
    {
        //Debug.Log(whichGroup);
        //if (whichGroup == 0 || whichGroup == 2)
        //{
        //    //print(limitedPersonCount.Length.ToString() + "    +++++    " + currentFramePersonCount.ToString());
        //    if (limitedPersonCount[currentFramePersonCount] <= 0)
        //    {
        //        if (whichGroup == 0)
        //        {
        //            return;
        //        }
        //        else
        //        {
        //            whichGroup = 1;
        //        }
        //    }
        //    else
        //    {
        //        limitedPersonCount[currentFramePersonCount]--;
        //    }
        //}
        //if (whichGroup == 1 || whichGroup == 2)
        //{
        //    if (limitedPersonCount2[currentFramePersonCount2] <= 0)
        //    {
        //        if (whichGroup == 1)
        //        {
        //            return;
        //        }
        //        else
        //        {
        //            whichGroup = 0;
        //        }
        //    }
        //    else
        //    {
        //        limitedPersonCount2[currentFramePersonCount2]--;
        //    }
        //}

        if (whichGroup == 0 || whichGroup == 1)
        {
            return;
        }
        else // if (whichGroup == 2)
        {
            if (limitedPersonCount[currentFramePersonCount] <= 0 || limitedPersonCount2[currentFramePersonCount2] <= 0)
            {
                return;
            }
            else
            {
                limitedPersonCount[currentFramePersonCount]--;
                limitedPersonCount2[currentFramePersonCount2]--;
            }

        }

        nextShotTime = Time.time + shotTimeInterval;

        //RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 0);
        //camera.targetTexture = rt;
        //camera.Render();

        //RenderTexture.active = rt;
        //Texture2D screenShot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        //screenShot.ReadPixels(Rect.MinMaxRect(0, 0, Screen.width, Screen.height), 0, 0);
        //screenShot.Apply();

        //camera.targetTexture = null;
        //RenderTexture.active = null;
        //Destroy(rt);
        mainCamera.targetTexture = captureRT;
        RenderTexture.active = captureRT;

        mainCamera.Render();

        Texture2D screenShot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenShot.ReadPixels(Rect.MinMaxRect(0, 0, Screen.width, Screen.height), 0, 0);
        screenShot.Apply();

        mainCamera.targetTexture = null;
        RenderTexture.active = null;

        byte[] jpgBytes = screenShot.EncodeToJPG();

        string dayNightStr;
        if (currentIsNight)
        {
            dayNightStr = "night";
        }
        else
        {
            dayNightStr = "day";
        }

        if (whichGroup == 0 || whichGroup == 2)
        {
            //string jpgName = "/Images_" + group1PersonCount.ToString() + "/" + IntAddZeros(groupIndex - 1) + "_pCount" + group1PersonCount.ToString() + "_cam" + (currentCameraIndex + 1).ToString() +
            //"_" + IntAddZeros(++screenShotIndex).ToString() + "_" + dayNightStr + ".jpg";
            long time = (long)(System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1)).TotalSeconds);
            string jpgName = "/Cam_" + (currentCameraIndex + 1).ToString() + "/" + "cam" + (currentCameraIndex + 1).ToString() + "_" + trainTestStatus + "_" + dayNightStr +
            "_" + IntAddZeros(++screenShotIndex).ToString() + "_" + time.ToString() + ".jpg";
            currentTextLine = jpgName + " " + (groupIndex - 1).ToString() + " " + currentTextLine;

            if (whichGroup == 2)
            {
                currentTextLine2 = jpgName + " " + groupIndex.ToString() + " " + currentTextLine2;
            }
            else
            {
                using (StreamWriter temp = new StreamWriter(datasetImagesPath + "/pCount_Unvalid.txt", true))
                {
                    temp.WriteLine(jpgName);
                }
            }

            saveJpgBuffer.Add(System.Tuple.Create(jpgName, (byte[])jpgBytes.Clone()));
            print("Save Picture by Group 1: " + jpgName);
        }
        else
        {
            //string jpgName = "/Images_" + group2PersonCount.ToString() + "/" + IntAddZeros(groupIndex) + "_pCount" + group2PersonCount.ToString() + "_cam" + (currentCameraIndex + 1).ToString() +
            //"_" + IntAddZeros(++screenShotIndex).ToString() + "_" + dayNightStr + ".jpg";

            long time = (long)(System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1)).TotalSeconds);
            string jpgName = "/Cam_" + (currentCameraIndex + 1).ToString() + "/" + "cam" + (currentCameraIndex + 1).ToString() + "_" + trainTestStatus + "_" + dayNightStr +
            "_" + IntAddZeros(++screenShotIndex).ToString() + "_" + time.ToString() + ".jpg";
            currentTextLine2 = jpgName + " " + groupIndex.ToString() + " " + currentTextLine2;

            using (StreamWriter temp = new StreamWriter(datasetImagesPath + "/pCount_Unvalid.txt", true))
            {
                temp.WriteLine(jpgName);
            }

            saveJpgBuffer.Add(System.Tuple.Create(jpgName, (byte[])jpgBytes.Clone()));
            print("Save Picture by Group 2: " + jpgName);
        }


        //Debug.Log("Write! " + saveJpgBuffer.Count.ToString());

        //Thread jpgThread = new Thread((t1) => {
        //    Tuple<string, byte[]> tuple = t1 as Tuple<string, byte[]>;
        //    File.WriteAllBytes(datasetImagesPath + "/Images/" + tuple.Item1, tuple.Item2);
        //});
        //jpgThread.Start(new Tuple<string, byte[]>(jpgName, (byte[])jpgBytes.Clone()));

        //JpgSaver jpgThreadClass = new JpgSaver(datasetImagesPath + "/Images/" + jpgName, jpgBytes);
        //Thread jpgThread = new Thread(jpgThreadClass.SaveJpg);
        //jpgThread.Start();

        if (whichGroup == 0 || whichGroup == 2)
        {
            currentWriteTxt += currentTextLine + '\n';
        }
        if (whichGroup == 1 || whichGroup == 2)
        {
            currentWriteTxt2 += currentTextLine2 + '\n';
        }

        screenShotCount++;

    }

    void HandleSceneSwitch()
    {

        //if (screenShotCount >= 7) {
        bool flag1 = (limitedPersonCount.Sum() == 0 && limitedPersonCount2.Sum() == 0);
        bool flag2 = (tolerance > 100) && (limitedPersonCount.Sum() < 3 && limitedPersonCount2.Sum() < 3); // at least capture one
        // bool flag3 = (tolerance > 300);
        if (flag1 || flag2)
        {
            if (flag2)
            {
                print("Final tolerance trigger!!!!");
            }
            currentCameraIndex++;
            tolerance = 0;

            timeCostList.Add(Round4Float(Time.time - preLogTime));
            preLogTime = Time.time;

            if (currentCameraIndex >= maxCameraCount)
            {

                string timeCostString = "";
                string timeCostpercentString = "";
                float totalTimeCost = timeCostList.Sum();
                for (int i = 0; i < timeCostList.Count; i++)
                {
                    timeCostString += timeCostList[i].ToString() + " ";
                    timeCostpercentString += Round4String(timeCostList[i] / totalTimeCost * 100) + "% ";
                }
                print(timeCostString + timeCostpercentString + totalTimeCost.ToString() + " ");
                timeCostList.Clear();


                currentCameraIndex = 0;

                if (currentIsNight)
                {

                    //using (StreamWriter temp = new StreamWriter(datasetImagesPath + "/pCount" + personCount.ToString() + ".txt", true)) {
                    //    temp.Write(currentWriteTxt);
                    //}
                    //currentWriteTxt = "";

                    //if (currentFbxIndex > fbxUsingRange[group1PersonCount][1]) {
                    //    if(currentFbxIndex == personCountList[personCountList.Count-1]) {
                    //        Debug.Log("All Works Done!");
                    //        QuitGame();
                    //        return;
                    //    }
                    //    do {
                    //        group1PersonCount++;
                    //    } while (group1PersonCount <= 6 && !personCountList.Contains(group1PersonCount));
                    //    if(group1PersonCount > 6) {
                    //        Debug.LogError("Person Count Code Bugs!");
                    //        QuitGame();
                    //        return;
                    //    }
                    //    groupIndex = startGroupIds[group1PersonCount];
                    //    currentFbxIndex = fbxUsingRange[group1PersonCount][0];
                    //}
                    SetNight2Day();

                    currentIsNight = false;
                }
                else
                {
                    SetDay2Night();
                    currentIsNight = true;
                }

            }

            SaveLabelTxt();

            if (--group1TimeSpan <= 0)
            {
                group1TimeSpan = Random.Range(2, 4);
                for (int i = 0; i < group1PersonCount; i++)
                {
                    Destroy(scenePersons[i]);
                }
                Resources.UnloadUnusedAssets();
                SwitchPersons();
            }
            else
            {
                SetLimitedPersonCount();
            }

            if (--group2TimeSpan <= 0)
            {
                group2TimeSpan = Random.Range(2, 4);
                for (int i = group1PersonCount; i < scenePersons.Length; i++)
                {
                    Destroy(scenePersons[i]);
                }
                Resources.UnloadUnusedAssets();
                SwitchPersons2();
            }
            else
            {
                SetLimitedPersonCount2();
            }

            transform.position = cameraPositions[currentCameraIndex].position;
            transform.rotation = cameraPositions[currentCameraIndex].rotation;
            ShuffleStartEndPositions();
            screenShotCount = 0;

            for (int i = 0; i < scenePersons.Length; i++)
            {
                PersonMove curScript = scenePersons[i].GetComponent<PersonMove>();

                if (isWalkCircle)
                {
                    if (i == 0)
                    {
                        curScript.weightPoints[0] = startPositionParent[currentCameraIndex].transform.GetChild(startPositionIndices[0]);
                        curScript.weightPoints[1] = endPositionParent[currentCameraIndex].transform.GetChild(endPositionIndices[0]);
                        curScript.ResetPosition();
                    }
                    else if (i == group1PersonCount)
                    {
                        curScript.weightPoints[0] = startPositionParent[currentCameraIndex].transform.GetChild(startPositionIndices[1]);
                        curScript.weightPoints[1] = endPositionParent[currentCameraIndex].transform.GetChild(endPositionIndices[1]);
                        curScript.ResetPosition();
                    }
                    else
                    {
                        curScript.ResetPosition(startPositionIndices[i % 6]);
                    }
                }
                else
                {
                    curScript.weightPoints[0] = startPositionParent[currentCameraIndex].transform.GetChild(startPositionIndices[i]);
                    curScript.weightPoints[1] = endPositionParent[currentCameraIndex].transform.GetChild(endPositionIndices[i]);
                    curScript.ResetPosition();
                }

            }

        }
        else
        {
            tolerance++;
        }
    }

    void SwitchPersons()
    {

        #region �������Ϣ�ļ���ȡ����
        //while (true) {
        //    group1PersonCount = Random.Range(2, 7);
        //    if (modelMapIndicies[group1PersonCount] < modelIndiciesMap[group1PersonCount].Count) {
        //        break;
        //    }
        //}
        //var curGroupFbx = modelIndiciesMap[group1PersonCount][modelMapIndicies[group1PersonCount]];
        #endregion

        //group1TimeSpan = Random.Range(2, 4); //Random.Range(2, 5); 

        List<string> curGroupFbx = new List<string>();
        currentFbxIndices.Clear();

        if (groupIndex >= groupIndicies.Count)
        {
            QuitGame();
        }

        var words = groupIndicies[groupIndex].Split(',');
        foreach (var word in words)
        {
            curGroupFbx.Add(word);
        }
        group1PersonCount = curGroupFbx.Count();

        SetLimitedPersonCount();

        //scenePersons = new GameObject[group1PersonCount];
        GameObject[] scenePersons2 = new GameObject[group1PersonCount + group2PersonCount];
        System.Array.Copy(scenePersons, scenePersons.Length - group2PersonCount, scenePersons2, group1PersonCount, group2PersonCount);
        scenePersons = scenePersons2;
        groupIndex++;

        for (int i = 0; i < group1PersonCount; i++)
        {
            string currentFbxIndex = curGroupFbx[i];
            int FbxIndexInt = int.Parse(currentFbxIndex);
            currentFbxIndices.Add(FbxIndexInt);
            if (FbxIndexInt > 31000)
            {
                Debug.LogError("Person FBX index Overflow!");
                QuitGame();
            }

            string modelNameExt;
            if (currentFbxIndex.Length < 2)
            {
                modelNameExt = "000" + currentFbxIndex.ToString();
            }
            else if (currentFbxIndex.Length < 3)
            {
                modelNameExt = "00" + currentFbxIndex.ToString();
            }
            else if (currentFbxIndex.Length < 4)
            {
                modelNameExt = "0" + currentFbxIndex.ToString();
            }
            else
            {
                modelNameExt = currentFbxIndex.ToString();
            }

            modelNameExt = "person_models/exports_" + trainTestStatus + string.Format("/City1Mv2_{0}_{1}", trainTestStatus, modelNameExt);

            fbxModelPrefab = (GameObject)Resources.Load(modelNameExt);
            if (fbxModelPrefab == null)
            {
                Debug.LogError("No fbx model!");
                //i--;
                continue;
            }

            GameObject personTest = Instantiate(fbxModelPrefab, Vector3.zero, Quaternion.identity);

            Animator currentAnimator = personTest.AddComponent<Animator>();
            currentAnimator.runtimeAnimatorController = walkController;
            currentAnimator.speed = (UnityEngine.Random.value * 0.6f + 0.7f) * runtimeRatio;
            //StartCoroutine(AddAnimator());

            Rigidbody curRigidbody = personTest.AddComponent<Rigidbody>();
            curRigidbody.useGravity = true;
            if (i == 0)
            {
                curRigidbody.mass = 100f;
            }
            curRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            curRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

            //CapsuleCollider curCollider = personTest.AddComponent<CapsuleCollider>();
            //curCollider.radius = 2.31f;
            //curCollider.center = new Vector3(0, 5.844589f, 0);
            //curCollider.height = 10.82011f;

            personTest.transform.localScale = Vector3.one * 0.025f;
            //personTest.transform.position = new Vector3(0, 0.06f, 0);
            var currentPersonMove = personTest.AddComponent<PersonMove>();
            currentPersonMove.moveSpeed = (UnityEngine.Random.value * 0.3f + 0.5f) * runtimeRatio;

            scenePersons[i] = personTest;

            if (isWalkCircle)
            {
                if (i == 0)
                {
                    currentPersonMove.isCenterPerson = true;
                    currentPersonMove.moveSpeed = (UnityEngine.Random.value * 0.3f + 0.5f) * runtimeRatio;
                    currentPersonMove.weightPoints[0] = startPositionParent[currentCameraIndex].transform.GetChild(startPositionIndices[i]);
                    currentPersonMove.weightPoints[1] = endPositionParent[currentCameraIndex].transform.GetChild(endPositionIndices[i]);
                    currentPersonMove.ResetPosition();

                }
                else
                {
                    currentPersonMove.centerPerson = scenePersons[0];
                    currentPersonMove.ResetPosition(startPositionIndices[i]);
                }
            }
            else
            {
                currentPersonMove.isCenterPerson = true;
                currentPersonMove.weightPoints[0] = startPositionParent[currentCameraIndex].transform.GetChild(startPositionIndices[i]);
                currentPersonMove.weightPoints[1] = endPositionParent[currentCameraIndex].transform.GetChild(endPositionIndices[i]);
                currentPersonMove.ResetPosition();
            }

            for (int j = 0; j < personTest.transform.childCount; j++)
            {
                GameObject partObject = personTest.transform.GetChild(j).gameObject;
                if (partObject.GetComponent<SkinnedMeshRenderer>() != null)
                {
                    MeshCollider curMesh = partObject.AddComponent<MeshCollider>();
                    curMesh.convex = true;
                    curMesh.sharedMesh = partObject.GetComponent<SkinnedMeshRenderer>().sharedMesh;
                }
            }
        }

        scenePersons[0].GetComponent<PersonMove>().isCenterPerson = true;

        // string msg = "SwitchScene   111111   ";
        // foreach(var temp1 in currentFbxIndices) {
        //     msg += temp1.ToString() + "  ";
        // }
        // Debug.Log(msg);

        if (Random.value < 0f)
        {
            //AddPersonGroup();
        }
    }

    void SwitchPersons2()
    {

        // = Random.Range(2, 4); //Random.Range(2, 5); 

        List<string> curGroupFbx = new List<string>();
        currentFbxIndices2.Clear();

        if (groupIndex >= groupIndicies.Count)
        {
            QuitGame();
        }

        var words = groupIndicies[groupIndex].Split(',');
        foreach (var word in words)
        {
            curGroupFbx.Add(word);
        }
        group2PersonCount = curGroupFbx.Count();

        SetLimitedPersonCount2();

        //scenePersons = new GameObject[group1PersonCount];
        GameObject[] scenePersons2 = new GameObject[group1PersonCount + group2PersonCount];
        System.Array.Copy(scenePersons, scenePersons2, group1PersonCount);
        scenePersons = scenePersons2;
        groupIndex++;

        for (int i = 0; i < group2PersonCount; i++)
        {
            string currentFbxIndex = curGroupFbx[i];
            int FbxIndexInt = int.Parse(currentFbxIndex);
            currentFbxIndices2.Add(FbxIndexInt);
            if (FbxIndexInt > 31000)
            {
                Debug.LogError("Person FBX index Overflow!");
                QuitGame();
            }

            string modelNameExt;
            if (currentFbxIndex.Length < 2)
            {
                modelNameExt = "000" + currentFbxIndex.ToString();
            }
            else if (currentFbxIndex.Length < 3)
            {
                modelNameExt = "00" + currentFbxIndex.ToString();
            }
            else if (currentFbxIndex.Length < 4)
            {
                modelNameExt = "0" + currentFbxIndex.ToString();
            }
            else
            {
                modelNameExt = currentFbxIndex.ToString();
            }

            modelNameExt = "person_models/exports_" + trainTestStatus + string.Format("/City1Mv2_{0}_{1}", trainTestStatus, modelNameExt);

            fbxModelPrefab = (GameObject)Resources.Load(modelNameExt);
            if (fbxModelPrefab == null)
            {
                Debug.LogError("No fbx model!!! " + modelNameExt);
                //i--;
                continue;
            }

            GameObject personTest = Instantiate(fbxModelPrefab, Vector3.zero, Quaternion.identity);

            Animator currentAnimator = personTest.AddComponent<Animator>();
            currentAnimator.runtimeAnimatorController = walkController;
            currentAnimator.speed = (UnityEngine.Random.value * 0.6f + 0.7f) * runtimeRatio;
            //StartCoroutine(AddAnimator());

            Rigidbody curRigidbody = personTest.AddComponent<Rigidbody>();
            curRigidbody.useGravity = true;
            if (i == 0)
            {
                curRigidbody.mass = 100f;
            }
            curRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            curRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

            //CapsuleCollider curCollider = personTest.AddComponent<CapsuleCollider>();
            //curCollider.radius = 2.31f;
            //curCollider.center = new Vector3(0, 5.844589f, 0);
            //curCollider.height = 10.82011f;

            personTest.transform.localScale = Vector3.one * 0.025f;
            //personTest.transform.position = new Vector3(0, 0.06f, 0);
            var currentPersonMove = personTest.AddComponent<PersonMove>();
            currentPersonMove.moveSpeed = (UnityEngine.Random.value * 0.3f + 0.5f) * runtimeRatio;

            scenePersons[i + group1PersonCount] = personTest;


            if (isWalkCircle)
            {
                if (i == 0)
                {
                    currentPersonMove.isCenterPerson = true;
                    currentPersonMove.moveSpeed = (UnityEngine.Random.value * 0.1f + 0.6f) * runtimeRatio;
                    currentPersonMove.weightPoints[0] = startPositionParent[currentCameraIndex].transform.GetChild(startPositionIndices[i + 1]);
                    currentPersonMove.weightPoints[1] = endPositionParent[currentCameraIndex].transform.GetChild(endPositionIndices[i + 1]);
                    currentPersonMove.ResetPosition();

                }
                else
                {
                    currentPersonMove.centerPerson = scenePersons[group1PersonCount];
                    currentPersonMove.ResetPosition(startPositionIndices[i]);
                }
            }
            else
            {
                currentPersonMove.isCenterPerson = true;
                currentPersonMove.weightPoints[0] = startPositionParent[currentCameraIndex].transform.GetChild(startPositionIndices[i]);
                currentPersonMove.weightPoints[1] = endPositionParent[currentCameraIndex].transform.GetChild(endPositionIndices[i]);
                currentPersonMove.ResetPosition();
            }

            for (int j = 0; j < personTest.transform.childCount; j++)
            {
                GameObject partObject = personTest.transform.GetChild(j).gameObject;
                if (partObject.GetComponent<SkinnedMeshRenderer>() != null)
                {
                    MeshCollider curMesh = partObject.AddComponent<MeshCollider>();
                    curMesh.convex = true;
                    curMesh.sharedMesh = partObject.GetComponent<SkinnedMeshRenderer>().sharedMesh;
                }
            }
        }

        scenePersons[group1PersonCount].GetComponent<PersonMove>().isCenterPerson = true;

        // string msg = "SwitchScene 2222222   ";
        // foreach(var temp1 in currentFbxIndices2) {
        //     msg += temp1.ToString() + "  ";
        // }
        // Debug.Log(msg);
    }

    void SaveLabelTxt()
    {

        if (currentWriteTxt != "" && currentWriteTxt2 != "" && group1PersonCount == group2PersonCount)
        {
            Thread txtThread1 = new Thread((txtPath) => {
                string[] strs = (string[])txtPath;
                using (StreamWriter temp = new StreamWriter(strs[0], true))
                {
                    temp.Write(strs[1]);
                }
            });
            Debug.Log("Write TXT1 for group   " + (groupIndex - 2).ToString());
            txtThread1.Start(new string[2] {
                datasetImagesPath + "/train1_pCount" + group1PersonCount.ToString() + ".txt",
                (string)currentWriteTxt.Clone() + (string)currentWriteTxt2.Clone()
            });
            currentWriteTxt = "";
            currentWriteTxt2 = "";
        }
        else
        {

            if (currentWriteTxt != "")
            {
                Thread txtThread1 = new Thread((txtPath) => {
                    string[] strs = (string[])txtPath;
                    using (StreamWriter temp = new StreamWriter(strs[0], true))
                    {
                        temp.Write(strs[1]);
                    }
                    currentWriteTxt = "";
                });
                Debug.Log("Write TXT1 for group   " + (groupIndex - 2).ToString());
                txtThread1.Start(new string[2] {
                    datasetImagesPath + "/train1_pCount" + group1PersonCount.ToString() + ".txt", (string)currentWriteTxt.Clone()
                });
            }

            if (currentWriteTxt2 != "")
            {
                Thread txtThread2 = new Thread((txtPath) => {
                    string[] strs = (string[])txtPath;
                    using (StreamWriter temp = new StreamWriter(strs[0], true))
                    {
                        temp.Write(strs[1]);
                    }
                    currentWriteTxt2 = "";
                });
                Debug.Log("Write TXT2 for group   " + (groupIndex - 1).ToString());
                txtThread2.Start(new string[2] {
                    datasetImagesPath + "/train1_pCount" + group2PersonCount.ToString() + ".txt", (string)currentWriteTxt2.Clone()
                });
            }
        }

        Debug.Log("Current Count of Jpg(s) To SAVE: " + saveJpgBuffer.Count.ToString());
    }

    void HandleUAVActions()
    {
        if (gameIsPaused)
        {
            return;
        }

        int nextIndex = (currentUAVIndex + 1) % UAVpath1.Length;
        Vector3 targetPosition = UAVpath1[nextIndex].position;
        Vector3 targetDirection = (targetPosition - transform.position).normalized;
        targetDirection.y = 0f;
        transform.Translate(targetDirection * UAVMovespeed * Time.deltaTime, Space.World);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            currentUAVIndex = nextIndex;
        }
    }

    private void OnGUI()
    {

        return;

        GUI.backgroundColor = Color.gray;
        foreach (var bbox in currentBoundings)
        {
            GUI.Box(bbox, "");
        }

    }

    // Person Count for situation that 10 pictures are taken for 1 person group
    //void SetLimitedPersonCount() {
    //    if(personCount == 2) {
    //        limitedPersonCount = new int[3]{ 0, 0, 10};
    //    } else if(personCount == 3) {
    //        limitedPersonCount = new int[4] { 0, 0, 4, 6 };
    //    } else if(personCount == 4) {
    //        limitedPersonCount = new int[5] { 0, 0, 0, 4, 6 };
    //    } else if(personCount == 5) {
    //        limitedPersonCount = new int[6] { 0, 0, 0, 1, 3, 6 };
    //    } else if(personCount == 6) {
    //        limitedPersonCount = new int[7] { 0, 0, 0, 0, 1, 3, 6 };
    //    }
    //}

    void SetLimitedPersonCount()
    {
        if (group1PersonCount == 2)
        {
            limitedPersonCount = new int[3] { 0, 0, 3 };
        }
        else if (group1PersonCount == 3)
        {
            limitedPersonCount = new int[4] { 0, 0, 1, 2 };
        }
        else if (group1PersonCount == 4)
        {
            limitedPersonCount = new int[5] { 0, 0, 0, 1, 2 };
        }
        else if (group1PersonCount == 5)
        {
            limitedPersonCount = new int[6] { 0, 0, 0, 1, 1, 1 };
        }
        else if (group1PersonCount == 6)
        {
            limitedPersonCount = new int[7] { 0, 0, 0, 0, 1, 1, 1 };
        }
    }

    void SetLimitedPersonCount2()
    {
        if (group2PersonCount == 2)
        {
            limitedPersonCount2 = new int[3] { 0, 0, 3 };
        }
        else if (group2PersonCount == 3)
        {
            limitedPersonCount2 = new int[4] { 0, 0, 1, 2 };
        }
        else if (group2PersonCount == 4)
        {
            limitedPersonCount2 = new int[5] { 0, 0, 0, 1, 2 };
        }
        else if (group2PersonCount == 5)
        {
            limitedPersonCount2 = new int[6] { 0, 0, 0, 1, 1, 1 };
        }
        else if (group2PersonCount == 6)
        {
            limitedPersonCount2 = new int[7] { 0, 0, 0, 0, 1, 1, 1 };
        }
        else if (group2PersonCount == 0)
        {
            limitedPersonCount2 = new int[1] { 0 };
        }
    }

    void SetDay2Night()
    {
        RenderSettings.skybox = null;
        gameIsPaused = true;
        RenderSettings.ambientLight = new Color(54f / 255, 68f / 255, 68f / 255);
        float upperBound = Random.value * 360, lowerBound = Random.value * 360;
        if (lowerBound > upperBound)
        {
            Swap(ref lowerBound, ref upperBound);
        }
        lightHCurve.ResetParameters((upperBound - lowerBound) / 2, (upperBound + lowerBound) / 2);
        upperBound = Random.value * 40;
        lowerBound = Random.value * 40;
        if (lowerBound > upperBound)
        {
            Swap(ref lowerBound, ref upperBound);
        }
        lightSCurve.ResetParameters((upperBound - lowerBound) / 2, (upperBound + lowerBound) / 2);
        upperBound = Random.value * 30;
        lowerBound = Random.value * 30;
        if (lowerBound > upperBound)
        {
            Swap(ref lowerBound, ref upperBound);
        }
        lightVCurve.ResetParameters((upperBound - lowerBound) / 2, (upperBound + lowerBound) / 2);

        dayLight.SetActive(false);
        nightLight.SetActive(true);
        roadLights.SetActive(true);
        StartCoroutine(ResumeGame(.5f));
    }

    void SetNight2Day()
    {
        //RenderSettings.skybox = defaultSkyboxMaterial;
        //RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        //RenderSettings.ambientLight = new Color(184f / 255, 212f / 255, 1);

        float upperBound = Random.value * 110, lowerBound = Random.value * 110;
        if (lowerBound > upperBound)
        {
            Swap(ref lowerBound, ref upperBound);
        }
        lightHCurve.ResetParameters((upperBound - lowerBound) / 2, (upperBound + lowerBound) / 2);
        upperBound = Random.value * 40;
        lowerBound = Random.value * 40;
        if (lowerBound > upperBound)
        {
            Swap(ref lowerBound, ref upperBound);
        }
        lightSCurve.ResetParameters((upperBound - lowerBound) / 2, (upperBound + lowerBound) / 2);
        upperBound = Random.value * 40 + 60;
        lowerBound = Random.value * 40 + 60;
        if (lowerBound > upperBound)
        {
            Swap(ref lowerBound, ref upperBound);
        }
        lightVCurve.ResetParameters((upperBound - lowerBound) / 2, (upperBound + lowerBound) / 2);

        HandleGlobalLight();

        gameIsPaused = true;

        dayLight.SetActive(true);
        nightLight.SetActive(false);
        roadLights.SetActive(false);
        StartCoroutine(ResumeGame(1f));
    }

    void HandleGlobalLight()
    {
        if (gameIsPaused)
        {
            return;
        }

        lightHCurve.UpdateX(Time.deltaTime);
        lightSCurve.UpdateX(Time.deltaTime);
        lightVCurve.UpdateX(Time.deltaTime);
        //Debug.Log(lightHCurve.GetValue().ToString() + "  " + lightSCurve.GetValue().ToString() + "  " + lightVCurve.GetValue().ToString());

        //Debug.Log(lightHCurve.x.ToString() + "   " + lightHCurve.GetValue());

        if (currentIsNight)
        {
            RenderSettings.ambientLight = MapHSVTo01(lightHCurve.GetValue(), lightSCurve.GetValue(), lightVCurve.GetValue());
        }
        else
        {
            if (lightHCurve.GetValue() > 40)
            {
                RenderSettings.ambientLight = MapHSVTo01((lightHCurve.GetValue() + 140), lightSCurve.GetValue(), lightVCurve.GetValue());
            }
            else
            {
                RenderSettings.ambientLight = MapHSVTo01(lightHCurve.GetValue(), lightSCurve.GetValue(), lightVCurve.GetValue());
            }
        }
    }



    bool IsValidBoundings(Rect newRect)
    {
        overlapAreas.Add(0);
        for (int i = 0; i < currentBoundings.Count; i++)
        {
            float areaValue = CalculateRectOverlapArea(currentBoundings[i], newRect);
            //if (areaValue > 0) {
            //    print(areaValue);
            //}

            //overlapAreas[i] += areaValue;
            overlapAreas[i] = Mathf.Max(overlapAreas[i], areaValue);
            overlapAreas[overlapAreas.Count - 1] = Mathf.Max(areaValue, overlapAreas[overlapAreas.Count - 1]);
        }
        currentBoundings.Add(newRect);
        for (int i = 0; i < currentBoundings.Count; i++)
        {
            float overlapPercent = overlapAreas[i] / currentBoundings[i].height / currentBoundings[i].width;
            //if (overlapAreas[i] > 0) {
            //    print(overlapPercent);
            //}
            if (tolerance < 100)
            {
                if (overlapPercent > .7f) // 1f
                {
                    return false;
                }
            }
            else
            {
                if (overlapPercent > .9f) // 1f
                {
                    return false;
                }
            }

        }
        return true;
    }

    float CalculateRectOverlapArea(Rect r1, Rect r2)
    {
        float x1 = r1.xMin, y1 = r1.yMin, x2 = r1.xMax, y2 = r1.yMax, x3 = r2.xMin, y3 = r2.yMin, x4 = r2.xMax, y4 = r2.yMax;
        float x = Mathf.Min(x2, x4) - Mathf.Max(x1, x3), y = Mathf.Min(y2, y4) - Mathf.Max(y1, y3);
        return Mathf.Max(0, x) * Mathf.Max(0, y);
    }

    IEnumerator ResumeGame(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        gameIsPaused = false;
        preLogTime = Time.time;
    }

    void SetPersonCountList()
    {
        fbxUsingRange = new Dictionary<int, int[]>();
        personCountList = new List<int>();
        startGroupIds = new Dictionary<int, int>();
        startGroupIds[6] = 0;
        startGroupIds[5] = 2000;
        startGroupIds[4] = 4000;
        startGroupIds[3] = 6500;
        startGroupIds[2] = 9500;
        if (personCountChosen == 0)
        {
            Debug.LogError("Should Choose Person Count in Camera Script!");
            QuitGame();
        }
        if ((personCountChosen & PersonCountEnum.p2) == PersonCountEnum.p2)
        {
            personCountList.Add(2);
        }
        if ((personCountChosen & PersonCountEnum.p3) == PersonCountEnum.p3)
        {
            personCountList.Add(3);
        }
        if ((personCountChosen & PersonCountEnum.p4) == PersonCountEnum.p4)
        {
            personCountList.Add(4);
        }
        if ((personCountChosen & PersonCountEnum.p5) == PersonCountEnum.p5)
        {
            personCountList.Add(5);
        }
        if ((personCountChosen & PersonCountEnum.p6) == PersonCountEnum.p6)
        {
            personCountList.Add(6);
        }
        //fbxUsingRange[2] = new int[2] { 1, 4000 };
        //fbxUsingRange[3] = new int[2] { 4101, 13100 };
        //fbxUsingRange[4] = new int[2] { 13201, 23200 };
        //fbxUsingRange[5] = new int[2] { 23301, 33300 };
        //fbxUsingRange[6] = new int[2] { 33501, 45500 };
        fbxUsingRange[2] = new int[2] { 1, 200 };
        fbxUsingRange[3] = new int[2] { 201, 500 };
        fbxUsingRange[4] = new int[2] { 501, 900 };
        fbxUsingRange[5] = new int[2] { 901, 1900 };
        fbxUsingRange[6] = new int[2] { 1901, 2500 };
        group1PersonCount = personCountList[0];
        //currentFbxIndex = fbxUsingRange[group1PersonCount][0];
        //groupIndex = startGroupIds[group1PersonCount];
    }

    void SaveJpgThreadFunction()
    {
        while (true)
        {
            while (saveJpgBuffer.Count > 0)
            {
                File.WriteAllBytes(datasetImagesPath + "/" + saveJpgBuffer[0].Item1, saveJpgBuffer[0].Item2);
                saveJpgBuffer.RemoveAt(0);
                //Debug.Log("Read! " + saveJpgBuffer.Count);
            }
            Thread.Sleep(100);
        }
    }

    void QuitGame()
    {
        if (Application.isEditor)
        {
            EditorApplication.ExitPlaymode();
        }
        else
        {
            Application.Quit();
        }
    }

    IEnumerator AddAnimator()
    {
        yield return new WaitForSeconds(1f);
        foreach (GameObject person in scenePersons)
        {
            person.AddComponent<Animator>();
            person.GetComponent<Animator>().runtimeAnimatorController = walkController;
        }
    }

    void Swap<T>(ref T a, ref T b)
    {
        T temp = a;
        a = b;
        b = temp;
    }

    string Round2(float a)
    {
        return System.Math.Round(a, 2).ToString();
    }
    string Round6(float a)
    {
        return System.Math.Round(a, 6).ToString();
    }
    float Round4Float(float a)
    {
        return (float)System.Math.Round(a, 4);
    }
    string Round4String(float a)
    {
        return System.Math.Round(a, 4).ToString();
    }

    string IntAddZeros(int a)
    {

        string temps = a.ToString();
        return new string('0', 7 - temps.Length) + temps;

    }

    Color MapHSVTo01(float H, float S, float V)
    {
        return Color.HSVToRGB(H / 360, S / 100, V / 100);
    }

    private void OnDrawGizmos()
    {
        return;
        Gizmos.color = Color.red;
        if (Application.isPlaying)
        {
            Gizmos.DrawLine(cubeEdgePositions[0], cubeEdgePositions[1]);
            Gizmos.DrawLine(cubeEdgePositions[1], cubeEdgePositions[2]);
            Gizmos.DrawLine(cubeEdgePositions[2], cubeEdgePositions[3]);
            Gizmos.DrawLine(cubeEdgePositions[3], cubeEdgePositions[0]);
            Gizmos.DrawLine(cubeEdgePositions[4], cubeEdgePositions[5]);
            Gizmos.DrawLine(cubeEdgePositions[5], cubeEdgePositions[6]);
            Gizmos.DrawLine(cubeEdgePositions[6], cubeEdgePositions[7]);
            Gizmos.DrawLine(cubeEdgePositions[7], cubeEdgePositions[4]);
            Gizmos.DrawLine(cubeEdgePositions[8], cubeEdgePositions[9]);
            Gizmos.DrawLine(cubeEdgePositions[9], cubeEdgePositions[10]);
            Gizmos.DrawLine(cubeEdgePositions[10], cubeEdgePositions[11]);
            Gizmos.DrawLine(cubeEdgePositions[11], cubeEdgePositions[8]);
        }
    }

}

class SineCurve
{
    float A;
    float T;
    //float phi;
    float B;
    float runtimeRatio;
    public float x;
    public SineCurve(float _A = 0, float _B = 0, float _runtimeRatio = 1)
    {
        runtimeRatio = _runtimeRatio;
        ResetParameters(_A, _B);
    }

    public float GetValue()
    {
        return A * Mathf.Sin(2 * Mathf.PI / T * x) + B;
    }

    public void SetStartX()
    {
        x = Random.value * T;
    }
    public void UpdateX(float deltaTime)
    {
        x += deltaTime;
    }

    public void ResetParameters(float _A = 0, float _B = 0)
    {
        T = (Random.value + 1f) * 1000f * runtimeRatio;

        A = _A;
        B = _B;
        SetStartX();
    }

    void Swap<T>(ref T a, ref T b)
    {
        T temp = a;
        a = b;
        b = temp;
    }
}
class JpgSaver
{
    public string jpgPath;
    public byte[] jpgBytes;
    public JpgSaver(string _path, byte[] _bytes)
    {
        jpgPath = _path;
        jpgBytes = new byte[_bytes.Length];
        _bytes.CopyTo(jpgBytes, 0);
    }
    ~JpgSaver()
    {

    }
    public void SaveJpg()
    {
        File.WriteAllBytes(jpgPath, jpgBytes);
    }
}


public class EnumFlag : PropertyAttribute
{

}

public enum PersonCountEnum
{
    p2 = 1,
    p3 = 2,
    p4 = 4,
    p5 = 8,
    p6 = 16
}
