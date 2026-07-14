using System.Reflection;
using ACS.SPiiPlusNET;

var filters = new[] { "Variable", "Read", "Write", "Transaction", "UploadData", "LoadData" };
foreach (var method in typeof(Api).GetMethods(BindingFlags.Public | BindingFlags.Instance).OrderBy(m => m.Name))
{
    if (!filters.Any(f => method.Name.Contains(f, StringComparison.OrdinalIgnoreCase))) continue;
    var parameters = string.Join(", ", method.GetParameters().Select(p => $"{FormatType(p.ParameterType)} {p.Name}"));
    Console.WriteLine($"{FormatType(method.ReturnType)} {method.Name}({parameters})");
}

static string FormatType(Type type)
{
    if (type.IsByRef) return FormatType(type.GetElementType()!) + "&";
    if (type.IsArray) return FormatType(type.GetElementType()!) + "[]";
    if (!type.IsGenericType) return type.FullName ?? type.Name;
    var name = type.GetGenericTypeDefinition().FullName ?? type.Name;
    name = name.Split('`')[0];
    return name + "<" + string.Join(", ", type.GetGenericArguments().Select(FormatType)) + ">";
}
