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
        private static readonly ILog log = log4net.LogManager.GetLogger("Program.cs");

        private static int RawPoints { get; set; }

        /// <summary>
        /// City filter
        /// </summary>
        private static List<string> Cities = new List<string> { };

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

        private static List<Point> FoundPoints = new List<Point> { };
        #endregion

        static void Main(string[] args)
        {
            log.Debug("Program started");
            Console.WriteLine("Hello World!");

            EstablishJobParams();

            OutputFilePath = $"{String.Join(" - ", Cities.ToArray())}_point output_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.csv";

            string recordPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Records";
            foreach (string file in Directory.GetFiles(recordPath, "*.csv", SearchOption.TopDirectoryOnly))
            {
                GatherAndProcessProjects(file);
            }

            //File.WriteAllLines(OutputFilePath, FoundPoints);
            Console.WriteLine("Processing completed.\n" +
                $"This program found {FoundPoints.Count} related points");

            log.Debug("Program ended");
            Console.Write("Program finished, press any key to exit");
            Console.ReadKey();
        }


        #region Establish Job Parameters
        private static void EstablishJobParams()
        {
            log.Debug("Establishing job start and end");
            /*Regex regex = new Regex(@"^\d{2}-\d{2}-\d{3}$");
            log.Debug("Grabbing starting job");
            while (string.IsNullOrEmpty(StartingJobNumber))
            {
                string startEntered = GetUserStringInput("Please enter the starting job number");
                if (regex.IsMatch(startEntered))
                {
                    StartingJobNumber = startEntered;
                }
                else
                {
                    log.Error("Starting job number is not valid.");
                }
            }
            log.Debug("Grabbing ending job");
            regex = new Regex(@"^(?:\d{2}-\d{2}-\d{3})$|^(?:\*)$");
            while (string.IsNullOrEmpty(EndingJobNumber))
            {
                string endjob = GetUserStringInput("Please enter the starting job number or * for last found job number");
                if (regex.IsMatch(endjob))
                {
                    EndingJobNumber = endjob;
                }
                else
                {
                    log.Error("Ending job number is not valid.");
                }
            }*/
            log.Debug("Grabbing cities");
            {
                bool citiesAdded = false;
                string cities = "";
                (int _, int top) = Console.GetCursorPosition();
                while (!citiesAdded) 
                {
                    ClearConsoleAtPoint(top);
                    Console.WriteLine($"Cites: {cities}");
                    string city = GetUserStringInput("Please enter a city to collect");
                    if (string.IsNullOrEmpty(city))
                    {
                        citiesAdded = true;
                    }
                    else
                    {
                        cities += city.ToUpper() + ", ";
                        Cities.Add(city.ToUpper());
                    }
                }
            }
        }
        #endregion

        #region Project Processing
        private static void GatherAndProcessProjects(string path)
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
                bool isWantedCity = Cities.Any(c => project.City.ToUpper().Equals(c));
                if (project != null)
                {
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
                    Point point = new(line, project.JobNumber, File.GetCreationTime(path));
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

        private static void ProcessPointList()
        {

        }

        #region User Input
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
        #endregion
    }
}
