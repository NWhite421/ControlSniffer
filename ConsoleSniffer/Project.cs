using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleSniffer
{
    public class Project
    {
        private static readonly ILog log = log4net.LogManager.GetLogger("Project.cs");

        /// <summary>
        /// The job number of the project.
        /// </summary>
        public string JobNumber { get; set; }

        /// <summary>
        /// Customer name.
        /// </summary>
        public string Customer { get; set; }

        /// <summary>
        /// Address or parcel information
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// City Name
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// ZIP Code
        /// </summary>
        public string ZIP { get; set; }

        /// <summary>
        /// Project description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Project Notes
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Project path on server.
        /// </summary>
        public string Path { 
            get {
                string[] jobNumber = JobNumber.Split('-');
                string year = $"20{jobNumber[0]}";
                string month = jobNumber[1];
                string project = jobNumber[2];
                return $@"Z:\{year}\{month}-{year}\{month}-{project}";
            } 
        }

        public bool ContainsCriticalNullValues
        {
            get
            {
                if (string.IsNullOrEmpty(JobNumber) ||
                    string.IsNullOrEmpty(City))
                {
                    return true;
                }
                return false;
            }
        }


        public Project(string[] lineItem)
        {
            try
            {
                string jobNumber = lineItem[0];
                string customer = lineItem[1];
                if (string.IsNullOrEmpty(jobNumber) || string.IsNullOrEmpty(customer))
                {
                    log.Info($"line discarded for inadequate info: {string.Join(',', lineItem)}");
                }
                JobNumber = lineItem[0];
                Customer = lineItem[1];
                Address = lineItem[2];
                City = lineItem[3];
                ZIP = lineItem[4];
                Description = lineItem[5];
                Notes = lineItem[6];
            }
            catch(IndexOutOfRangeException)
            {
                log.Warn("Project did not have enough arguments (probably a formatting issue)");
            }
            catch(Exception e)
            {
                log.Error("Error processing project", e);
            }
        }
    }
}
