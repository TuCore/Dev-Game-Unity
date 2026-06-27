using UnityEngine;

namespace DevGameUnity.Delivery
{
    public sealed class DeliveryPoint : MonoBehaviour
    {
        public string addressId = "A-001";
        public string customerName = "Khach hang";
        public int reward = 45000;
        public bool delivered;

        public string Prompt => delivered
            ? $"{addressId} da giao xong"
            : $"Bam E de giao hang cho {addressId}";
    }
}
