using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Prism.Ioc;
using ReeYin_V.Core.IOC;
using ReeYin_V.Logger;

namespace ReeYin_V.Core.Extension
{
    /// <summary>
    /// 依赖注入扩展类，可以实现加载模块时，实例化标注为ExposedServiceAttriubute特性的类
    /// </summary>
    public static class DependencyExtension
    {

        private static List<Type> GetTypes(Assembly assembly)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                var loaderErrors = ex.LoaderExceptions?
                    .Where(e => e != null)
                    .Select(e => e.Message)
                    .Distinct()
                    .ToArray() ?? Array.Empty<string>();

                Logs.LogWarning(
                    $"程序集类型加载不完整: {assembly.FullName}; " +
                    $"已跳过无法加载的类型。{string.Join("; ", loaderErrors)}");
                types = ex.Types.Where(t => t != null).ToArray();
            }

            var result = types.Where(t => t != null && t.IsClass && !t.IsAbstract &&
            t.CustomAttributes.Any(p => p.AttributeType == typeof(ExposedServiceAttribute))).ToList();

            return result;
        }


        /// <summary>
        /// 扩展IContainerRegistry接口的注册类型的功能
        /// </summary>
        /// <param name="container"></param>
        /// <param name="assembly"></param>
        public static void RegisterAssembly(this IContainerRegistry container, Assembly assembly)
        {
            var list = GetTypes(assembly);

            // 按优先级排序（升序，值越小优先级越高）
            var sortedTypes = list
                .SelectMany(type => GetExposedServices(type).Select(attr => new { Type = type, Attribute = attr }))
                .OrderBy(x => x.Attribute.RegistrationPriority)
                .ToList();


            foreach (var item in sortedTypes)
            {
                RegisterAssembly(container, item.Type);
            }
        }


        private static IEnumerable<ExposedServiceAttribute> GetExposedServices(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.GetCustomAttributes<ExposedServiceAttribute>();
        }

        public static void RegisterAssembly(IContainerRegistry container, Type type)
        {
            var list = GetExposedServices(type).ToList();

            foreach (var item in list)
            {
                if (item.Lifetime == Lifetime.Singleton)
                {
                    container.RegisterSingleton(type);//注册单例
                }

                foreach (var IType in item.Types)
                {
                    if (item.Lifetime == Lifetime.Singleton)
                    {
                        container.RegisterSingleton(IType, type);//以接口注册单例
                    }
                    else if (item.Lifetime == Lifetime.Transient)
                    {
                        container.Register(IType, type);//以接口注册多例
                    }
                }
            }
        }


        /// <summary>
        /// 初始化程序集中所有标注为ExposedServiceAttriubute特性的类，要求单例具自动加载AutoInitialize=true
        /// </summary>
        /// <param name="container"></param>
        /// <param name="assembly"></param>
        public static void InitializeAssembly(this IContainerProvider container, Assembly assembly)
        {
            var list = GetTypes(assembly);

            foreach (var item in list)
            {
                InitializeAssembly(container, item);
            }
        }

        private static void InitializeAssembly(IContainerProvider container, Type type)
        {
            var list = GetExposedServices(type);

            foreach (var item in list)
            {
                if (item.Lifetime == Lifetime.Singleton && item.AutoInitialize)
                {
                    container.Resolve(type);
                }
            }
        }
    }
}
