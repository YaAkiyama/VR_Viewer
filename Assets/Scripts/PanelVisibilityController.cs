using UnityEngine;
using System.Collections.Generic;

public class PanelVisibilityController : MonoBehaviour
{
    // VRコントローラーの設定
    public Transform leftControllerAnchor;
    public Transform rightControllerAnchor;
    
    // ポインターの設定
    public GameObject laserPointerPrefab;
    
    // パネル関連の設定
    [System.Serializable]
    public class InteractivePanel
    {
        public GameObject panelObject;
        public string panelID;
        public bool isVisible = true;
        public bool requiresPointing = true;
        
        [HideInInspector]
        public List<MonoBehaviour> interactables = new List<MonoBehaviour>();
    }
    
    public List<InteractivePanel> panels = new List<InteractivePanel>();
    
    // コンポーネント参照
    private VRLaserPointer leftPointer;
    private VRLaserPointer rightPointer;
    
    // 状態変数
    private InteractivePanel currentPanel;
    private bool isInitialized = false;

    void Start()
    {
        InitializePointers();
        InitializePanels();
        isInitialized = true;
    }
    
    void Update()
    {
        if (!isInitialized) return;
        
        UpdatePanelInteractions();
    }
    
    // ポインターの初期化
    private void InitializePointers()
    {
        if (laserPointerPrefab != null)
        {
            // 左コントローラーのポインター
            if (leftControllerAnchor != null)
            {
                GameObject leftPointerObj = Instantiate(laserPointerPrefab, leftControllerAnchor.position, leftControllerAnchor.rotation);
                leftPointerObj.transform.parent = leftControllerAnchor;
                leftPointer = leftPointerObj.GetComponent<VRLaserPointer>();
                if (leftPointer == null)
                {
                    leftPointer = leftPointerObj.AddComponent<VRLaserPointer>();
                }
                leftPointer.laserColor = Color.blue;
            }
            
            // 右コントローラーのポインター
            if (rightControllerAnchor != null)
            {
                GameObject rightPointerObj = Instantiate(laserPointerPrefab, rightControllerAnchor.position, rightControllerAnchor.rotation);
                rightPointerObj.transform.parent = rightControllerAnchor;
                rightPointer = rightPointerObj.GetComponent<VRLaserPointer>();
                if (rightPointer == null)
                {
                    rightPointer = rightPointerObj.AddComponent<VRLaserPointer>();
                }
                rightPointer.laserColor = Color.red;
            }
        }
        else
        {
            // プレハブがない場合は直接コンポーネントを追加
            if (leftControllerAnchor != null)
            {
                leftPointer = leftControllerAnchor.gameObject.AddComponent<VRLaserPointer>();
                leftPointer.laserColor = Color.blue;
            }
            
            if (rightControllerAnchor != null)
            {
                rightPointer = rightControllerAnchor.gameObject.AddComponent<VRLaserPointer>();
                rightPointer.laserColor = Color.red;
            }
        }
    }
    
    // パネルの初期化
    private void InitializePanels()
    {
        foreach (InteractivePanel panel in panels)
        {
            if (panel.panelObject != null)
            {
                // インタラクティブなコンポーネントを検索
                MonoBehaviour[] components = panel.panelObject.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (MonoBehaviour comp in components)
                {
                    // インタラクティブなコンポーネントを判別する処理
                    // 例: ボタンやスライダーなどを追加
                    if (comp.GetType().Name.Contains("Button") || 
                        comp.GetType().Name.Contains("Slider") ||
                        comp.GetType().Name.Contains("Toggle") ||
                        comp.GetType().Name.Contains("Interactable"))
                    {
                        panel.interactables.Add(comp);
                    }
                }
                
                // 初期表示状態を設定
                panel.panelObject.SetActive(panel.isVisible);
            }
        }
    }
    
    // パネルとのインタラクション更新
    private void UpdatePanelInteractions()
    {
        bool leftHit = leftPointer != null && leftPointer.IsHitting();
        bool rightHit = rightPointer != null && rightPointer.IsHitting();
        
        RaycastHit hitInfo;
        if (leftHit)
        {
            hitInfo = leftPointer.GetHitInfo();
            ProcessHit(hitInfo);
        }
        
        if (rightHit)
        {
            hitInfo = rightPointer.GetHitInfo();
            ProcessHit(hitInfo);
        }
    }
    
    // レイキャストヒット処理
    private void ProcessHit(RaycastHit hit)
    {
        GameObject hitObject = hit.transform.gameObject;
        
        // ヒットしたオブジェクトがどのパネルに属するか判定
        foreach (InteractivePanel panel in panels)
        {
            if (panel.panelObject != null && panel.isVisible)
            {
                if (IsPartOf(hitObject, panel.panelObject))
                {
                    currentPanel = panel;
                    // インタラクション処理を行う
                    // （必要に応じてここにクリックイベントなどを追加）
                    break;
                }
            }
        }
    }
    
    // オブジェクトが指定された親の子孫かどうかを判定
    private bool IsPartOf(GameObject obj, GameObject parent)
    {
        Transform current = obj.transform;
        while (current != null)
        {
            if (current.gameObject == parent)
            {
                return true;
            }
            current = current.parent;
        }
        return false;
    }
    
    // パネルの表示・非表示を切り替えるメソッド
    public void TogglePanelVisibility(string panelID)
    {
        foreach (InteractivePanel panel in panels)
        {
            if (panel.panelID == panelID && panel.panelObject != null)
            {
                panel.isVisible = !panel.isVisible;
                panel.panelObject.SetActive(panel.isVisible);
                break;
            }
        }
    }
    
    // 特定のパネルを表示するメソッド
    public void ShowPanel(string panelID)
    {
        foreach (InteractivePanel panel in panels)
        {
            if (panel.panelID == panelID && panel.panelObject != null)
            {
                panel.isVisible = true;
                panel.panelObject.SetActive(true);
                break;
            }
        }
    }
    
    // 特定のパネルを非表示にするメソッド
    public void HidePanel(string panelID)
    {
        foreach (InteractivePanel panel in panels)
        {
            if (panel.panelID == panelID && panel.panelObject != null)
            {
                panel.isVisible = false;
                panel.panelObject.SetActive(false);
                break;
            }
        }
    }
    
    // すべてのパネルを非表示にするメソッド
    public void HideAllPanels()
    {
        foreach (InteractivePanel panel in panels)
        {
            if (panel.panelObject != null)
            {
                panel.isVisible = false;
                panel.panelObject.SetActive(false);
            }
        }
    }
}
