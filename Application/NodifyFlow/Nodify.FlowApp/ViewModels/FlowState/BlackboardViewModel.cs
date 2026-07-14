using ReeYin_V.NodifyManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DelegateCommand = ReeYin_V.NodifyManager.DelegateCommand;

namespace Nodify.FlowApp
{
    public class BlackboardViewModel : ObservableObject
    {
        private NodifyObservableCollection<BlackboardKeyViewModel> _keys = new NodifyObservableCollection<BlackboardKeyViewModel>();
        public NodifyObservableCollection<BlackboardKeyViewModel> Keys
        {
            get => _keys;
            set => SetProperty(ref _keys, value);
        }

        private NodifyObservableCollection<BlackboardItemReferenceViewModel> _actions = new NodifyObservableCollection<BlackboardItemReferenceViewModel>();
        public NodifyObservableCollection<BlackboardItemReferenceViewModel> Actions
        {
            get => _actions;
            set => SetProperty(ref _actions, value);
        }

        private NodifyObservableCollection<BlackboardItemReferenceViewModel> _conditions = new NodifyObservableCollection<BlackboardItemReferenceViewModel>();
        public NodifyObservableCollection<BlackboardItemReferenceViewModel> Conditions
        {
            get => _conditions;
            set => SetProperty(ref _conditions, value);
        }

        public INodifyCommand AddKeyCommand { get; }
        public INodifyCommand RemoveKeyCommand { get; }

        public BlackboardViewModel()
        {
            AddKeyCommand = new DelegateCommand(() => Keys.Add(new BlackboardKeyViewModel
            {
                Name = "New Key "
            }));

            RemoveKeyCommand = new ReeYin_V.NodifyManager.DelegateCommand<BlackboardKeyViewModel>(key => Keys.Remove(key));

            Keys.WhenAdded(key =>
            {
                var existingKeyNames = Keys.Where(k => k != key).Select(k => k.Name).ToList();
                key.Name = existingKeyNames.GetUnique(key.Name);
            });
        }
    }
}
