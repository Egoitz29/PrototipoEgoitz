using UnityEngine;

public class RTSCameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 20f;
    public float dragSpeed = 0.5f;

    [Header("Rotation")]
    public float rotationSpeed = 80f;

    [Header("Zoom")]
    public float zoomSpeed = 150f;
    public float minHeight = 12f;
    public float maxHeight = 40f;

    [Header("Map Limits")]
    public float limitX = 50f;
    public float limitZ = 50f;

    [Header("Battle Follow")]
    public bool followBattleDuringSimulation = true;
    public float followSmoothness = 3f;
    public float battleHeightMultiplier = 2f;

    private Vector3 lastMousePosition;

    public bool autoFrameBattleStart = true;
    public float framingPadding = 8f;

    void Update()
    {
        if (BattleManager.Instance != null &&
            BattleManager.Instance.BattleStarted &&
            followBattleDuringSimulation)
        {
            FollowBattleUnits();
            ClampPosition();
            return;
        }

        MoveCamera();
        DragCamera();
        RotateCamera();
        ZoomCamera();
        ClampPosition();
    }
    void MoveCamera()
    {
        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
            move += transform.forward;

        if (Input.GetKey(KeyCode.S))
            move -= transform.forward;

        if (Input.GetKey(KeyCode.D))
            move += transform.right;

        if (Input.GetKey(KeyCode.A))
            move -= transform.right;

        move.y = 0f;

        if (move != Vector3.zero)
        {
            transform.position += move.normalized * moveSpeed * Time.deltaTime;
        }
    }

    void DragCamera()
    {
        if (Input.GetMouseButtonDown(1))
        {
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(1))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;

            Vector3 move =
                (-transform.right * delta.x -
                 transform.forward * delta.y) * dragSpeed;

            move.y = 0f;

            transform.position += move;
            lastMousePosition = Input.mousePosition;
        }
    }
    void RotateCamera()
    {
        float rotation = 0f;

        if (Input.GetKey(KeyCode.Q))
            rotation -= rotationSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.E))
            rotation += rotationSpeed * Time.deltaTime;

        if (rotation != 0f)
        {
            transform.Rotate(Vector3.up, rotation, Space.World);
        }
    }

    void ZoomCamera()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll == 0f)
            return;

        Vector3 pos = transform.position;

        pos += transform.forward * scroll * zoomSpeed * Time.deltaTime * 10f;

        pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);

        transform.position = pos;
    }

    void ClampPosition()
    {
        Vector3 pos = transform.position;

        pos.x = Mathf.Clamp(pos.x, -limitX, limitX);
        pos.z = Mathf.Clamp(pos.z, -limitZ, limitZ);

        transform.position = pos;
    }

    public void FrameAllUnits()
    {
        UnitIdentity[] units =
            FindObjectsByType<UnitIdentity>(FindObjectsSortMode.None);

        if (units.Length == 0)
            return;

        Vector3 center = Vector3.zero;

        foreach (UnitIdentity u in units)
            center += u.transform.position;

        center /= units.Length;

        float maxDistance = 0f;

        foreach (UnitIdentity u in units)
        {
            float d =
            Vector3.Distance(
                center,
                u.transform.position);

            if (d > maxDistance)
                maxDistance = d;
        }

        float desiredHeight =
            Mathf.Clamp(
                maxDistance * 2f + framingPadding,
                minHeight,
                maxHeight);

        transform.position =
            new Vector3(
                center.x,
                desiredHeight,
                center.z - desiredHeight * 0.6f);
    }

    public void FollowBattleUnits()
    {
        if (BattleManager.Instance == null)
            return;

        if (!BattleManager.Instance.BattleStarted)
            return;

        UnitIdentity[] units =
            FindObjectsByType<UnitIdentity>(FindObjectsSortMode.None);

        if (units.Length == 0)
            return;

        Vector3 center = Vector3.zero;

        foreach (UnitIdentity unit in units)
        {
            center += unit.transform.position;
        }

        center /= units.Length;

        float maxDistance = 0f;

        foreach (UnitIdentity unit in units)
        {
            float distance =
                Vector3.Distance(center, unit.transform.position);

            if (distance > maxDistance)
                maxDistance = distance;
        }

        float desiredHeight =
            Mathf.Clamp(
                maxDistance * battleHeightMultiplier + framingPadding,
                minHeight,
                maxHeight);

        Vector3 desiredPosition =
            new Vector3(
                center.x,
                desiredHeight,
                center.z - desiredHeight * 0.6f);

        transform.position =
            Vector3.Lerp(
                transform.position,
                desiredPosition,
                followSmoothness * Time.deltaTime);
    }
}