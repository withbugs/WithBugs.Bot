using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.ConfigOptions
{
    public class CustomVisionOptions
    {
        public string ProjectId { get; set; }
        public string PublishedName { get; set; }

        public string RegionEndpoint { get; set; }

        public string TrainingKey { get; set; }
        public string PredictionKey { get; set; }

        public double ProbabilityThreshold { get; set; }
    }
}
