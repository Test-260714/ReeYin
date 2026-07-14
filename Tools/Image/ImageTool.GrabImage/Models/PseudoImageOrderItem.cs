using Prism.Mvvm;
namespace ImageTool.GrabImage.Models
{
    public class PseudoImageOrderItem : BindableBase
    {
        private bool _isCurrent;

        public int Index { get; set; }

        public string FileName { get; set; }

        public string FullPath { get; set; }

        public bool IsCurrent
        {
            get => _isCurrent;
            set
            {
                _isCurrent = value;
                RaisePropertyChanged();
            }
        }

        public void Reset()
        {
            IsCurrent = false;
        }

        public void MarkCurrent()
        {
            IsCurrent = true;
        }

        public void MarkNotCurrent()
        {
            IsCurrent = false;
        }
    }
}
