using UnityEngine;

public class CameraDragController : MonoBehaviour
{
    public Camera mainCamera;

    private Vector3 dragStartPosition;
    private Vector3 cameraStartPosition;
    private bool isDragging = false;
    private bool isCameraAdjusting = false;

    private float currentZoom = 10f;
    private float zoomSpeed = 10f;
    private float rotationSpeed = 0.5f;
    private float moveSpeed = 0.5f;

    public float minX = -300f, maxX = 300f;
    public float minY = 5f, maxY = 300f;
    public float minZ = -300f, maxZ = 300f;

    private Quaternion targetRotation;
    private Vector3 targetPosition;

    private bool isDraggingRotation = false;
    private bool isDraggingMovement = false; // 添加新变量，避免轻微点击导致误判
    private float dragThreshold = 5f; // 设定拖动阈值

    void Start()
    {
        mainCamera.orthographic = true;
        currentZoom = mainCamera.orthographicSize;
        targetRotation = mainCamera.transform.rotation;
        targetPosition = mainCamera.transform.position;
    }

    void LateUpdate()
    {
        if (isCameraAdjusting)
        {
            mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, targetRotation, Time.deltaTime * 5f);
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, Time.deltaTime * 10f);
        }

        HandleRotation();
        HandleZoom();
        HandleMovement();
    }

    void HandleRotation()
    {
        if (Input.GetMouseButtonDown(1))
        {
            dragStartPosition = Input.mousePosition;
            isDraggingRotation = false;
            isCameraAdjusting = true;
        }

        if (Input.GetMouseButton(1))
        {
            if (!isDraggingRotation && Vector3.Distance(Input.mousePosition, dragStartPosition) > dragThreshold)
            {
                isDraggingRotation = true; // 只有移动超过阈值，才开始旋转
            }

            if (isDraggingRotation)
            {
                Vector3 dragDelta = (Input.mousePosition - dragStartPosition) * 0.1f;
                float rotationX = dragDelta.x * rotationSpeed;
                float rotationY = -dragDelta.y * rotationSpeed;

                targetRotation = Quaternion.Euler(
                    Mathf.Clamp(targetRotation.eulerAngles.x + rotationY, 0f, 90f),
                    targetRotation.eulerAngles.y + rotationX,
                    0f
                );

                dragStartPosition = Input.mousePosition;
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
            isDraggingRotation = false;
            isCameraAdjusting = false;
        }
    }

    void HandleZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            currentZoom -= scrollInput * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, 5f, 200f);

            isCameraAdjusting = true;
            mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, currentZoom, Time.deltaTime * 5f);
        }
    }

    void HandleMovement()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragStartPosition = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, mainCamera.transform.position.y));
            cameraStartPosition = targetPosition;
            isDragging = true;
            isDraggingMovement = false; // 初始状态，未判定为拖动
            isCameraAdjusting = true;
        }

        if (Input.GetMouseButton(0))
        {
            Vector3 currentMouseWorldPosition = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, mainCamera.transform.position.y));
            Vector3 dragDelta = dragStartPosition - currentMouseWorldPosition;

            // 只有鼠标移动距离超过阈值，才判定为拖动
            if (!isDraggingMovement && dragDelta.magnitude > dragThreshold)
            {
                isDraggingMovement = true;
            }

            if (isDraggingMovement)
            {
                Vector3 move = new Vector3(dragDelta.x, 0, dragDelta.z) * moveSpeed;
                targetPosition = cameraStartPosition + move;

                targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
                targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
                targetPosition.z = Mathf.Clamp(targetPosition.z, minZ, maxZ);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            isDraggingMovement = false;
            isCameraAdjusting = false;
        }
    }
}
