using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.ResultsDisplay
{
    public class ResultsDisplayBase : IResultsDisplay
    {
        public string Name { get; set; }
        public bool IsValid { get; set; }
        public Dictionary<string, object> OtherResult { get; set; }
        public DateTime TransferTime { get; set; } = DateTime.Now;



    }
}
