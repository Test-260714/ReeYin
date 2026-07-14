using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models.Database.Repository;
using ReeYin_V.Core.Models.Database.Tables;
using ReeYin_V.Core.Services;
using ReeYin_V.Share.Models;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.UserManager.ViewModels
{
    public class EditPermissionViewModel : DialogViewModelBase
    {
        private PermMenuRelationRepository PermMenuRelationRepository { get; }

        private ObservableCollection<PermMenuRelation> _menus;
        /// <summary>
        /// 所有菜单
        /// </summary>
        public ObservableCollection<PermMenuRelation> Menus
        {
            get
            {
                return _menus;
            }
            set
            {
                _menus = value;
                RaisePropertyChanged();
            }
        }

        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "取消":
                    CloseDialog(ButtonResult.No, new DialogParameters()
                    {
                    });
                    break;
                case "确认":
                    CloseDialog(ButtonResult.Yes, new DialogParameters()
                    {
                       
                    });
                    PermMenuRelationRepository.UpdateRange(Menus.ToList());
                    break;
            }
        });

        #region Methods
        public EditPermissionViewModel(PermMenuRelationRepository permMenuRelationRepository)
        {
            PermMenuRelationRepository = permMenuRelationRepository;
        }
        public override void InitParam()
        {
            if (Param != null && (Param is int))
            {
                Menus = PermMenuRelationRepository.GetList(x => x.PermId == (int)Param).ToObservableCollection();
            }
        }
        #endregion
    }
}
