using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

public class VRLaserPointer : MonoBehaviour
{
    [Header("レーザー設定")]
    public Color laserColor = Color.blue;
    public float laserWidth = 0.002f;
    public float laserMaxLength = 10f;
    public GameObject laserDot;
    public float dotScale = 0.01f;

    [Header("コントローラー参照")]
    public Transform leftControllerAnchor;
    public Transform rightControllerAnchor;
    public bool useLeftController = true;
    public bool useRightController = true;

    [Header("インタラクション設定")]
    public LayerMask interactionLayers;
    public InputActionReference triggerAction;
    public InputActionReference primaryButtonAction;

    // レーザーとドットのレンダラー
    private LineRenderer leftLaser;
    private LineRenderer rightLaser;
    private GameObject leftDot;
    private GameObject rightDot;

    // 現在のポインターデータ
    private PointerEventData leftPointerEventData;
    private PointerEventData rightPointerEventData;
    private List<RaycastResult> leftRaycastResults = new List<RaycastResult>();
    private List<RaycastResult> rightRaycastResults = new List<RaycastResult>();

    // Start is called before the first frame update
    void Start()
    {
        // 左コントローラーレーザー初期化
        if (useLeftController && leftControllerAnchor != null)
        {
            leftLaser = CreateLaserBeam(leftControllerAnchor, "LeftLaser");
            leftDot = CreateLaserDot(leftControllerAnchor, "LeftDot");
            leftPointerEventData = new PointerEventData(EventSystem.current);
        }

        // 右コントローラーレーザー初期化
        if (useRightController && rightControllerAnchor != null)
        {
            rightLaser = CreateLaserBeam(rightControllerAnchor, "RightLaser");
            rightDot = CreateLaserDot(rightControllerAnchor, "RightDot");
            rightPointerEventData = new PointerEventData(EventSystem.current);
        }

        // 入力アクションを有効化
        if (triggerAction != null && triggerAction.action != null)
        {
            triggerAction.action.Enable();
            triggerAction.action.performed += OnTriggerPerformed;
        }

        if (primaryButtonAction != null && primaryButtonAction.action != null)
        {
            primaryButtonAction.action.Enable();
            primaryButtonAction.action.performed += OnPrimaryButtonPerformed;
        }
    }

    // レーザービームを作成
    private LineRenderer CreateLaserBeam(Transform parent, string name)
    {
        GameObject laserObj = new GameObject(name);
        laserObj.transform.parent = parent;
        laserObj.transform.localPosition = Vector3.zero;
        laserObj.transform.localRotation = Quaternion.identity;

        LineRenderer lr = laserObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.startWidth = laserWidth;
        lr.endWidth = laserWidth;
        lr.positionCount = 2;
        lr.material = new Material(Shader.Find("Unlit/Color"));
        lr.material.color = laserColor;

        return lr;
    }

    // レーザードットを作成
    private GameObject CreateLaserDot(Transform parent, string name)
    {
        if (laserDot != null)
        {
            GameObject dotObj = Instantiate(laserDot, parent);
            dotObj.name = name;
            dotObj.transform.localScale = new Vector3(dotScale, dotScale, dotScale);
            dotObj.SetActive(false);
            return dotObj;
        }
        else
        {
            GameObject dotObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dotObj.name = name;
            dotObj.transform.parent = parent;
            dotObj.transform.localScale = new Vector3(dotScale, dotScale, dotScale);

            Renderer renderer = dotObj.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Unlit/Color"));
            renderer.material.color = laserColor;

            Collider collider = dotObj.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            dotObj.SetActive(false);
            return dotObj;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (useLeftController && leftControllerAnchor != null)
        {
            UpdateLaser(leftLaser, leftDot, leftControllerAnchor, leftPointerEventData, leftRaycastResults);
        }

        if (useRightController && rightControllerAnchor != null)
        {
            UpdateLaser(rightLaser, rightDot, rightControllerAnchor, rightPointerEventData, rightRaycastResults);
        }
    }

    // レーザーを更新
    private void UpdateLaser(LineRenderer laser, GameObject dot, Transform controller, PointerEventData pointerEventData, List<RaycastResult> raycastResults)
    {
        // レーザーの開始位置を設定
        Vector3 startPos = controller.position;
        laser.SetPosition(0, startPos);

        // レーザーの方向（コントローラーの前方）
        Vector3 direction = controller.forward;

        // ヒットポイントの初期化
        Vector3 hitPoint = startPos + direction * laserMaxLength;
        bool hitUI = false;

        // UIとの衝突検出
        raycastResults.Clear();
        pointerEventData.position = new Vector2(Screen.width / 2, Screen.height / 2); // 仮の値
        pointerEventData.position = Camera.main.WorldToScreenPoint(startPos + direction);
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        if (raycastResults.Count > 0)
        {
            // UI要素にヒットした場合
            hitPoint = raycastResults[0].worldPosition;
            hitUI = true;
        }
        else
        {
            // 物理的なオブジェクトとの衝突検出
            Ray ray = new Ray(startPos, direction);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, laserMaxLength, interactionLayers))
            {
                hitPoint = hit.point;
                hitUI = false;
            }
        }

        // レーザーの終了位置を設定
        laser.SetPosition(1, hitPoint);

        // ドットの位置を更新
        if (dot != null)
        {
            dot.transform.position = hitPoint;
            dot.SetActive(true);
        }
    }

    // トリガー入力時の処理
    private void OnTriggerPerformed(InputAction.CallbackContext context)
    {
        // コントローラーからのレイキャスト結果に基づいてクリックイベントを発生させる
        if (useLeftController && leftRaycastResults.Count > 0)
        {
            ExecuteUIClick(leftRaycastResults[0]);
        }

        if (useRightController && rightRaycastResults.Count > 0)
        {
            ExecuteUIClick(rightRaycastResults[0]);
        }
    }

    // プライマリボタン入力時の処理
    private void OnPrimaryButtonPerformed(InputAction.CallbackContext context)
    {
        // 追加の機能を実装（例：バックボタン）
        Debug.Log("Primary Button Pressed");
    }

    // UI要素のクリックを実行
    private void ExecuteUIClick(RaycastResult raycastResult)
    {
        GameObject hitObj = raycastResult.gameObject;

        // ボタンの場合
        Button button = hitObj.GetComponent<Button>();
        if (button != null && button.isActiveAndEnabled)
        {
            button.onClick.Invoke();
            return;
        }

        // その他のUI要素（必要に応じて拡張）
        // ...
    }

    // アプリケーション終了時にクリーンアップ
    private void OnDestroy()
    {
        if (triggerAction != null && triggerAction.action != null)
        {
            triggerAction.action.performed -= OnTriggerPerformed;
        }

        if (primaryButtonAction != null && primaryButtonAction.action != null)
        {
            primaryButtonAction.action.performed -= OnPrimaryButtonPerformed;
        }
    }
}