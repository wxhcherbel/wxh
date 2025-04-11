using UnityEngine;
using UnityEngine.UI;
using System.IO;
using Newtonsoft.Json;
using TMPro;
using System.Collections.Generic;
using System.Collections;


public class MainMenuController : MonoBehaviour
{
    // 主逻辑中脚本实例化
    public MainLogic mainLogic;
    //随机地图实例化
    public MapGenerator mapGener;
    // 摄像头引用
    public Camera mainCamera;
    // UI按钮设置
    public Button importButton;
    public Button createButton;
    public Button generateButton;


    //确定按钮设置
    public Button ImportConfirm;
    public Button CreateConfirm;
      

    //返回上一级
    public Button ImportReturn;
    public Button CreateReturn;

    // 弹窗的引用
    public GameObject MainMenuPopup; //游戏主页弹窗
    public GameObject importPopup;   // 导入地图弹窗
    public GameObject createPopup;   // 创建地图弹窗

    public GameObject GamePopup;    //游戏内部ui

    // 导入地图时输入的地图路径
    public TMP_InputField mapNameInput; 

    // 创建地图输入框CreateMap
    public TMP_InputField lengthInput;
    public TMP_InputField widthInput;
    public TMP_InputField heightInput;
    public Slider Explorer;
    public Slider Worker;
    public Slider Defender;
    public Slider Hazard;
    public Slider Static;
    public Slider Resource;
    public Slider Target;

    public TMP_Text text1;
    public TMP_Text text2;
    public TMP_Text text3;
    public TMP_Text text4;
    public TMP_Text text5;
    public TMP_Text text6;
    public TMP_Text text7;
    public TMP_Dropdown MapRules;
    public TMP_Dropdown MapMaterial;

    void Start()
    {
        importButton.onClick.AddListener(OnImportButtonClicked);
        createButton.onClick.AddListener(OnCreateButtonClicked);
        generateButton.onClick.AddListener(OnGenerateButtonClicked);

        // 初始时，隐藏弹窗
        importPopup.SetActive(false);
        createPopup.SetActive(false);
        GamePopup.SetActive(false);
        

        ImportConfirm.onClick.AddListener(OnImportConfirmButtonClicked);
        CreateConfirm.onClick.AddListener(OnCreateConfirmButtonClicked);
        ImportReturn.onClick.AddListener(OnReturnStartButtonClicked);
        CreateReturn.onClick.AddListener(OnReturnStartButtonClicked);

        // 在开始时为每个Slider设置监听器，当数值变化时更新文本
        Explorer.onValueChanged.AddListener((value) => UpdateText(text1, Explorer));
        Worker.onValueChanged.AddListener((value) => UpdateText(text2, Worker));
        Defender.onValueChanged.AddListener((value) => UpdateText(text3, Defender));
        Hazard.onValueChanged.AddListener((value) => UpdateText(text4, Hazard));
        Static.onValueChanged.AddListener((value) => UpdateText(text5, Static));
        Resource.onValueChanged.AddListener((value) => UpdateText(text6, Resource));
        Target.onValueChanged.AddListener((value) => UpdateText(text7, Target));

        // 初始化文本显示
        UpdateText(text1, Explorer);
        UpdateText(text2, Worker);
        UpdateText(text3, Defender);
        UpdateText(text4, Hazard);
        UpdateText(text5, Static);
        UpdateText(text6, Resource);
        UpdateText(text7, Target);
    }

    // 更新文本显示的方法（使用 TMP_Text）
    void UpdateText(TMP_Text targetText, Slider slider)
    {
        targetText.text = $"{slider.value}/{slider.maxValue}";
    }

    //返回开始界面
    public void OnReturnStartButtonClicked()
    {
        Debug.Log("User : 用户点击返回开始界面按钮");
        importPopup.SetActive(false);
        createPopup.SetActive(false);
    }

    // 导入数字地图
    public void OnImportButtonClicked()
    {
        Debug.Log("User : 用户点击导入地图按钮");
        // 显示导入地图的弹窗
        importPopup.SetActive(true);
    }

    // 确定按钮：导入地图
    public void OnImportConfirmButtonClicked()
    {
        Debug.Log("User : 用户点击确定导入地图按钮");
        string mapName = mapNameInput.text;
        if (!string.IsNullOrEmpty(mapName))
        {
            string path = OpenFileDialog(mapName);
            Debug.Log("path = " + path);
            if (!string.IsNullOrEmpty(path))
            {
                Debug.Log("进入mainLogic.LoadModelsFromJson(jsonData);");
                mainLogic.LoadModelsFromJson(path);
            }
        }
        // 关闭弹窗
        importPopup.SetActive(false);
        MainMenuPopup.SetActive(false);
        // 对焦到地图
        FocusCameraOnMap();
        GamePopup.SetActive(true);
        //EnableDraggableObjects(false);
    }

    // 自动创建地图
    public void OnCreateButtonClicked()
    {
        Debug.Log("User : 用户点击自主创建地图按钮");
        // 显示创建地图的弹窗
        createPopup.SetActive(true);
    }

    // 确定按钮：自动创建地图
    public void OnCreateConfirmButtonClicked()
    {
        Debug.Log("User : 用户点击确定自主创建地图按钮");

        // 收集地图尺寸数据
        float length = float.Parse(lengthInput.text);
        float width = float.Parse(widthInput.text);
        float height = float.Parse(heightInput.text);

        // 收集地图规则和材质
        string mapRules = MapRules.options[MapRules.value].text;
        string mapMaterial = MapMaterial.options[MapMaterial.value].text;

        // 收集智能体和障碍物数量
        int explorerCount = (int)Explorer.value;
        int workerCount = (int)Worker.value;
        int defenderCount = (int)Defender.value;
        int hazardCount = (int)Hazard.value;
        int staticCount = (int)Static.value;
        int resourceCount = (int)Resource.value;
        int targetCount = (int)Target.value;

        // 创建 JSON 对象
        var mapData = new Dictionary<string, object>
        {
            // 地图尺寸
            ["mapLength"] = length,
            ["mapWidth"] = width,
            ["mapHeight"] = height,

            // 地图规则和材质
            ["MapRules"] = mapRules,
            ["MapMaterial"] = mapMaterial,

            // 智能体配置
            ["Agents"] = new Dictionary<string, int>
            {
                ["Explorer"] = explorerCount,
                ["Worker"] = workerCount,
                ["Defender"] = defenderCount
            },

            // 障碍物配置
            ["Obstacles"] = new Dictionary<string, int>
            {
                ["Hazard"] = hazardCount,
                ["Static"] = staticCount
            },

            // 资源点和目标点
            ["ResourcePoints"] = resourceCount,
            ["TargetPoints"] = targetCount
        };
        mapGener.GenerateMapFromData(mapData);
        mainLogic.LoadModelsFromJson(OpenFileDialog("mapData"));
        // 关闭弹窗
        createPopup.SetActive(false);
        MainMenuPopup.SetActive(false);
        FocusCameraOnMap();
        GamePopup.SetActive(true);
        
        //EnableDraggableObjects(false);
    }

    //随机创建地图
    public void OnGenerateButtonClicked()
    {
        Debug.Log("User : 用户点击随机创建地图按钮");
        //mapGener.GenerateMapFromData();
        mainLogic.LoadModelsFromJson(OpenFileDialog("mapData"));
        MainMenuPopup.SetActive(false);
        FocusCameraOnMap();
        GamePopup.SetActive(true);
        //EnableDraggableObjects(false);
    }
    // 弹出文件对话框，选择文件
    public string OpenFileDialog(string mapName)
    {
        // 假设用户可以手动输入文件路径，或者用 mapName 拼接文件名路径
        return "Assets/Resources/HistoryMap/" + mapName + ".json";
    }

    public void FocusCameraOnMap()
    {
        if (mainLogic == null || mainCamera == null) return;

        // 计算地图中心点
        Vector3 mapCenter = new Vector3(50, 30, 50);

        // 设置摄像头位置（在地图上方一定高度位置）
        float cameraHeight = Mathf.Max(mainLogic.mapLength, mainLogic.mapWidth) * 0.8f;
        mainCamera.transform.position = new Vector3(mapCenter.x, mapCenter.y, mapCenter.z);

        // 设置摄像头旋转
        // X轴上俯视45度，Y轴旋转180度，保证朝向地图
        mainCamera.transform.rotation = Quaternion.Euler(17f, -135f, 0f);

        // 调整摄像头视距（根据地图大小动态调整）
        if (mainCamera.orthographic)
        {
            mainCamera.orthographicSize = Mathf.Max(mainLogic.mapLength, mainLogic.mapWidth) / 2f;
        }
        else
        {
            mainCamera.fieldOfView = Mathf.Clamp(Mathf.Max(mainLogic.mapLength, mainLogic.mapWidth) * 1.5f, 30f, 90f);
        }

        Debug.Log("摄像头已对准地图中心");
    }





    private void EnableDraggableObjects(bool enable)
    {
        Debug.Log("启用或禁用该脚本");
        // 查找所有带有 "BarrierModel" 标签的对象
        GameObject[] modelsWithTag = GameObject.FindGameObjectsWithTag("BarrierModel");

        foreach (var model in modelsWithTag)
        {
            // 获取该物体上的 DraggableObject 脚本
            Debug.Log("识别到标签为BarrierModel的模型");
            DraggableObject draggableObject = model.GetComponent<DraggableObject>();
            if (draggableObject != null)
            {
                if(enable == false)
                    Debug.Log("禁用单个模型上的修改功能脚本");
                // 启用或禁用该脚本
                draggableObject.enabled = enable;
            }
        }
    }


}
