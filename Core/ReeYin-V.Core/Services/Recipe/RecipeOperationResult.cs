using System;

namespace ReeYin_V.Core.Services.Recipe
{
    /// <summary>
    /// 配方操作结果
    /// </summary>
    public class RecipeOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }

        public static RecipeOperationResult Ok(string message = null)
        {
            return new RecipeOperationResult
            {
                Success = true,
                Message = message ?? "操作成功"
            };
        }

        public static RecipeOperationResult Fail(string message, Exception ex = null)
        {
            return new RecipeOperationResult
            {
                Success = false,
                Message = message,
                Exception = ex
            };
        }
    }
}

