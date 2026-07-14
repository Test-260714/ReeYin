using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComTool.General.Communacation
{
    public interface IDataCell
    {
        byte[] ToBuffer();
        void FromBuffer(byte[] buffer);
    }
}
