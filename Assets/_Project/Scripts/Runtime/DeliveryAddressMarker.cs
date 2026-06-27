using UnityEngine;

namespace DevGameUnity.Delivery
{
    public sealed class DeliveryAddressMarker : MonoBehaviour
    {
        public string addressId = "A-001";
        public string displayName = "Delivery Door";
        public int floor = 1;
        public bool acceptsCash = true;
        public bool canBeBombed = true;
        public Transform dropPoint;
    }
}
