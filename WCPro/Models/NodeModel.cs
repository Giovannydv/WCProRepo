using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCPro.Models
{
    public class NodeModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string MacAddress { get; set; }

        public string NodeType { get; set; }

        public double X { get; set; }

        public double Y { get; set; }
    }
}
