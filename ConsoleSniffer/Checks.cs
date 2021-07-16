using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleSniffer
{
    //TODO: Get this to work.
    public class Checks
    {
        /// <summary>
        /// Logger
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger("Checks.cs");

        public static List<SurveyPoint> PurgePointsByBoundary(List<SurveyPoint> pointList, string boundaryFile)
        {
            List<SurveyPoint> validPoints = new() { };
            List<Point> boundaryPoints = new() { };

            foreach (string line in File.ReadAllLines(boundaryFile))
            {
                boundaryPoints.Add(new Point(line));
            }

            foreach (SurveyPoint surveyPoint in pointList)
            {
                if (IsInPolygon(boundaryPoints, surveyPoint))
                {
                    log.Debug("Survey point is located in boundary");
                    validPoints.Add(surveyPoint);
                }
                else
                {
                    log.Warn("Survey point was discarded for being outside city boundary\nPress any key to continue");
                    Console.ReadKey();
                }
            }

            return validPoints;
        }


        /// <summary>
        /// Checks if the <paramref name="surveyPoint"/> is within the <paramref name="boundaryPoints"/>
        /// </summary>
        /// <param name="boundaryPoints"></param>
        /// <param name="surveyPoint"></param>
        /// <returns></returns>
        private static bool IsInPolygon(List<Point> boundaryPoints, SurveyPoint surveyPoint)
        {
            //If the boundary does not contain enough points, return false.
            if (boundaryPoints.Count < 4)
            {
                log.Error($"Not enough points in the boundaryPoints ({boundaryPoints.Count})");
                return false;
            }

            var coef = boundaryPoints.Skip(1).Select((p, i) =>
                                           (surveyPoint.Northing - boundaryPoints[i].Y) * (p.X - boundaryPoints[i].X)
                                         - (surveyPoint.Easting - boundaryPoints[i].X) * (p.Y - boundaryPoints[i].Y))
                                   .ToList();

            if (coef.Any(p => p == 0))
                return true;

            for (int i = 1; i < coef.Count(); i++)
            {
                if (coef[i] * coef[i - 1] < 0)
                    return false;
            }
            return true;
        }
    }
}
