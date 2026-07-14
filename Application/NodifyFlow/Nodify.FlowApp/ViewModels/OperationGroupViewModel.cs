using Newtonsoft.Json;
using ReeYin_V.NodifyManager;
using System.Windows;

namespace Nodify.FlowApp
{
    [Serializable]
    public class OperationGroupViewModel : OperationViewModel
    {
        [JsonIgnore]
        private Size _size;
        public Size GroupSize
        {
            get => _size;
            set => SetProperty(ref _size, value);
        }
    }
}