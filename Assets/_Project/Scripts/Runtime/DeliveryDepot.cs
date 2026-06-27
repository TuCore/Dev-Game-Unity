using UnityEngine;

namespace DevGameUnity.Delivery
{
    public sealed class DeliveryDepot : MonoBehaviour
    {
        public string depotName = "Kho EMS";
        public int packageCapacity = 5;

        public string Prompt => $"Bam E de nhan {packageCapacity} thung hang";
    }
}
