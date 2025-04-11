using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System;


public class MainLogic : MonoBehaviour
{
    public static event Action OnResourcesInitialized; // 事件通知
    public PlacementSystem plsys;
    // 地图的长宽高
    public float mapLength;
    public float mapHeight;
    public float mapWidth;

    [System.Serializable]
    public class Model
    {
        public string type;
        public string function;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
    }

    [System.Serializable]
    public class MapData
    {
        public float mapLength;
        public float mapHeight;
        public float mapWidth;
        public string mapRules;
        public string mapMaterial;
        public List<Model> models;
    }
    public Dictionary<string, int> modelTypeCounters = new Dictionary<string, int>();
    private void Awake()
    {
        Debug.Log("------------Main Awake------------");
    }

    void Start()
    {
        Debug.Log("------------Main Start------------");
    }

    void Update()
    {
    }

    public void LoadModelsFromJson(string path)
    {
        Debug.Log("----Function LoadModelsFromJson----");
        string json = File.ReadAllText(path);
        // 解析整个 JSON 为 MapData 对象
        MapData mapData = JsonConvert.DeserializeObject<MapData>(json);

        this.mapWidth = mapData.mapWidth;
        this.mapHeight = mapData.mapHeight;
        this.mapLength = mapData.mapLength;
        Debug.Log($"地图长度: {this.mapLength}, 地图宽度: {this.mapWidth}, 地图高度: {this.mapHeight}");

        CreateMapPlane(this.mapLength, this.mapWidth);

        // 通过预制体加载模型
        foreach (var model in mapData.models)
        {
            string prefabPath = $"Prefabs/{model.function}";
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab != null)
            {
                // 创建或获取父节点
                string parentNodeName = model.function + "Node";
                GameObject parentNode = GameObject.Find(parentNodeName) ?? new GameObject(parentNodeName);

                // 获取当前模型类型的计数器
                if (!modelTypeCounters.ContainsKey(model.function))
                {
                    modelTypeCounters[model.function] = 1; // 如果没有该类型的模型，计数器从1开始
                }
                else
                {
                    modelTypeCounters[model.function]++; // 如果有，递增计数器
                }

                // 根据计数器创建唯一名称
                string instanceName = $"{model.function}{modelTypeCounters[model.function]}";

                // 实例化模型并设置父节点
                GameObject instance = Instantiate(prefab, model.position, Quaternion.Euler(model.rotation), parentNode.transform);
                instance.transform.localScale = model.scale;
                instance.name = instanceName; // 设置模型的名称
                instance.tag = model.function; // 根据需求设置模型的标签
                //instance.layer = LayerMask.NameToLayer("Placement");
                plsys.placedGameObjects.Add(instance);
            }
            else
            {
                Debug.LogError($"无法找到名为 {model.function} 的预制体");
            }
        }
        OnResourcesInitialized?.Invoke(); // 触发事件，通知 ExplorerAgent
    }


    public void CreateMapPlane(float length, float width)
    {
        // 创建平面
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.position = new Vector3(0, 0, 0);
        plane.transform.localScale = new Vector3(length / 10f, 1, width / 10f);
        Renderer renderer = plane.GetComponent<Renderer>();
        renderer.material.color = Color.yellow;
        plane.tag = "GamePlane";
        plane.layer = LayerMask.NameToLayer("Placement");

        // 创建平面
        GameObject grass = GameObject.CreatePrimitive(PrimitiveType.Plane);
        grass.transform.position = new Vector3(0, -0.1f, 0);
        grass.transform.localScale = new Vector3(length / 2f,1, width / 2f);
        renderer = grass.GetComponent<Renderer>();
        renderer.material.color = new Color(0.56f, 0.93f, 0.56f);  // 类似 spring green / light green
        grass.tag = "GamePlane";

        // 创建四个墙壁

        float wallHeight = 2f; // 自定义墙壁高度

        // 上墙
        GameObject topWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        topWall.transform.position = new Vector3(0, wallHeight / 2f, width / 2f);
        topWall.transform.localScale = new Vector3(length, wallHeight, 1);
        topWall.GetComponent<Renderer>().enabled = false;
        topWall.AddComponent<BoxCollider>();
        topWall.tag = "GamePlane";

        // 下墙
        GameObject bottomWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bottomWall.transform.position = new Vector3(0, wallHeight / 2f, -width / 2f);
        bottomWall.transform.localScale = new Vector3(length, wallHeight, 1);
        bottomWall.GetComponent<Renderer>().enabled = false;
        bottomWall.AddComponent<BoxCollider>();
        bottomWall.tag = "GamePlane";

        // 左墙
        GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWall.transform.position = new Vector3(-length / 2f, wallHeight / 2f, 0);
        leftWall.transform.localScale = new Vector3(1, wallHeight, width);
        leftWall.GetComponent<Renderer>().enabled = false;
        leftWall.AddComponent<BoxCollider>();
        leftWall.tag = "GamePlane";

        // 右墙
        GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWall.transform.position = new Vector3(length / 2f, wallHeight / 2f, 0);
        rightWall.transform.localScale = new Vector3(1, wallHeight, width);
        rightWall.GetComponent<Renderer>().enabled = false;
        rightWall.AddComponent<BoxCollider>();
        rightWall.tag = "GamePlane";

    }



}
