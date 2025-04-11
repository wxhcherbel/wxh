using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems; 
using Unity.MLAgents.Policies;

public class AgentInfoPanel : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI agentNameText;
    public TextMeshProUGUI behaviorTypeText;
    public Toggle modeToggle;
    private CustomAgent currentAgent;
    public Animator handleAnimator; // 控制滑块动画的 Animator


    void Start()
    {
        Hide();
    }
    void Update()
    {
        // 如果panel是显示状态
        if (panel.activeSelf)
        {
            // 检查是否有鼠标点击（左键0或右键1）
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                // 如果点击不在UI上（比如没点击到 panel 或其子元素）
                if (!IsPointerOverUIElement(panel))
                {
                    Hide();
                }
            }
        }
    }
    // 检查是否点击了特定UI GameObject（包括其子物体）
    bool IsPointerOverUIElement(GameObject target)
    {
        // 当前点击指针下的所有UI元素
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        foreach (var result in raycastResults)
        {
            if (result.gameObject == target || result.gameObject.transform.IsChildOf(target.transform))
            {
                return true; // 点击在panel内
            }
        }

        return false; // 点击在panel外
    }

    public void Show(CustomAgent agent)
    {
        currentAgent = agent;
        panel.SetActive(true);

        agentNameText.text = agent.name;

        var bp = agent.GetComponent<BehaviorParameters>();

        bool isManual = bp.BehaviorType == BehaviorType.HeuristicOnly;
        modeToggle.isOn = isManual;
        behaviorTypeText.text = bp.BehaviorType.ToString();

        // 👇 这里控制动画
        if (handleAnimator != null)
            handleAnimator.SetBool("isOn", isManual);

        Vector3 worldPos = agent.transform.position;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        screenPos += new Vector3(100, 50, 0);
        panel.GetComponent<RectTransform>().position = screenPos;

        modeToggle.onValueChanged.RemoveAllListeners();
        modeToggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool isOn)
    {
        if (currentAgent == null) return;

        var bp = currentAgent.GetComponent<BehaviorParameters>();
        bp.BehaviorType = isOn ? BehaviorType.HeuristicOnly : BehaviorType.Default;
        behaviorTypeText.text = bp.BehaviorType.ToString();

        // 👇 动画切换
        if (handleAnimator != null)
            handleAnimator.SetBool("isOn", isOn);
    }


    public void Hide()
    {
        panel.SetActive(false);
    }
}
