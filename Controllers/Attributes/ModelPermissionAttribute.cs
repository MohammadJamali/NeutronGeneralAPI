using System;
using API.Enums;

namespace API.Attributes {
    /// <summary>
    /// ModelPermission is the most important attribute in this general api, it can be use
    /// for classes and properties and It has different behaviors for each of them.
    ///
    /// <param name="RequestMethod">With this property you can define appropriate HttpRequestMethod which this policy must be enforced to.</param>
    /// <param name="ModelAction">With this property you can define appropriate ModelAction which this policy must be enforced to.</param>
    /// <param name="RequestMethod">With this property you can define the policy with must be pass
    /// on each request with matched ModelAction and HttpRequestMethod. This type must implement
    /// <seealso cref="IAccessChainValidator"/> or <seealso cref="IPermissionValidator"/> depend on usage</param>
    ///
    /// Usage description:
    /// <list type="bulltet">
    ///     <listheader>
    ///        <term>Class</term>
    ///        <description>If object don't pass access chain, api will reject that action {ModelAction} and replay with appropriate error message.</description>
    ///     </listheader>
    ///     <item>
    ///        <term>Property</term>
    ///        <description>If property don't pass access chain, api will replace its value with null</description>
    ///     </item>
    /// </list>
    /// </summary>
    [AttributeUsage (AttributeTargets.Class | AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public sealed class ModelPermissionAttribute : PermissionAttribute {
        public HttpRequestMethod RequestMethod { get; set; }
        public ModelAction ModelAction { get; set; }
        public Type AccessChainResolver { get; set; }

        public ModelPermissionAttribute (
            HttpRequestMethod requestMethod,
            ModelAction modelAction,
            Type AccessChainResolver) {
            this.RequestMethod = requestMethod;
            this.ModelAction = modelAction;
            this.AccessChainResolver = AccessChainResolver;
        }
    }
}