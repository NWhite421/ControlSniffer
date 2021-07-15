using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleSniffer
{
    public record Point
    {
        private static readonly ILog log = log4net.LogManager.GetLogger("Point.cs");

        #region Public Variables
        public string ID { get; set; }
        public double Northing { get; set; }
        public double Easting { get; set; }
        public double Elevation { get; set; }
        public string Description { get; set; }
        public DateTime DateAquired { get; set; }
        public string SourceJob { get; set; }
        public string SourcePointID { get; set; }
        #endregion

        #region Public Methods
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

        override public string ToString()
        {
            return $"{SourcePointID},{Northing:#.000},{Easting:#.000},{Elevation:#.000},{Description},{SourceJob},{DateAquired},{ID}";
        } 

        #endregion

        #region Initializers
        public Point(string inputString, string jobNumber, DateTime aquireDate)
        {
            List<string> attributes = inputString.Split(",").ToList();
            if (attributes.Count > 5)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                if (!string.IsNullOrEmpty(attributes[5]))
                {
                    Console.Write("A point value has too many commas.\n" +
                        $"Value 5: {attributes[4]} | Value 6: {attributes[5]}\n" +
                        "Combine value 5 & 6 [y/n]: ");
                    var ret = Console.ReadKey();
                    if (ret.Key != ConsoleKey.Y)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        return;
                    }
                }
                
                attributes[4] = $"{attributes[4]} {attributes[5]}";
                attributes.RemoveAt(5);
                Console.ForegroundColor = ConsoleColor.White;
            }
            if (attributes.Count < 5)
            {
                log.Warn("Point could not be created due to too little arguments passed.");
                return;
            }
            SourcePointID = attributes[0];
            Northing = double.Parse(attributes[1]);
            Easting = double.Parse(attributes[2]);
            Elevation = double.Parse(attributes[3]);
            Description = attributes[4];
            SourceJob = jobNumber;
            DateAquired = aquireDate;
        } 
        #endregion
    }
}
