using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleSniffer
{
    public class Point
    {
        public string ID { get; set; }
        public double Northing { get; set; }
        public double Easting { get; set; }
        public double Elevation { get; set; }
        public string Description { get; set; }
        public DateTime DateAquired { get; set; }

        public double[] GetCoordinatePoint()
        {
            return new double[] { Northing, Easting, Elevation };
        }

        public (bool isSamePoint, double[] averagePoint) CompareLocation(Point otherPoint, double deltaDistance)
        {
            if (otherPoint == null)
            {
                return (false, null);
            }
            var deltaNorthing = otherPoint.Northing - Northing;
            var deltaEasting = otherPoint.Easting - Easting;
            var deltaElevation = otherPoint.Elevation - Elevation;
            double totaldistance = Math.Sqrt((deltaNorthing * deltaNorthing) + (deltaEasting * deltaEasting) + (deltaElevation * deltaElevation));
            if (totaldistance <= deltaDistance)
            {
                return (true, new double[] { (deltaNorthing + Northing) / 2, (deltaEasting + Easting) / 2, (deltaElevation + Elevation) / 2 });
            }
            else
            {
                return (false, null);
            }
        }
    }
}
