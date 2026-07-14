using Newtonsoft.Json;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.ObjectModel;

namespace LogicalTool.ParamLink.Models
{
    /// <summary>
    /// 配方模型
    /// </summary>
    [Serializable]
    public class Recipe : BindableBase
    {
        [JsonIgnore]
        private Guid _id = Guid.NewGuid();
        public Guid Id
        {
            get { return _id; }
            set { _id = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _name = "新配方";
        public string Name
        {
            get { return _name; }
            set { _name = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _description = "";
        public string Description
        {
            get { return _description; }
            set { _description = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private DateTime _createTime = DateTime.Now;
        public DateTime CreateTime
        {
            get { return _createTime; }
            set { _createTime = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private DateTime _updateTime = DateTime.Now;
        public DateTime UpdateTime
        {
            get { return _updateTime; }
            set { _updateTime = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<TransmitParam> _params = new ObservableCollection<TransmitParam>();
        public ObservableCollection<TransmitParam> Params
        {
            get { return _params; }
            set { _params = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(ParamCount)); }
        }

        /// <summary>
        /// 参数数量
        /// </summary>
        [JsonIgnore]
        public int ParamCount => Params?.Count ?? 0;

        public Recipe()
        {
            Params.CollectionChanged += (s, e) => RaisePropertyChanged(nameof(ParamCount));
        }

        /// <summary>
        /// 深拷贝
        /// </summary>
        public Recipe Clone()
        {
            var clone = new Recipe
            {
                Id = Guid.NewGuid(),
                Name = this.Name + "_副本",
                Description = this.Description,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now,
                Params = new ObservableCollection<TransmitParam>()
            };

            foreach (var param in this.Params)
            {
                clone.Params.Add(new TransmitParam
                {
                    Name = param.Name,
                    Type = param.Type,
                    Value = param.Value,
                    Describe = param.Describe,
                    Resourece = param.Resourece
                });
            }

            return clone;
        }
    }
}
