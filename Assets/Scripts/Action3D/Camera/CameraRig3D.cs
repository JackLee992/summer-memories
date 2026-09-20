using UnityEngine;

namespace SummerMemories.Action3D
{
    /// <summary>
    /// 第三人称跟随相机：yaw/pitch 轨道跟随，可锁定敌人时自动环绕；带命中震屏。
    /// 用 unscaledDeltaTime 更新，保证命中顿帧(timeScale≈0)时震屏/UI 仍有反馈。
    /// </summary>
    public class CameraRig3D : MonoBehaviour
    {
        public Transform Target;
        public bool InputEnabled = true;
        public float Distance = 5.2f;
        public float Height = 2.3f;
        public float FollowLerp = 12f;
        public float YawSpeed = 160f;
        public float PitchSpeed = 90f;
        public float MinPitch = -18f;
        public float MaxPitch = 52f;

        public float Yaw = 0f;
        public float Pitch = 14f;

        public Transform LockTarget;
        public float LockTurnSpeed = 8f;

        private Camera _cam;
        private float _shake;
        private Vector3 _shakeOffset;

        public Camera Camera => _cam;

        public void Init(Transform target)
        {
            Target = target;
            var go = new GameObject("MainCamera");
            go.transform.SetParent(transform, false);
            _cam = go.AddComponent<Camera>();
            _cam.fieldOfView = 55f;
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 600f;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.02f, 0.04f, 0.08f, 1f);
            _cam.allowMSAA = true;
            var listener = go.AddComponent<AudioListener>();
            // 保证只有一个 AudioListener
            var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            for (var i = 0; i < listeners.Length; i++)
                if (listeners[i] != listener) Destroy(listeners[i]);
            Yaw = target.eulerAngles.y;
        }

        public void AddShake(float amount)
        {
            _shake = Mathf.Clamp01(Mathf.Max(_shake, amount));
        }

        public void Snap()
        {
            if (Target == null || _cam == null) return;
            _cam.transform.position = Target.position + Vector3.up * Height - Quaternion.Euler(Pitch,Yaw,0) * Vector3.forward * Distance;
            _cam.transform.LookAt(Target.position + Vector3.up * 1.4f);
        }

        private void LateUpdate()
        {
            if (Target == null || _cam == null) return;
            var dt = Time.unscaledDeltaTime;

            var look = InputEnabled ? Input3D.Look() : Vector2.zero;
            if (LockTarget == null)
            {
                Yaw += look.x * YawSpeed / 60f;
                Pitch -= look.y * PitchSpeed / 60f;
                Pitch = Mathf.Clamp(Pitch, MinPitch, MaxPitch);
            }
            else
            {
                // 锁定时相机自动绕到能看到目标的方位
                var toTarget = LockTarget.position - Target.position;
                var wantYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
                Yaw = Mathf.LerpAngle(Yaw, wantYaw, LockTurnSpeed * dt);
                Pitch = Mathf.Lerp(Pitch, 12f, LockTurnSpeed * dt);
            }

            // 震动（顿帧期间也衰减）
            _shake = Mathf.Max(0f, _shake - dt * 2.4f);
            var s = _shake * _shake;
            _shakeOffset = new Vector3(
                (Mathf.PerlinNoise(Time.unscaledTime * 31f, 0f) - 0.5f) * 2f * s * 0.35f,
                (Mathf.PerlinNoise(0f, Time.unscaledTime * 31f) - 0.5f) * 2f * s * 0.35f,
                0f);

            var pivot = Target.position + Vector3.up * Height;
            var rot = Quaternion.Euler(Pitch, Yaw, 0f);
            var desired = pivot - (rot * Vector3.forward * Distance) + _shakeOffset;

            // 简单墙体防穿墙：从玩家向相机做一次球体检测（忽略角色层 8）
            var mask = ~(1 << 8);
            var dir = desired - pivot;
            var dist = dir.magnitude;
            if (Physics.SphereCast(pivot, 0.25f, dir.normalized, out var hit, dist,
                mask, QueryTriggerInteraction.Ignore))
            {
                desired = pivot + dir.normalized * Mathf.Max(0.6f, hit.distance - 0.15f);
            }

            _cam.transform.position = Vector3.Lerp(_cam.transform.position, desired,
                1f - Mathf.Exp(-FollowLerp * dt));
            var lookAt = LockTarget != null
                ? Vector3.Lerp(Target.position + Vector3.up * 1.4f, LockTarget.position + Vector3.up * 1.2f, 0.6f)
                : Target.position + Vector3.up * 1.4f;
            _cam.transform.LookAt(lookAt + _shakeOffset * 0.5f);
        }
    }
}
