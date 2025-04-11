using UnityEngine;
using UnityEngine.UI;
using System.IO;
using Newtonsoft.Json;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class GameLogic : MonoBehaviour
{
    // 主逻辑中脚本实例化
    public MainLogic mainLogic;
    //随机地图实例化
    public MapGenerator mapGener;
    // 摄像头引用
    public Camera mainCamera;

    public GameObject MainMenuPopup;

    public GameObject PlaceObjPopup;

    //---------------------------游戏UI界面元素---------------------------
    public Button HomeButton;
    public Button RefreshButton;
    public Button SaveButton;
    public TMP_Dropdown SenceState;
    public TMP_Dropdown direction;
    public GameObject SaveMapPop; 
    public GameObject ObjectInfo; 
    public TMP_InputField SaveMapName;
    public Button SaveComfirmButton;
    public Button SaveReturnButton;

    // Start is called before the first frame update
    void Start()
    {
        HomeButton.onClick.AddListener(OnHomeButtonClicked);
        RefreshButton.onClick.AddListener(OnRefreshButtonClicked);
        SaveButton.onClick.AddListener(OnSaveButtonClicked);

        SenceState.onValueChanged.AddListener(OnSenceStateChanged);
        direction.onValueChanged.AddListener(OnDirectionChanged);

        SaveComfirmButton.onClick.AddListener(OnSaveComfirmButtonClicked);
        SaveReturnButton.onClick.AddListener(OnSaveReturnButtonClicked);

        SaveMapPop.SetActive(false);
        PlaceObjPopup.SetActive(false);
        ObjectInfo.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
        //游戏UI界面相关函数
    public void OnHomeButtonClicked()
    {
        DestroyAllModelsWithTags(new string[] { "Obstacle", "GamePlane" });
        //ClearMapDataJson();
        //this.GetComponent<Renderer>().enabled = false;
        MainMenuPopup.SetActive(true);
    }
    public void OnRefreshButtonClicked()
    {
        mainCamera.transform.position = new Vector3(50, 30, -50); 
        mainCamera.transform.rotation = Quaternion.Euler(19f, -50f, 0f); 
    }

    public void OnSaveButtonClicked()
    {
        //保存场景中的模型成json格式。
        SaveMapPop.SetActive(true);
    }
    public void OnSaveComfirmButtonClicked()
    {
        string mapName = SaveMapName.text;
        if (string.IsNullOrEmpty(mapName))
        {
            Debug.LogWarning("请输入保存的地图名称！");
            return;
        }
        // 创建一个包含地图信息的对象
        MapData mapData = new MapData();
        mapData.mapLength = mainLogic.mapLength;
        mapData.mapWidth = mainLogic.mapWidth;
        mapData.mapHeight = mainLogic.mapHeight;
        mapData.models = new List<ModelData>();

        // 获取所有的树节点
        Transform treeParent = GameObject.Find("TreeNode").transform;
        foreach (Transform child in treeParent)
        {
            ModelData modelData = new ModelData
            {
                name = "Tree",
                position = new Position { x = child.position.x, y = child.position.y, z = child.position.z },
                rotation = new Rotation { x = child.rotation.eulerAngles.x, y = child.rotation.eulerAngles.y, z = child.rotation.eulerAngles.z },
                scale = new Scale { x = child.localScale.x, y = child.localScale.y, z = child.localScale.z }
            };
            mapData.models.Add(modelData);
        }

        // 获取所有的 Agent 节点
        Transform agentParent = GameObject.Find("AgentNode").transform;
        foreach (Transform child in agentParent)
        {
            ModelData modelData = new ModelData
            {
                name = "Agent",
                position = new Position { x = child.position.x, y = child.position.y, z = child.position.z },
                rotation = new Rotation { x = child.rotation.eulerAngles.x, y = child.rotation.eulerAngles.y, z = child.rotation.eulerAngles.z },
                scale = new Scale { x = child.localScale.x, y = child.localScale.y, z = child.localScale.z }
            };
            mapData.models.Add(modelData);
        }

        // 获取所有的 Barrier 节点
        Transform barrierParent = GameObject.Find("BarrierNode").transform;
        foreach (Transform child in barrierParent)
        {
            ModelData modelData = new ModelData
            {
                name = "Barrier",
                position = new Position { x = child.position.x, y = child.position.y, z = child.position.z },
                rotation = new Rotation { x = child.rotation.eulerAngles.x, y = child.rotation.eulerAngles.y, z = child.rotation.eulerAngles.z },
                scale = new Scale { x = child.localScale.x, y = child.localScale.y, z = child.localScale.z }
            };
            mapData.models.Add(modelData);
        }

        // 将地图数据转换为 JSON 格式
        string json = JsonConvert.SerializeObject(mapData, Formatting.Indented);

        // 保存到文件
        string filePath = "Assets/Resources/HistoryMap/" + mapName + ".json";
        File.WriteAllText(filePath, json);
        Debug.Log("地图数据已保存到：" + filePath);
        SaveMapPop.SetActive(false);
    }
    public void OnSaveReturnButtonClicked()
    {
         SaveMapPop.SetActive(false);
    }
    private void DestroyAllModelsWithTags(string[] tags)
    {
        foreach (string tag in tags)
        {
            // 查找所有带有指定标签的模型
            GameObject[] modelsWithTag = GameObject.FindGameObjectsWithTag(tag);

            foreach (var model in modelsWithTag)
            {
                Destroy(model); // 销毁模型
            }
        }
    }

    private void ClearMapDataJson()
    {
        string filePath = "Assets/Resources/Models/mapData.json";
        
        if (File.Exists(filePath))
        {
            // 清空文件内容
            File.WriteAllText(filePath, string.Empty);
            Debug.Log("mapData.json 文件内容已清空");
            mapGener.models.Clear(); 
            Debug.Log("模型列表已清空");
        }
        else
        {
            Debug.LogError("mapData.json 文件未找到");
        }
    }
    
    //启用/禁用脚本
    public void OnSenceStateChanged(int index)
    {
        // 获取下拉框的值
        string selectedState = SenceState.options[index].text;
        GameObject plane = GameObject.Find("Plane");
        Renderer renderer = plane.GetComponent<Renderer>();

        if (selectedState == "Edit")
        {
            Debug.Log("SenceStateChanged Edit");
            
            PlaceObjPopup.SetActive(true);
            //EnableDraggableObjects(true);
            ObjectInfo.SetActive(true);
            if (renderer != null)
            {
                Material gridEditMaterial = Resources.Load<Material>("Shaders/GridEdit");
                renderer.material = gridEditMaterial;
            }
        }
        else if (selectedState == "Run")
        {
            Debug.Log("SenceStateChanged Run");
            PlaceObjPopup.SetActive(false);
            ObjectInfo.SetActive(false);
            //EnableDraggableObjects(false);
            if (renderer != null)
            {
                renderer.material = Resources.Load<Material>("default");
                renderer.material.color = Color.yellow; // 恢复默认的黄色材质
            }
        }
    }

    // private void EnableDraggableObjects(bool enable)
    // {
    //     Debug.Log("启用或禁用该脚本");
    //     // 查找所有带有 "Obstacle" 标签的对象
    //     GameObject[] modelsWithTag = GameObject.FindGameObjectsWithTag("Static");

    //     foreach (var model in modelsWithTag)
    //     {
    //         // 获取该物体上的 DraggableObject 脚本
    //         Debug.Log("识别到标签为BarrierModel的模型");
    //         DraggableObject draggableObject = model.GetComponent<DraggableObject>();
    //         if (draggableObject != null)
    //         {
    //             if(enable == false)
    //                 Debug.Log("禁用单个模型上的修改功能脚本");
    //             // 启用或禁用该脚本
    //             draggableObject.enabled = enable;
    //         }
    //     }
    // }

    public void OnDirectionChanged(int index)
    {
        // 获取下拉框的值
        string selectedState = direction.options[index].text;
        
        // 根据选择的方向调整摄像头位置和朝向
        if (selectedState == "xy")
        {
            // 设置摄像头位置
            mainCamera.transform.position = new Vector3(0, 0, 50); // 假设你希望摄像头能覆盖地图
            mainCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // 朝向xy平面
        }
        else if (selectedState == "xz")
        {
            // 设置摄像头位置
            mainCamera.transform.position = new Vector3(0, 30, 0); // 假设摄像头位置位于xz平面
            mainCamera.transform.rotation = Quaternion.Euler(100f, 84f, 84f); // 朝向xz平面
        }
        else if (selectedState == "yz")
        {
            // 设置摄像头位置
            mainCamera.transform.position = new Vector3(50 , 0, 0); // 假设摄像头位置位于yz平面
            mainCamera.transform.rotation = Quaternion.Euler(0f, 0f, 0f); // 朝向yz平面
        }
        else
            OnRefreshButtonClicked();
    }

    [System.Serializable]
    public class MapData
    {
        public float mapLength;
        public float mapWidth;
        public float mapHeight;
        public List<ModelData> models;
    }

    [System.Serializable]
    public class ModelData
    {
        public string name;
        public Position position;
        public Rotation rotation;
        public Scale scale;
    }
    public class Position
    {
        public float x;
        public float y;
        public float z;
    }

    [System.Serializable]
    public class Rotation
    {
        public float x;
        public float y;
        public float z;
    }

    [System.Serializable]
    public class Scale
    {
        public float x;
        public float y;
        public float z;
    }

}


