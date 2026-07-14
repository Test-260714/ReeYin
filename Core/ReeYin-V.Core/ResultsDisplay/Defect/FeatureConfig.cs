using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.ResultsDisplay
{
    public class FeatureConfig
    {
        [JsonProperty("algorithm_list")]
        public List<Algorithm> AlgorithmList { get; set; } = new List<Algorithm>();

        [JsonProperty("defect_list")]
        public List<Defect> DefectList { get; set; } = new List<Defect>();
    }

    public class Algorithm
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("parameters")]
        public List<Parameter> Parameters { get; set; } = new List<Parameter>();
    }

    public class Defect
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("alg_name")]
        public string AlgName { get; set; }

        [JsonProperty("alg_param")]
        public List<Parameter> AlgParam { get; set; } = new List<Parameter>();
    }

    public class Parameter
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("describe")]
        public string Describe { get; set; }

        [JsonProperty("value")]
        public object Value { get; set; }

        // UI配置属性
        [JsonProperty("ui_type")]
        public string UiType { get; set; } = "numeric"; // numeric, combo, checkbox

        [JsonProperty("min_value")]
        public double? MinValue { get; set; }

        [JsonProperty("max_value")]
        public double? MaxValue { get; set; }

        [JsonProperty("decimal_places")]
        public int? DecimalPlaces { get; set; }

        [JsonProperty("increment")]
        public double? Increment { get; set; }

        [JsonProperty("unit")]
        public string Unit { get; set; }

        [JsonProperty("options")]
        public List<string> Options { get; set; } = new List<string>();
    }
}
