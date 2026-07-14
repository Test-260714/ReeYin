using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Share.Communication.Tool
{
    public interface IDataCell
    {
        byte[] ToBuffer();
        void FromBuffer(byte[] buffer);
    }
}
