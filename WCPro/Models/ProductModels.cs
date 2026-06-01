using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCPro.Models
{
    class ProductModels
    {
        public class ProductModel
        {
            public string ProductName { get; set; }

            public string ImagePath { get; set; }

            public List<NodeModel> Nodes { get; set; }
        }
    }
}
