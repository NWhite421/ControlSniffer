using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleSniffer
{
    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Point(string coordinates)
        {
            string[] parts = coordinates.Split(',');
            X = double.Parse(parts[0]);
            Y = double.Parse(parts[1]);
        }
    }
    public record SurveyPoint
    {
        private static readonly ILog log = log4net.LogManager.GetLogger("Point.cs");

        #region Public Variables

        /// <summary>
        /// The unique ID of the survey point.
        /// </summary>
        public string ID { get; set; }
        /// <summary>
        /// The northing of the point
        /// </summary>
        public double Northing { get; set; }
        /// <summary>
        /// The easting of the point
        /// </summary>
        public double Easting { get; set; }
        /// <summary>
        /// the elevation (zenith) of the point
        /// </summary>
        public double Elevation { get; set; }
        /// <summary>
        /// The description of the point.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The aquire date.
        /// </summary>
        public DateTime DateAquired { get; set; }
        /// <summary>
        /// The source job number in XX-XX-XXX format.
        /// </summary>
        public string SourceJob { get; set; }
        /// <summary>
        /// The source point number
        /// </summary>
        public string SourcePointID { get; set; }
        /// <summary>
        /// The 2D Corrdinates in X, Y.
        /// </summary>
        public (double x, double y) Coordinates2D
        {
            get
            {
                return (Easting, Northing);
            }
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Get the 3D Coordinate.
        /// </summary>
        /// <returns>Array of 3 doubles.</returns>
        public double[] Get3DCoordinates()
        {
            return new double[] { Northing, Easting, Elevation };
        }

        /// <summary>
        /// Compares the location of the point to another provided point.
        /// </summary>
        /// <param name="otherPoint">Point to compare.</param>
        /// <param name="deltaDistance">Distance to consider the point "same".</param>
        /// <returns></returns>
        public bool CompareLocation(SurveyPoint otherPoint, double deltaDistance)
        {
            if (otherPoint == null)
            {
                return false;
            }
            var deltaNorthing = otherPoint.Northing - Northing;
            var deltaEasting = otherPoint.Easting - Easting;
            var deltaElevation = otherPoint.Elevation - Elevation;
            double totaldistance = Math.Sqrt((deltaNorthing * deltaNorthing) + (deltaEasting * deltaEasting) + (deltaElevation * deltaElevation));
            if (totaldistance <= deltaDistance)
            {
                return true;
            }
            return false;
        }

        override public string ToString()
        {
            return $"{ID},{Northing:#.000},{Easting:#.000},{Elevation:#.000},{Description},{SourceJob},{DateAquired},{SourcePointID}";
        } 

        public string ToSimpleString()
        {
            return $"{SourcePointID},{Northing:#.000},{Easting:#.000},{Elevation:#.000},{Description}";
        }

        public SurveyPoint CombinePoints(IEnumerable<SurveyPoint> otherPoints)
        {
            var NorthingList = otherPoints.Select(op => op.Northing).ToList();
            var EastingList = otherPoints.Select(op => op.Easting).ToList();
            var ElevationList = otherPoints.Select(op => op.Elevation).ToList();

            NorthingList.Add(Northing);
            EastingList.Add(Easting);
            ElevationList.Add(Elevation);

            var newNorthing = NorthingList.Average();
            var newEasting = EastingList.Average();
            var newElevation = ElevationList.Average();

            log.Warn($"New coordinates are: N: {newNorthing:#.000}({Northing - newNorthing}) | E: {newEasting:#.000}({Easting - newEasting}) | Z: {newElevation:#.000}({Elevation - newElevation})");
            Northing = newNorthing;
            Easting = newEasting;
            Elevation = newElevation;

            return this;
        }
        #endregion

        #region Initializers
        /// <summary>
        /// A survey point representation for addition into a GeoDatabase.
        /// </summary>
        /// <param name="inputString">Input string from field text file.</param>
        /// <param name="jobNumber">Job number of source job.</param>
        /// <param name="aquireDate">Date the point was aquired.</param>
        public SurveyPoint(string inputString, string jobNumber, DateTime aquireDate)
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
                    var ret = Console.ReadKey(true);
                    if (ret.Key != ConsoleKey.Y)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        return;
                    }
                }
                attributes[4] = $"{attributes[4]}--{attributes[5]}";
                attributes.RemoveAt(5);
                Console.ForegroundColor = ConsoleColor.White;
            }
            if (attributes.Count < 5)
            {
                log.Warn($"Point could not be created due to too little arguments passed.\n--> {inputString}");
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

        public SurveyPoint(string inputString)
        {
            if (string.IsNullOrEmpty(inputString))
            {
                log.Warn($"Could not process existing line item.\n\"{inputString}\"");
                return;
            }
            string[] attributes = inputString.Split(',');
            ID = attributes[0];
            Northing = double.Parse(attributes[1]);
            Easting = double.Parse(attributes[2]);
            Elevation = double.Parse(attributes[3]);
            Description = attributes[4];
            DateAquired = DateTime.Parse(attributes[6]);
            SourceJob = attributes[5];
            SourcePointID = attributes[7];
        }
        #endregion
    }
}
