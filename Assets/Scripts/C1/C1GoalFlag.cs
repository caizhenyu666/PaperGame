using System;
using UnityEngine;

namespace PaperGame.C1
{
    public sealed class C1GoalFlag : MonoBehaviour
    {
        public event Action<C1PlayerController2D> Reached;

        public bool HasBeenReached { get; private set; }

        public bool TryReach(C1PlayerController2D player)
        {
            if (player == null || HasBeenReached || player.IsFallen)
            {
                return false;
            }

            HasBeenReached = true;
            player.Complete();
            Reached?.Invoke(player);
            return true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryReach(other.GetComponent<C1PlayerController2D>());
        }
    }
}
