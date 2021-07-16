using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using log4net;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace ConsoleSniffer
{
    class Program
    {
        #region private variables
        /// <summary>
        /// Logger
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger("Program.cs");

        /// <summary>
        /// City filter
        /// </summary>
        private static string TargetCity { get; set; }

        /// <summary>
        /// The point output file;
        /// </summary>
        private static string OutputFilePath { get; set; }

        /// <summary>
        /// Point keyword whitelist
        /// </summary>
        private static readonly List<string> KeywordWhitelist = new List<string> { "PCP", ",CM", "NLD" };

        /// <summary>
        /// Point keywork blacklist
        /// </summary>
        private static readonly List<string> KeywordBlacklist = new List<string> { ",IRC","SET" };

        private static List<SurveyPoint> FoundPoints = new List<SurveyPoint> { };
        private static List<SurveyPoint> PointsToRemove = new List<SurveyPoint> { };
        private static List<SurveyPoint> SavePoints = new List<SurveyPoint> { };

        #endregion

        /// <summary>
        /// Main entry point, handles app closure.
        /// </summary>
        /// <param name="args">Command line params (just don't)</param>
        static void Main(string[] args)
        {
            log.Debug("Program started");

            //Initial project setup.
            GetCityFromUser();
            bool isValidExistingData = ProessExistingPoints();
            ExitApplication(isValidExistingData, 1);

            //Process project files.
            bool ProjectFileProcessed = HandleRecordFiles();
            ExitApplication(ProjectFileProcessed, 2);

            //Process points.
            bool PointsHandled = HandlePointProcessing();
            ExitApplication(PointsHandled, 4);

            UpdateFile();

            Console.WriteLine("Processing completed.\n" +
                $"This program found {SavePoints.Count} (of total {FoundPoints.Count}) related points");
            ExitApplication(true, 0);
        }


        #region Phase 1 - Establish Job Parameters
        /// <summary>
        /// Gets the city from the user.
        /// </summary>
        private static void GetCityFromUser()
        {            
            log.Debug("Grabbing city");
            while (string.IsNullOrEmpty(TargetCity))
            {
                string consoleReturn = GetUserStringInput("Please enter a city.");
                if (!string.IsNullOrEmpty(consoleReturn))
                {
                    TargetCity = consoleReturn;
                }
                else
                {
                    log.Error("You must enter a city.");
                }
            }
        }

        /// <summary>
        /// Establishes existing data. If database (.csv) does not exist. Ask to create or quit.
        /// </summary>
        /// <returns>Database exists and points are compiled.</returns>
        private static bool ProessExistingPoints()
        {
            string baseDir = @"Z:\GIS Data\Control Points\Source\" + TargetCity.ToUpper() + ".csv";
            if (!File.Exists(baseDir))
            {
                Console.Write($"\nA database for {TargetCity.ToUpper()} does not exist. Create a new database? [y/n]: ");
                var userKey = Console.ReadKey();
                if (userKey.Key == ConsoleKey.N)
                {
                    log.Error($"Base file \"{baseDir}\" does not exist");
                    return false;
                }
                var newFile = File.Create(baseDir);
                newFile.Close();
            }
            OutputFilePath = baseDir;
            log.Info("PHASE 1. Processing existing points.");
            foreach (string line in File.ReadAllLines(baseDir))
            {
                if (line.Equals("ID,Y,X,Z,Description,SourceJob,DateCollected,OriginalNumber"))
                {
                    continue;
                }
                FoundPoints.Add(new(line));
            }
            return true;
        }
        #endregion

        #region Phase 2 - Project Files Processing

        private static bool HandleRecordFiles()
        {
            string recordPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Records";
            if (!Directory.Exists(recordPath) || Directory.GetFiles(recordPath).Count() < 1)
            {
                log.Error($"No record information was found in {recordPath}");
                return false;
            }
            bool recordFailedToProcess = false;
            foreach (string file in Directory.GetFiles(recordPath, "*.csv", SearchOption.TopDirectoryOnly))
            {
                if (!recordFailedToProcess && GatherAndProcessProjects(file) == recordFailedToProcess)
                {
                    recordFailedToProcess = true;
                }
            }
            return recordFailedToProcess ? false : true;
        }

        private static bool GatherAndProcessProjects(string path)
        {
            try
            {
                log.Info($"Processing log file \"{Path.GetFileName(path)}\"");
                var projectRawData = File.ReadAllLines(path);
                foreach (string projectRaw in projectRawData)
                {
                    Project project = new(projectRaw.Split('|'));
                    if (project.ContainsCriticalNullValues)
                    {
                        continue;
                    }
                    if (project != null)
                    {
                        bool isWantedCity = project.City.ToUpper() == TargetCity.ToUpper();
                        if (isWantedCity)
                        {
                            log.Debug($"Project matches criteria: {project.JobNumber}");
                            ProcessProject(project);
                        }
                        else
                        {
                            log.Debug($"Project discarded for non-wanted city");
                        }
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                log.Error($"Processing of record {path} failed", e);
                return false;
            }
        }

        private static void ProcessProject(Project project)
        {
            log.Info($"Processing Project {project.JobNumber} in {project.City}");
            foreach (string file in GetAllTextFiles(project.Path))
            {
                HandleFile(file, project);
            }
        }

        private static List<string> GetAllTextFiles(string projectPath)
        {
            log.Debug($"Project path: {projectPath}");
            if (!Directory.Exists(projectPath))
            {
                log.Warn("Project does not have a file on the server.");
                return new List<string> { };
            }
            List<string> textFiles = Directory.GetFiles(projectPath, "*.txt", SearchOption.AllDirectories).ToList();
            log.Debug($"Text files found: {textFiles.Count}");
            return textFiles;
        }

        /// <summary>
        /// Reads a text file and grabs all control points.
        /// </summary>
        /// <param name="path">text file path.</param>
        private static void HandleFile(string path, Project project)
        {
            log.Info($"Processing File: {path}");
            var fileLines = File.ReadAllLines(path);
            foreach (string line in fileLines)
            {
                log.Debug(line);
                bool containsWhitelist = KeywordWhitelist.Any(s => line.Contains(s));
                bool containsBlacklist = KeywordBlacklist.Any(s => line.Contains(s));
                if (containsWhitelist && !containsBlacklist)
                {
                    SurveyPoint point = new(line, project.JobNumber, File.GetCreationTime(path));
                    point.ID = $"{DateTime.Now:yy}-{FoundPoints.Count + 1:00000}";
                    bool alreadyFound = FoundPoints.Any(p => point.Equals(p));
                    if (alreadyFound)
                    {
                        log.Debug($"Duplicate point: {point}");
                    }
                    else
                    {
                        log.Info($"Found point: {point}");
                        FoundPoints.Add(point);
                    }
                }
                else
                {
                    log.Debug($"Discarded point: {line}");
                }
            }
        }
        #endregion

        #region Phase 3 - Point Checks and Storing

        private static bool HandlePointProcessing()
        {
            if (FoundPoints.Count < 1)
            {
                log.Error("No points are found.");
                ExitApplication(false, 3);
            }
            foreach (SurveyPoint point in FoundPoints)
            {
                if (PointsToRemove.Contains(point) || SavePoints.Contains(point))
                {
                    log.Info("Point has already been marked for removal.");
                    continue;
                }
                Console.WriteLine(point.ToString() + "\n===================================================================================");
                var ClosePointCount = FoundPoints.Where(op => op.ID != point.ID && point.CompareLocation(op, 2));
                if (ClosePointCount.Count() > 0)
                {
                    var VaryingPointCount = FoundPoints.Where(op => op.ID != point.ID && point.CompareLocation(op, .15));
                    log.Info($"Point has {ClosePointCount.Count()} nearby points (<= 2) and {VaryingPointCount.Count()} varying points (<= 0.15).");
                    log.Warn("Close Points:");
                    foreach (SurveyPoint pointOther in ClosePointCount)
                    {
                        log.Info($"\t{pointOther}");
                    }
                    log.Warn("Really Close Points:");
                    foreach (SurveyPoint pointOther in VaryingPointCount)
                    {
                        log.Info($"\t{pointOther}");
                    }
                    CombinePoints(point, VaryingPointCount.ToList());
                }
            }
            return true;
        }

        private static bool CombinePoints(SurveyPoint basePoint, List<SurveyPoint> otherPoints)
        {
            SurveyPoint point = basePoint.CombinePoints(otherPoints);
            SavePoints.Add(point);
            foreach (SurveyPoint surveyPoint in otherPoints)
            {
                PointsToRemove.Add(surveyPoint);
            }
            log.Info($"Removed {otherPoints.Count} points and kept {point.ID}");
            return true;
        }

        private static void UpdateFile()
        {
            var stream = File.Create(OutputFilePath);
            stream.Close();
            File.WriteAllText(OutputFilePath, "ID,Y,X,Z,Description,SourceJob,DateCollected,OriginalNumber\n");
            for (int i = 0; i < SavePoints.Count; i++)
            {
                SurveyPoint point = SavePoints[i];
                point.ID = $"{DateTime.Now:yy}-{i + 1:00000}";
                log.Info($"Writing {point} to database");
                File.AppendAllText(OutputFilePath, point.ToString() + Environment.NewLine);
            }
        }
        #endregion

        #region Misc - User Input
        /// <summary>
        /// Get string from user.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <returns>string of input</returns>
        private static string GetUserStringInput(string message)
        {
            Console.Write($"{message}: ");
            string userInput = Console.ReadLine();
            if (string.IsNullOrEmpty(userInput))
            {
                return "";
            }
            else
            {
                return userInput;
            }
        }

        private static void ClearConsoleAtPoint(int top)
        {
            int consoleHeight = Console.WindowHeight;
            int consoleWidth = Console.WindowWidth;
            Console.SetCursorPosition(0, top);
            string blank = new string(' ', consoleWidth);
            for (int i = 0; i < consoleHeight-top; i++)
            {
                Console.WriteLine(blank);
            }
            Console.SetCursorPosition(0, top);
        }

        /// <summary>
        /// Alerts the user the application cannot continue and closes application.
        /// </summary>
        /// <param name="success">Bool if method succeded</param>
        /// <param name="errorCode">Error code ID</param>
        private static void ExitApplication(bool success, int errorCode)
        {
            if (!success)
            {
                //If no database exists, we are not going to process data.
                log.Error("Could not continue due to fatal error. Press any key to exit.");
                Console.ReadKey();
                log.Debug($"Program closed with exit code {errorCode}");
                Environment.Exit(errorCode);
            }
            else if (errorCode == 0)
            {
                Console.WriteLine("Program successfully completed.");
                log.Debug("Program ended with exit code 0");
                Console.Write("Press any key to exit");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }
        #endregion
    }
}
