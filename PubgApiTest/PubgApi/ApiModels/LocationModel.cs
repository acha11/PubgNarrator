using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubgApiTest.PubgApi.ApiModels
{
    public class LocationModel
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public double DistanceFrom(LocationModel location)
        {
            return
                Math.Sqrt(
                    (location.X - this.X) * (location.X - this.X) +
                    (location.Y - this.Y) * (location.Y - this.Y) +
                    (location.Z - this.Z) * (location.Z - this.Z)
                );
        }
    }
}
