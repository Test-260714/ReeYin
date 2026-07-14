using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.CustomProject
{
    public interface ICustomProjectManager
    {
        /// <summary>
        /// 初始化相关资源
        /// </summary>
        /// <returns></returns>
        bool Init();


        /// <summary>
        /// 释放相关资源
        /// </summary>
        /// <returns></returns>
        void Dispose();
    }

    public interface ICustomAlgo
    {
        int InitVariable();

        void Dispose();
    }
}
