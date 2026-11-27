using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadCity.Models
{
    public class OrderItemModel
    {
        public int Id { get; set; }
        public string OrderArticles { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; }
        public int PickupPointId { get; set; }
        public string PickupAddress { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime DeliveryDate { get; set; }
        public int PickupCode { get; set; }
        public int UserId { get; set; }

        public string OrderDateFormatted
        {
            get => OrderDate.ToString("dd.MM.yyyy");
            set { }
        }

        public string DeliveryDateFormatted
        {
            get => DeliveryDate.ToString("dd.MM.yyyy");
            set { }
        }

    }
}
