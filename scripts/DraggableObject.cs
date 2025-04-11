using UnityEngine;
using UnityEngine.UI;  
using TMPro;

public class DraggableObject : MonoBehaviour
{
    public Camera mainCamera;
    private Vector3 offset;
    private float mouseZCoord;

    public MainLogic mainLogic;
    private string objectName = null;
    private string place = null;

    // 缩放和旋转的增量
    public float rotationSpeed = 100f;
    public float scaleSpeed = 0.5f;

    private string isSelected = null;  // 用于标记选中物体的名字

    private Vector3 initialScale;  // 存储初始缩放大小

    // UI 相关字段
    public TMP_InputField xInputField;
    public TMP_InputField yInputField;
    public TMP_InputField zInputField;
    public TMP_InputField xrotation;
    public TMP_InputField yrotation;
    public TMP_InputField zrotation;
    public TMP_InputField xsclae;
    public TMP_InputField ysclae;
    public TMP_InputField zsclae;

    public Toggle selectionToggle;
    public Button DeleteButton;

    void Start()
    {
        // 自动绑定主摄像头，如果没有手动设置的话
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("没有找到主摄像头，请确保场景中有一个标记为 'MainCamera' 的摄像头！");
            }
        }

        // 获取 MainLogic 对象
        if (mainLogic == null)
        {
            mainLogic = FindObjectOfType<MainLogic>();
        }

        if (mainLogic == null)
        {
            Debug.LogError("没有找到 MainLogic 对象！请确保场景中有一个 MainLogic 对象。");
        }
     
        // 确保物体有 Collider（例如 BoxCollider）
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }

        // 获取物体的名字，便于后续判断
        objectName = gameObject.transform.parent.name;

        // 存储物体的初始缩放大小
        initialScale = transform.localScale;

        // 自动绑定UI输入框，如果没有手动设置的话
        if (xInputField == null)
        {
            xInputField = GameObject.Find("XInputField")?.GetComponent<TMP_InputField>();
            if (xInputField == null) Debug.LogError("没有找到名为 XInputField 的输入框！");
        }

        if (yInputField == null)
        {
            yInputField = GameObject.Find("YInputField")?.GetComponent<TMP_InputField>();
            if (yInputField == null) Debug.LogError("没有找到名为 YInputField 的输入框！");
        }

        if (zInputField == null)
        {
            zInputField = GameObject.Find("ZInputField")?.GetComponent<TMP_InputField>();
            if (zInputField == null) Debug.LogError("没有找到名为 ZInputField 的输入框！");
        }
        if (xrotation == null)
        {
            xrotation = GameObject.Find("Xrotation")?.GetComponent<TMP_InputField>();
            if (xrotation == null) Debug.LogError("没有找到名为 XRotationInputField 的输入框！");
        }

        if (yrotation == null)
        {
            yrotation = GameObject.Find("Yrotation")?.GetComponent<TMP_InputField>();
            if (yrotation == null) Debug.LogError("没有找到名为 YRotationInputField 的输入框！");
        }

        if (zrotation == null)
        {
            zrotation = GameObject.Find("Zrotation")?.GetComponent<TMP_InputField>();
            if (zrotation == null) Debug.LogError("没有找到名为 ZRotationInputField 的输入框！");
        }

        if (xsclae == null)
        {
            xsclae = GameObject.Find("Xscale")?.GetComponent<TMP_InputField>();
            if (xsclae == null) Debug.LogError("没有找到名为 XScaleInputField 的输入框！");
        }

        if (ysclae == null)
        {
            ysclae = GameObject.Find("Yscale")?.GetComponent<TMP_InputField>();
            if (ysclae == null) Debug.LogError("没有找到名为 YScaleInputField 的输入框！");
        }

        if (zsclae == null)
        {
            zsclae = GameObject.Find("Zscale")?.GetComponent<TMP_InputField>();
            if (zsclae == null) Debug.LogError("没有找到名为 ZScaleInputField 的输入框！");
        }

        if(DeleteButton == null)
        {
            DeleteButton =  GameObject.Find("DeleteButton")?.GetComponent<Button>();
        }
        xInputField.onEndEdit.AddListener(delegate { OnPositionInputChanged(); });
        yInputField.onEndEdit.AddListener(delegate { OnPositionInputChanged(); });
        zInputField.onEndEdit.AddListener(delegate { OnPositionInputChanged(); });
        
        xrotation.onEndEdit.AddListener(delegate { OnRotationOrScaleInputChanged(); });
        yrotation.onEndEdit.AddListener(delegate { OnRotationOrScaleInputChanged(); });
        zrotation.onEndEdit.AddListener(delegate { OnRotationOrScaleInputChanged(); });

        xsclae.onEndEdit.AddListener(delegate { OnRotationOrScaleInputChanged(); });
        ysclae.onEndEdit.AddListener(delegate { OnRotationOrScaleInputChanged(); });
        zsclae.onEndEdit.AddListener(delegate { OnRotationOrScaleInputChanged(); });

        DeleteButton.onClick.AddListener(OnDeleteButtonClicked);

        if (selectionToggle == null)
        {
            selectionToggle = GameObject.Find("checked")?.GetComponent<Toggle>();
            if (selectionToggle == null) Debug.LogError("没有找到名为 checked 的 Toggle！");
        }
        // 监听Toggle状态变化
        selectionToggle.onValueChanged.AddListener(OnToggleChanged);
        selectionToggle.isOn = false;
    }

    private void DrawOutline() 
    { // 绘制描边
        Debug.Log("DrawOutline()..");
        if (gameObject.GetComponent<OutlineEffect>() == null) {
            Debug.Log("DrawOutline()添加..");
            gameObject.AddComponent<OutlineEffect>();
        } else {
            Debug.Log("DrawOutline()使能..");
            gameObject.GetComponent<OutlineEffect>().enabled = true;
        }
    }
    private void DeleteOutline() 
    {
        Debug.Log("DeleteOutline()..");
        if ( gameObject.GetComponent<OutlineEffect>() != null) {
            Debug.Log("DrawOutline()禁用..");
            gameObject.GetComponent<OutlineEffect>().enabled = false;
        }
    }
    void OnMouseDown()
    {
        if (!enabled) return;
        if(isSelected != null) return;
        // 禁用摄像头的拖动控制
        EnableCameraDragController(false);

        // 高亮显示被点击的物体
        DrawOutline();
        mouseZCoord = mainCamera.WorldToScreenPoint(transform.position).z;
        offset = transform.position - GetMouseWorldPos();

        isSelected = gameObject.transform.name;  // 标记为选中状态
        if(place == null && !selectionToggle.isOn) 
        {
            place = gameObject.transform.name;
            selectionToggle.isOn = true;  // 使 Toggle 变为选中
        }
        else UpdatePositionInputs();
    }

    void OnMouseDrag()
    {
        if (!enabled) return;
        if (isSelected == null)return;
        // 禁用摄像头的拖动控制
        EnableCameraDragController(false);

        Vector3 targetPosition = GetMouseWorldPos() + offset;

        // 限制物体的x、z坐标范围
        targetPosition.x = Mathf.Clamp(targetPosition.x, -mainLogic.mapLength / 2, mainLogic.mapLength / 2);
        targetPosition.z = Mathf.Clamp(targetPosition.z, -mainLogic.mapWidth / 2, mainLogic.mapWidth / 2);

        // 根据物体类型进行不同的y轴限制
        if (objectName == "TreeNode")
        {
            targetPosition.y = 0;  // 限制在y=0平面上
        }
        else if(objectName == "BarrierNode")
        {
            // 对Barrier，根据缩放比例调整y位置，保证底部不变
            targetPosition.y = 2.6f + (transform.localScale.y - initialScale.y) / 2;
        }
        else if (objectName == "AgentNode")
        {
            // 对Agent，我们允许修改y坐标
            targetPosition.y = Mathf.Max(0, targetPosition.y);  // 防止y小于0
        }

        transform.position = targetPosition;

        // 更新UI中的位置
        UpdatePositionInputs();
    }

    void OnMouseUp()
    {
        if (!enabled) return;
        if(isSelected == null)return;
        isSelected = null;
        DeleteOutline();
         // 恢复摄像头的拖动控制
        EnableCameraDragController(true);
    }
    public void OnToggleChanged(bool isOn)
    {
        if (isOn != true)
        {
            place = null;
        }
    }
    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = mouseZCoord;
        return mainCamera.ScreenToWorldPoint(mousePoint);
    }

    private void EnableCameraDragController(bool enable)
    {
        // 获取摄像头上的 CameraDragController 脚本
        CameraDragController cameraDragController = mainCamera.GetComponent<CameraDragController>();
        if (cameraDragController != null)
        {
            // 启用或禁用该脚本
            cameraDragController.enabled = enable;
        }
    }

    void Update()
    {
        if (!enabled) return;
        // 只有选中的物体才能进行缩放和旋转操作
        if (isSelected == null)return;
        // 长按A和D进行物体旋转
        if (Input.GetKey(KeyCode.A))
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime); // 向左旋转
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime); // 向右旋转
        }

        // 长按W和S进行物体缩放
        if (Input.GetKey(KeyCode.W))
        {
            transform.localScale += Vector3.one * scaleSpeed * Time.deltaTime; // 放大物体
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.localScale -= Vector3.one * scaleSpeed * Time.deltaTime; // 缩小物体
        }

        // 保证树木和障碍物的底部位置
        if (objectName == "TreeNode")
        {
            // 放大缩小时，保持物体的y坐标始终为0
            Vector3 newPosition = transform.position;
            newPosition.y = 0;
            transform.position = newPosition;
        }
        else if (objectName == "BarrierNode")
        {
            // Barrier 放大时，调整y坐标，保持底部不变
            Vector3 newPosition = transform.position;
            newPosition.y = 2.6f + (transform.localScale.y - initialScale.y) / 2;
            transform.position = newPosition;
        }
    }

    // 更新UI中的位置输入框
    void UpdatePositionInputs()
    {
        if (xInputField != null && yInputField != null && zInputField != null)
        {
            xInputField.text = transform.position.x.ToString("F2");
            yInputField.text = transform.position.y.ToString("F2");
            zInputField.text = transform.position.z.ToString("F2");
        }
        // 更新旋转输入框
        if (xrotation != null && yrotation != null && zrotation != null)
        {
            xrotation.text = transform.rotation.eulerAngles.x.ToString("F2");
            yrotation.text = transform.rotation.eulerAngles.y.ToString("F2");
            zrotation.text = transform.rotation.eulerAngles.z.ToString("F2");
        }

        // 更新缩放输入框
        if (xsclae != null && ysclae != null && zsclae != null)
        {
            xsclae.text = transform.localScale.x.ToString("F2");
            ysclae.text = transform.localScale.y.ToString("F2");
            zsclae.text = transform.localScale.z.ToString("F2");
        }
    }

    // 监听输入框中的变化并更新物体位置
    public void OnPositionInputChanged()
    {
        if (place == null)return;

        // 获取输入框的值并更新物体位置
        float x = float.Parse(xInputField.text);
        float y = float.Parse(yInputField.text);
        float z = float.Parse(zInputField.text);

        transform.position = new Vector3(x, y, z);
    }

    // 监听旋转和缩放输入框的变化
    public void OnRotationOrScaleInputChanged()
    {
        if (place == null) return;

        // 获取旋转值并更新物体旋转
        float xRotation = float.Parse(xrotation.text);
        float yRotation = float.Parse(yrotation.text);
        float zRotation = float.Parse(zrotation.text);
        transform.rotation = Quaternion.Euler(xRotation, yRotation, zRotation);

        // 获取缩放值并更新物体缩放
        float xScale = float.Parse(xsclae.text);
        float yScale = float.Parse(ysclae.text);
        float zScale = float.Parse(zsclae.text);
        transform.localScale = new Vector3(xScale, yScale, zScale);
    }

    public void OnDeleteButtonClicked()
    {
        if(place == null)return;
        GameObject dobj = GameObject.Find(place);
        Destroy(dobj);
    }
}
