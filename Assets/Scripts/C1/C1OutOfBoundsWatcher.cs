using System;
using UnityEngine;

namespace PaperGame.C1
{
    public sealed class C1OutOfBoundsWatcher : MonoBehaviour
    {
        public event Action<C1PlayerController2D> Fell;

        private C1PlayerController2D player;
        private float killY = float.NegativeInfinity;

        public float KillY => killY;

        public void Configure(C1PlayerController2D target, float killYValue)
        {
            player = target;
            killY = killYValue;
        }

        private void LateUpdate()
        {
            Evaluate();
        }

        public void Evaluate()
        {
            if (player == null || player.IsCompleted || player.IsFallen)
            {
                return;
            }

            if (player.transform.position.y >= killY)
            {
                return;
            }

            player.Fall();
            Fell?.Invoke(player);
        }
    }
}
