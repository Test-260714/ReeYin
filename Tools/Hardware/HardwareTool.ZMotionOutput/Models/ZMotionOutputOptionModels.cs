namespace HardwareTool.ZMotionOutput.Models
{
    public sealed class ZMotionOutputRoleOption
    {
        public ZMotionOutputRoleOption(string roleKey, string roleName)
        {
            RoleKey = roleKey;
            RoleName = roleName;
        }

        public string RoleKey { get; }

        public string RoleName { get; }
    }

    public sealed class ZMotionBoolOption
    {
        public ZMotionBoolOption(bool value, string name)
        {
            Value = value;
            Name = name;
        }

        public bool Value { get; }

        public string Name { get; }
    }

    public sealed class ZMotionResetPolicyOption
    {
        public ZMotionResetPolicyOption(string policyKey, string policyName)
        {
            PolicyKey = policyKey;
            PolicyName = policyName;
        }

        public string PolicyKey { get; }

        public string PolicyName { get; }
    }

    public sealed class ZMotionSourceRoleOption
    {
        public ZMotionSourceRoleOption(string roleKey, string roleName)
        {
            RoleKey = roleKey;
            RoleName = roleName;
        }

        public string RoleKey { get; }

        public string RoleName { get; }
    }

    public sealed class ZMotionSourceResolverOption
    {
        public ZMotionSourceResolverOption(string resolverKey, string resolverName)
        {
            ResolverKey = resolverKey;
            ResolverName = resolverName;
        }

        public string ResolverKey { get; }

        public string ResolverName { get; }
    }

    public sealed class ZMotionRuleConditionOption
    {
        public ZMotionRuleConditionOption(string conditionKey, string conditionName)
        {
            ConditionKey = conditionKey;
            ConditionName = conditionName;
        }

        public string ConditionKey { get; }

        public string ConditionName { get; }
    }

    public sealed class ZMotionRuleActionOption
    {
        public ZMotionRuleActionOption(string actionKey, string actionName)
        {
            ActionKey = actionKey;
            ActionName = actionName;
        }

        public string ActionKey { get; }

        public string ActionName { get; }
    }
}