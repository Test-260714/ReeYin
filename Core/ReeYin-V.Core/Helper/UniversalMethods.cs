using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Share.Helper
{
    public static class UniversalMethods
    {
        /// <summary>
        /// 查找有序List<int>中缺失的最小数字（从0开始连续递增）
        /// </summary>
        /// <param name="sortedList">从小到大排序的List<int></param>
        /// <returns>第一个缺失的数字；若完整则返回最后一个数字+1</returns>
        public static int FindMissingNumber(List<int> sortedList)
        {
            sortedList.Sort();
            // 边界条件：空列表直接返回0
            if (sortedList == null || sortedList.Count == 0)
                return 0;

            // 遍历列表，对比索引与对应的值
            for (int i = 0; i < sortedList.Count; i++)
            {
                // 索引i处的值应该等于i，否则i就是缺失的数字
                if (sortedList[i] != i)
                    return i;
            }

            // 若所有数字连续（如0,1,2,3），则缺失的是最后一个数字+1
            return sortedList.Count;
        }


    }
}
