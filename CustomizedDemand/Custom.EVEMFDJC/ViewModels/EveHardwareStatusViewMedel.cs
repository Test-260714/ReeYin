using Custom.EVEMFDJC.Models;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Custom.EVEMFDJC.ViewModels
{
    public class EveHardwareStatusViewMedel : DialogViewModelBase
    {
        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {

        });


        public DelegateCommand Unloaded => new DelegateCommand(() =>
        {

        });
    }
}
