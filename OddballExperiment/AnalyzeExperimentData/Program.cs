using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AnalyzeExperimentData
{
    class Program
    {
        static void Main(string[] args)
        {
            DataLoadAndAggregate dlaa = new DataLoadAndAggregate();
            dlaa.startExecution();
        }
    }
}
