using UnityEngine;

public static class CameraUtils
{
    /* ---------------------------------------------------------
     * 1) 오브젝트가 카메라 화면 안에 있는지 확인 (Viewport 방식)
     * --------------------------------------------------------- */
    public static bool IsInCameraView(Camera cam, Vector3 worldPos)
    {
        Vector3 viewportPos = cam.WorldToViewportPoint(worldPos);

        // 카메라 뒤 (z < 0) 면 무조건 화면 밖
        if (viewportPos.z < 0)
            return false;

        // x,y가 0~1 범위면 화면 안
        return (
            viewportPos.x >= 0f && viewportPos.x <= 1f && viewportPos.y >= 0f && viewportPos.y <= 1f
        );
    }


    /* ---------------------------------------------------------
     * 2) 오브젝트의 Renderer가 화면에 보이는지 (크기 포함)
     * GeometryUtility + renderer.bounds 기반
     * --------------------------------------------------------- */
    public static bool IsRendererVisible(Camera cam, Renderer renderer)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
        return GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
    }


    /* ---------------------------------------------------------
     * 3) 화면 안쪽에 얼마나 남았는지 (Viewport 좌표 반환)
     * ex) UI panning, 화면밖 떨어지기 체크 등에 유용
     * --------------------------------------------------------- */
    public static Vector3 GetViewportPos(Camera cam, Vector3 worldPos)
    {
        return cam.WorldToViewportPoint(worldPos);
    }


    /* ---------------------------------------------------------
     * 4) 오브젝트의 스크린 픽셀 좌표 얻기 (UI 응용)
     * --------------------------------------------------------- */
    public static Vector3 GetScreenPos(Camera cam, Vector3 worldPos)
    {
        return cam.WorldToScreenPoint(worldPos);
    }


    /* ---------------------------------------------------------
     * 5) 화면 밖으로 나갔는지 체크 (ScreenPoint 기반)
     * --------------------------------------------------------- */
    public static bool IsOutsideScreen(Camera cam, Vector3 worldPos)
    {
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        // 카메라 뒤편
        if (screenPos.z < 0)
            return true;

        return (
            screenPos.x < 0 || screenPos.x > Screen.width || screenPos.y < 0 || screenPos.y > Screen.height
        );
    }


    /* ---------------------------------------------------------
     * 6) 카메라 기준으로 거리(z)가 몇인지 (전방/후방 판정)
     * --------------------------------------------------------- */
    public static float DistanceFromCamera(Camera cam, Vector3 worldPos)
    {
        return cam.WorldToViewportPoint(worldPos).z;
    }


    /* ---------------------------------------------------------
     * 7) 오브젝트가 카메라 전방(FOV 안)인지 판정 (각도 기반)
     * --------------------------------------------------------- */
    public static bool IsInCameraFOV(Camera cam, Vector3 worldPos, float fovDegreeMargin = 0f)
    {
        Vector3 dirToTarget = worldPos - cam.transform.position;
        float angle = Vector3.Angle(cam.transform.forward, dirToTarget);

        return angle < (cam.fieldOfView * 0.5f + fovDegreeMargin);
    }
}
