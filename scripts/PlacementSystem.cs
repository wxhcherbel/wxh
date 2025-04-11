using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class PlacementSystem : MonoBehaviour
{
    [SerializeField]
    GameObject mouseIndicator,cellIndicator;
    [SerializeField]
    private InputManager inputManager;
    [SerializeField]
    public Grid grid;
    [SerializeField] 
    private ObjectsDatabaseSO database;
    private int selectedObjectIndex = -1;
    [SerializeField]
    private GameObject gridVisualization;
    [SerializeField]
    public GridData agentData,floorData;

    private Renderer previewRenderer;

    public List<GameObject> placedGameObjects = new();
    public MainLogic mglogic;
    public Button ExitButton;

    private void Start()
    {
        StopPlacement();
        floorData = new GridData();
        agentData = new GridData(); 
        previewRenderer = cellIndicator.GetComponent<Renderer>();
        ExitButton.onClick.AddListener(StopPlacement);
    }
    public void SetGridSize(int ml, int mw)
    {
        // 通过地图大小动态设置网格大小
        float gridSizeX = ml /10f /2f;
        float gridSizeZ = mw /10f /2f;

        // 设置网格单元大小
        grid.cellSize = new Vector3(gridSizeX, 1, gridSizeZ);

        Debug.Log($"网格大小设置为: {gridSizeX} x {gridSizeZ}");
    }
    public void StartPlacement(int ID)
    {
        StopPlacement();
        selectedObjectIndex = database.objectsData.FindIndex(database => database.ID == ID);
        if(selectedObjectIndex < 0){
            Debug.LogError($"No ID Found {ID}");
            return;
        }
        gridVisualization.SetActive(true);
        cellIndicator.SetActive(true);
        inputManager.OnClicked += PlaceStructure;
        inputManager.OnExit += StopPlacement;
    }
    private void PlaceStructure()
    {
        Vector3 modelScale = new Vector3{ x = 1, y = 1, z = 1 };;
        if(inputManager.IsPointerOverUI()){
            return;
        }
        Vector3 mousePosition = inputManager.GetSelectedMapPostion();
        Vector3Int gridPosition = grid.WorldToCell(mousePosition);

        bool placementValidity = CheckPlacementValidity(gridPosition,selectedObjectIndex);
        if(placementValidity == false)return;
        //加入父结点并继承编号：
        // 创建或获取父节点
        string ObjectName = database.objectsData[selectedObjectIndex].Name;
        string parentNodeName = ObjectName + "Node";
        GameObject parentNode = GameObject.Find(parentNodeName) ?? new GameObject(parentNodeName);
        GameObject gameObject = Instantiate(database.objectsData[selectedObjectIndex].Prefab,parentNode.transform);
        
        //gameObject.transform.position = grid.CellToWorld(gridPosition) + new Vector3(2.3f,0f,4.3f);
        gameObject.transform.position = grid.CellToWorld(gridPosition) + grid.cellSize / 2f;


        // 获取当前模型类型的计数器
        if (!mglogic.modelTypeCounters.ContainsKey(ObjectName))
        {
            mglogic.modelTypeCounters[ObjectName] = 1; // 如果没有该类型的模型，计数器从1开始
        }
        else
        {
            mglogic.modelTypeCounters[ObjectName]++; // 如果有，递增计数器
        }

        // 根据计数器创建唯一名称
        string instanceName = $"{ObjectName}{mglogic.modelTypeCounters[ObjectName]}";

        //Explorer
        if (selectedObjectIndex == 0)
        {
            modelScale = new Vector3{ x = 2, y = 2, z = 2 };
            gameObject.tag = "Explorer"; // 根据需求设置模型的标签
        }
        //Worker
        else if(selectedObjectIndex == 1)
        {
            modelScale = new Vector3{ x = 4, y = 3, z = 4 };
            gameObject.tag = "Worker"; // 根据需求设置模型的标签
        }
        //Static
        else if(selectedObjectIndex == 2)
        {
            modelScale = new Vector3{ x = 120, y = 50, z = 120 };
            gameObject.tag = "Static"; // 根据需求设置模型的标签
            gameObject.transform.position += new Vector3(0f,1.7f,0f);
        }
        //Hazard
        else if(selectedObjectIndex == 3)
        {
            modelScale = new Vector3{ x = 120, y = 120, z = 100 };
            gameObject.tag = "Hazard"; // 根据需求设置模型的标签
            gameObject.transform.position += new Vector3(0f,1.5f,0f);
        }
        //Resource
        else if(selectedObjectIndex == 4)
        {
            modelScale = new Vector3{ x = 1, y = 1, z = 1 };
            gameObject.tag = "Resource"; // 根据需求设置模型的标签
        }
        //Target
        else if(selectedObjectIndex == 5)
        {
            modelScale = new Vector3{ x = 0.5f, y = 0.5f, z = 0.5f };
            gameObject.tag = "Target"; // 根据需求设置模型的标签
            gameObject.transform.position += new Vector3(0f,4f,0f);
        }
        gameObject.transform.localScale = modelScale;
        gameObject.name = instanceName; // 设置模型的名称
        

        placedGameObjects.Add(gameObject);
        GridData selectedData = database.objectsData[selectedObjectIndex].ID == 1 ? 
           agentData: 
           floorData;
        selectedData.AddObjectAt(gridPosition,
                                database.objectsData[selectedObjectIndex].Size,
                                database.objectsData[selectedObjectIndex].ID,
                                placedGameObjects.Count - 1);
    }
    private void StopPlacement()
    {
        selectedObjectIndex = -1;
        gridVisualization.SetActive(false);
        cellIndicator.SetActive(false);
        inputManager.OnClicked -= PlaceStructure;
        inputManager.OnExit -= StopPlacement;
    }
    
    private bool CheckPlacementValidity(Vector3Int gridPosition, int selectedObjectIndex)
    {
       GridData selectedData = database.objectsData[selectedObjectIndex].ID == 1 ? 
           agentData: 
           floorData;

       return selectedData.CanPlaceObejctAt(gridPosition, database.objectsData[selectedObjectIndex].Size);
    }
    private void Update()
    {
        if(selectedObjectIndex < 0){
            return;
        }
        Vector3 mousePosition = inputManager.GetSelectedMapPostion();
        Vector3Int gridPosition = grid.WorldToCell(mousePosition);

        bool placementValidity = CheckPlacementValidity(gridPosition,selectedObjectIndex);
        previewRenderer.material.color = placementValidity? Color.white : Color.red;

        mouseIndicator.transform.position = mousePosition;
        //cellIndicator.transform.position = grid.CellToWorld(gridPosition) + new Vector3(2.3f,0.3f,4.3f);
        cellIndicator.transform.position = grid.CellToWorld(gridPosition) + grid.cellSize / 2f + new Vector3(0f,-0.4f,0f);
    }
}
