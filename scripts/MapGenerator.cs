using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

public class MapGenerator : MonoBehaviour
{
    public PlacementSystem plasys;
    public int PlacedNum = 0;

    // 存储生成的模型
    public List<Model> models = new List<Model>();

    // 地图规则和材质
    private Dictionary<string, object> mapData;

    // 地图尺寸（提取为类成员变量）
    private int mapLength;
    private int mapWidth;
    private int mapHeight;

    // 模型类型
    private enum ModelType { Agent, Obstacle, Target, Resource }

    public void GenerateMapFromData(Dictionary<string, object> inputData)
    {
        mapLength = int.Parse(inputData["mapLength"].ToString());
        mapWidth = int.Parse(inputData["mapWidth"].ToString());
        mapHeight = int.Parse(inputData["mapHeight"].ToString());

        //plasys.SetGridSize(mapLength,mapWidth);

        string mapRules = inputData.ContainsKey("MapRules") ? inputData["MapRules"].ToString() : "Default";
        string mapMaterial = inputData.ContainsKey("MapMaterial") ? inputData["MapMaterial"].ToString() : "Default";

        models.Clear(); // 清空现有模型

        // ✅ 生成Agent
        if (inputData.ContainsKey("Agents"))
        {
            Dictionary<string, int> agentData = inputData["Agents"] as Dictionary<string, int>;
            if (agentData != null)
            {
                GenerateModels(ModelType.Agent, agentData);
            }
        }

        // ✅ 生成Obstacle
        if (inputData.ContainsKey("Obstacles"))
        {
            Dictionary<string, int> obstacleData = inputData["Obstacles"] as Dictionary<string, int>;
            if (obstacleData != null)
            {
                GenerateModels(ModelType.Obstacle, obstacleData);
            }
        }

        // ✅ 生成ResourcePoints
        if (inputData.ContainsKey("ResourcePoints"))
        {
            int resourceCount = int.Parse(inputData["ResourcePoints"].ToString());
            GenerateModels(ModelType.Resource, resourceCount);
        }

        // ✅ 生成TargetPoints
        if (inputData.ContainsKey("TargetPoints"))
        {
            int targetCount = int.Parse(inputData["TargetPoints"].ToString());
            GenerateModels(ModelType.Target, targetCount);
        }

        // ✅ 保存为 JSON
        SaveToJson(mapRules, mapMaterial);
    }

    // 🏆 生成模型（合并Agent和Obstacle的生成逻辑）
    void GenerateModels(ModelType modelType, Dictionary<string, int> modelData)
    {
        foreach (var data in modelData)
        {
            int count = data.Value;
            string function = data.Key;

            for (int i = 0; i < count; i++)
            {
                if (!TryPlaceModel(modelType, function))
                {
                    Debug.LogWarning($"生成 {modelType} 失败，超过最大尝试次数");
                }
            }
        }
    }

    void GenerateModels(ModelType modelType, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (!TryPlaceModel(modelType, modelType.ToString()))
            {
                Debug.LogWarning($"生成 {modelType} 失败，超过最大尝试次数");
            }
        }
    }

    // ✅ 生成单个模型（合并Agent和Obstacle逻辑）
    bool TryPlaceModel(ModelType modelType, string function)
    {
        Vector3 position;
        Vector3Int gridPosition;
        Vector2Int objectSize = new Vector2Int(1, 1);
        int maxAttempts = 20;
        int attempts = 0;

        do
        {
            position = GenerateRandomPosition(function);
            gridPosition = plasys.grid.WorldToCell(position);
            attempts++;

            // 如果达到最大尝试次数，返回false
            if (attempts >= maxAttempts) return false;

        } while (!plasys.floorData.CanPlaceObejctAt(gridPosition, objectSize) || !IsValidPosition(position));
        position = plasys.grid.CellToWorld(gridPosition) + plasys.grid.cellSize / 2f;
        PlacedNum++;
        float rotation_x = 0;
        if(modelType == ModelType.Obstacle)rotation_x = -90;
        int ID = 0;
        Scale mScale = new Scale { x = 1, y = 1, z = 1 }; // 默认值
        if(function == "Target"){
            mScale = new Scale { x = 0.5f, y = 0.5f, z = 0.5f };
            position.y = 4;
            ID = 5;
        }
        else if(function == "Worker"){
            mScale = new Scale { x = 4, y = 3, z = 4 };
            ID = 1;
        }
        else if(function == "Static"){
            mScale = new Scale { x = 120, y = 50, z = 120 };
            position.y = 1.7f;
            ID = 2;
        }
        else if(function == "Resource"){
            mScale = new Scale { x = 1, y = 1, z = 1 };
            position.y = 0;
            ID = 4;
        }
        else if(function == "Hazard"){
            mScale = new Scale { x = 120, y = 120, z = 100 };
            position.y = 1.5f;
            ID = 3;
        }
        else if(function == "Explorer"){
            mScale = new Scale { x = 2, y = 2, z = 2 };
            position.y = -0.5f;
            ID = 0;
        }

        models.Add(new Model
        {
            type = modelType.ToString(),
            function = function,
            position = new Position { x = position.x, y = position.y, z = position.z },
            rotation = new Rotation { x = rotation_x, y = 0f, z = 0f },
            scale = mScale
        });
        if(ID != 1)plasys.floorData.AddObjectAt(gridPosition,objectSize,ID,PlacedNum-1);
        return true;
    }

    // ✅ 获取随机位置（根据地图大小）
    Vector3 GenerateRandomPosition(string function)
    {
        float x = Random.Range(-mapLength / 2f, mapLength / 2f);
        float z = Random.Range(-mapWidth / 2f, mapWidth / 2f);
        float y = (function == "Worker") ? Random.Range(5f, mapHeight/2f) : 0f;

        return new Vector3(x, y, z);
    }

    // ✅ 检查位置是否合法（避免重叠）
    bool IsValidPosition(Vector3 position)
    {
        float minDistance = 2.0f; // 允许的最小距离
        foreach (var model in models)
        {
            Vector3 existingPosition = new Vector3(model.position.x, model.position.y, model.position.z);
            if (Vector3.Distance(existingPosition, position) < minDistance)
            {
                return false;
            }
        }
        return true;
    }

    // ✅ 保存JSON
    void SaveToJson(string mapRules, string mapMaterial)
    {
        var mapData = new MapData
        {
            mapLength = mapLength,
            mapWidth = mapWidth,
            mapHeight = mapHeight,
            mapRules = mapRules,
            mapMaterial = mapMaterial,
            models = models,
        };

        string json = JsonConvert.SerializeObject(mapData, Formatting.Indented);
        string path = Path.Combine(Application.dataPath, "Resources/HistoryMap", "mapData.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, json);

        Debug.Log($"地图数据已保存至：{path}");
    }
}



// ✅ 地图结构体
[System.Serializable]
public class MapData
{
    public int mapLength;
    public int mapWidth;
    public int mapHeight;
    public string mapRules;
    public string mapMaterial;
    public List<Model> models;
}

[System.Serializable]
public class Model
{
    public string type;
    public string function;
    public Position position;
    public Rotation rotation;
    public Scale scale;
}

[System.Serializable]
public class Position { public float x, y, z; }
[System.Serializable]
public class Rotation { public float x, y, z; }
[System.Serializable]
public class Scale { public float x, y, z; }
