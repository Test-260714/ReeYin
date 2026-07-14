using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Custom.KCJC.Models.KCJC0_Algorithm;

namespace Custom.KCJC.Models.ALGO
{
    public class KCJC0_AlgorithmService
    {

        public KCJC0_AlgorithmService()
        {

        }

        public static KCJC0_Algorithm CreateAlgorithm(KCJC0_MeasureParam param)
        {
            if (param.AlgorithmType == 0)
            {
                return new KCJC0_AlgorithmMeasureLine();
            }
            else if (param.AlgorithmType == 1)
            {
                return new KCJC0_AlgorithmMeasurePoint();
            }
            else if (param.AlgorithmType == 2)
            {
                return new KCJC0_AlgorithmMeasureLinePoint();
            }
            else
            {
                return new KCJC0_AlgorithmMeasurePoint();
            }
        }


        public void Dispose()
        {

        }






    }
}
