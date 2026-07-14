using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nodify.FlowApp
{
    public enum SettingsType
    {
        Boolean,
        Number,
        Option,
        Point,
        Text
    }
    public interface ISettingViewModel
    {
        string Name { get; }

        /// <summary>
        /// Represents the content within the tooltip.
        /// </summary>
        string? Description { get; }
        object? Value { get; set; }

        SettingsType Type { get; }
    }
}
