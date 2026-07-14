using System;
using static Custom.KCJC.Models.StandardPlate.KCJC0_StandardPlateAlgorithm;

namespace Custom.KCJC.Models.StandardPlate
{
    public static class KCJC0_StandardPlateAlgorithmService
    {
        public static KCJC0_StandardPlateAlgorithm CreateAlgorithm(KCJC0_StandardPlateMeasureParam param)
        {
            if (param == null)
            {
                throw new ArgumentNullException(nameof(param));
            }

            return param.AlgorithmType switch
            {
                0 => new KCJC0_StandardPlateAlgorithmMeasureLine(),
                1 => new KCJC0_StandardPlateAlgorithmMeasurePoint(),
                2 => new KCJC0_StandardPlateAlgorithmMeasureStep(),
                _ => new KCJC0_StandardPlateAlgorithmMeasurePoint()
            };
        }

        public static KCJC0_StandardPlateAlgorithm CreateMeasureLineAlgorithm()
        {
            return new KCJC0_StandardPlateAlgorithmMeasureLine();
        }

        public static KCJC0_StandardPlateAlgorithm CreateMeasurePointAlgorithm()
        {
            return new KCJC0_StandardPlateAlgorithmMeasurePoint();
        }

        public static KCJC0_StandardPlateAlgorithm CreateMeasureStepAlgorithm()
        {
            return new KCJC0_StandardPlateAlgorithmMeasureStep();
        }
    }
}
