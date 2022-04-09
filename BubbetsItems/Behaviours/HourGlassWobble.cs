using System.Linq;
using UnityEngine;

namespace BubbetsItems.Behaviours
{
    public class HourGlassWobble : MonoBehaviour
    {
        private Renderer? _rend;
        private Vector3 _lastPos;
        private Vector3 _velocity;
        private Vector3 _lastRot;
        private Vector3 _angularVelocity;
        public float maxWobble = 0.03f;
        public float wobbleSpeed = 1f;
        public float recovery = 1f;
        private float _wobbleAmountX;
        private float _wobbleAmountZ;
        private float _wobbleAmountToAddX;
        private float _wobbleAmountToAddZ;
        private float _pulse;
        private float _time = 0.5f;
        private Material? _material;
        private static readonly int WobbleX = Shader.PropertyToID("_WobbleX");
        private static readonly int WobbleZ = Shader.PropertyToID("_WobbleZ");

        // Use this for initialization
        private void Start()
        {
            _rend = GetComponent<Renderer>();
            _material = _rend.materials.First(x => x.shader.name.Contains("Pain"));
            _wobbleAmountX = 10f;
        }
        private void Update()
        {
            if (!_material || !transform) return;
            _time += Time.deltaTime;
            // decrease wobble over time
            _wobbleAmountToAddX = Mathf.Lerp(_wobbleAmountToAddX, 0, Time.deltaTime * (recovery));
            _wobbleAmountToAddZ = Mathf.Lerp(_wobbleAmountToAddZ, 0, Time.deltaTime * (recovery));
 
            // make a sine wave of the decreasing wobble
            _pulse = 2 * Mathf.PI * wobbleSpeed;
            _wobbleAmountX = _wobbleAmountToAddX * Mathf.Sin(_pulse * _time);
            _wobbleAmountZ = _wobbleAmountToAddZ * Mathf.Sin(_pulse * _time);
 
            // send it to the shader
            _material!.SetFloat(WobbleX, _wobbleAmountX);
            _material.SetFloat(WobbleZ, _wobbleAmountZ);
 
            // velocity
            var transform1 = transform;
            var position = transform1.position;
            var rotation = transform1.rotation;
            
            _velocity = (_lastPos - position) / Time.deltaTime;
            _angularVelocity = rotation.eulerAngles - _lastRot;
 
 
            // add clamped velocity to wobble
            _wobbleAmountToAddX += Mathf.Clamp((_velocity.x + (_angularVelocity.z * 0.2f)) * maxWobble, -maxWobble, maxWobble);
            _wobbleAmountToAddZ += Mathf.Clamp((_velocity.z + (_angularVelocity.x * 0.2f)) * maxWobble, -maxWobble, maxWobble);
 
            // keep last position
            _lastPos = position;
            _lastRot = rotation.eulerAngles;
        }
    }
}