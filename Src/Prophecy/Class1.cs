using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Prophecy
{
    [ProtoContract]
    [Serializable]
    public class Customer
    {
        [ProtoMember(1)]
        public string CustomerID { get; set; }
        [ProtoMember(2)]
        public int PO { get; set; }
        [ProtoMember(3)]
        public Address Address { get; set; }
        [ProtoMember(4)]
        public Order Order { get; set; }

        [ProtoMember(5, IsRequired = false)]
        public byte[] BytesData; 


        public static Customer GetOneCustomer()
        {
            Customer customer = new Customer
            {
                CustomerID = "ALFKI",
                PO = 9572658,
                Address = new Address
                {
                    Street = "One Main Street",
                    City = "Anywhere",
                    State = "NJ",
                    Zip = 08080
                },
                Order = new Order
                {
                    OrderID = 10966,
                    LineItems = new List<LineItem>
                    {
                        new LineItem
                            {
                                ProductID = 37,
                                UnitPrice = 26.50M,
                                Quantity =8,
                                Description ="Gravad lax"
                            },
                        new LineItem
                            {
                                ProductID = 56,
                                UnitPrice = 38.00M,
                                Quantity =12,
                                Description ="Gnocchi di nonna Alice"    
                            }
                    }
                }
                , BytesData = System.Text.Encoding.Unicode.GetBytes("Hello World")
            };
            return customer;
        }
    }

    [ProtoContract]
    [Serializable]
    public class Address
    {
        [ProtoMember(1)]
        public string Street { get; set; }
        [ProtoMember(2)]
        public string City { get; set; }
        [ProtoMember(3)]
        public string State { get; set; }
        [ProtoMember(4)]
        public int Zip { get; set; }
    }

    [ProtoContract]
    [Serializable]
    public class Order
    {
        [ProtoMember(1)]
        public int OrderID { get; set; }
        [ProtoMember(2)]
        public List<LineItem> LineItems { get; set; }
    }

    [ProtoContract]
    [Serializable]
    public class LineItem
    {
        [ProtoMember(1)]
        public int ProductID { get; set; }
        [ProtoMember(2)]
        public decimal UnitPrice { get; set; }
        [ProtoMember(3)]
        public int Quantity { get; set; }
        [ProtoMember(4)]
        public string Description { get; set; }
    }

}
