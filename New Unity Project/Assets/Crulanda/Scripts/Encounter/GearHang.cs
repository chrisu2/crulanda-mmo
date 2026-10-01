using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Keeps a hung off-hand piece (a lantern, a censer, a pair of scales) hanging straight down from the hand whatever the arm
    /// is doing, turned the way its wearer faces.
    /// </summary>
    public sealed class GearHang : MonoBehaviour
    {
        public Transform wearer;
        void LateUpdate()
        {
            if (wearer == null) return;
            transform.rotation = Quaternion.Euler(0, wearer.eulerAngles.y, 0);
        }
    }
}
