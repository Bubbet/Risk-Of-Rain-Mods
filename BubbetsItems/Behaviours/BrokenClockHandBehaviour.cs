using UnityEngine;

namespace BubbetsItems
{
    public class BrokenClockHandBehaviour : MonoBehaviour
    {
        public Transform hourHand;
        public Transform minuteHand;

        private float _ratio = 2f;
        private float _nextMinuteY;
        private float _nextHourY;

        public void Update()
        {
            _ratio += Time.deltaTime;
            if (_ratio > 1f)
            {
                _ratio = 0f;
                _nextHourY = UnityEngine.Random.value* 2f - 1f;
                _nextMinuteY = UnityEngine.Random.value * 2f - 1f;
            }
            
            var hourHandeuler = hourHand.rotation.eulerAngles;
            var minuteHandeuler = minuteHand.rotation.eulerAngles;
            hourHand.Rotate(0,0, _nextHourY);
            minuteHand.Rotate(0, 0, _nextMinuteY);
            //minuteHand.rotation = Quaternion.Euler(minuteHandeuler.x, Mathf.LerpAngle(_prevMinuteY, _nextMinuteY, _ratio), minuteHandeuler.z);
        }
    }
}