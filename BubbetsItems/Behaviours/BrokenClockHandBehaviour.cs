using UnityEngine;

namespace BubbetsItems.Behaviours
{
    public class BrokenClockHandBehaviour : MonoBehaviour
    {
        public Transform? hourHand;
        public Transform? minuteHand;

        private float _ratio = 2f;
        private float _nextMinuteY;
        private float _nextHourY;

        public void Update()
        {
            if (!hourHand || !minuteHand) return;
            _ratio += Time.deltaTime;
            if (_ratio > 1f)
            {
                _ratio = 0f;
                _nextHourY = Random.value* 2f - 1f;
                _nextMinuteY = Random.value * 2f - 1f;
            }

            hourHand!.Rotate(0,0, _nextHourY);
            minuteHand!.Rotate(0, 0, _nextMinuteY);
            //minuteHand.rotation = Quaternion.Euler(minuteHandeuler.x, Mathf.LerpAngle(_prevMinuteY, _nextMinuteY, _ratio), minuteHandeuler.z);
        }
    }
}