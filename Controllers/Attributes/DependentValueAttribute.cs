using System;
using API.Enums;

namespace API.Attributes {
    /// <summary>
    /// This is the most useful attribute, with this attribute you can provide a value for a dependent
    /// property on serialization or deserialization, for example, if you have a class which must save
    /// profile pictures and there is an other property which must hold thumbnail of this original image
    /// you can use this attribute on that property to let it create thumbnail on serialization.
    ///
    /// <remarks>How to enforce this property to have value? just use <seealso cref="RequiredAttribute"/></remarks>
    ///
    /// <param name="RequestMethod"> Define when API dependency resolver engine should try to resolve the resolver for target property</param>
    /// <param name="ModelAction"> Define when API dependency resolver engine should try to resolve the resolver for target property</param>
    /// <param name="DependentOn"> If only one parameter value is needed to provide dependent value, the property name can be define here, if not set, then hole object will be pass to resolver </param>
    /// <param name="Resolver"> When the time comes, API dependency resolver engine will try to resolve the resolver to determine value of the dependent property</param>
    /// <typeparam name="Resolver">Resolver must implement IDependencyResolver</typeparam>
    ///
    /// بعضی از صفات به مقدار صفات دیگر وابسته هستند، با استفاده از این صفت در زمان سریالاز و یا دیسریالایز مقدار آن ها را
    /// مشخص کنید، مثلا اگر یک صفت باید عکس بندانگشتی از صفت دیگر را نگاهداری کند برای صفت عکس بند انگشتی استفاده کنید و
    /// عکس را در زمان سریالایز شدن مدل بسازید
    /// اگر خواستید مقدار آن را اجباری کنید تنها کافی است از صفت RequiredAttribute در کنار این صفت استفاده کنید
    ///
    /// <seealso cref="ThumbnailDependencyResolver"/>
    /// </summary>
    [AttributeUsage (System.AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class DependentValueAttribute : Attribute {
        public HttpRequestMethod RequestMethod { get; set; }
        public ModelAction ModelAction { get; set; }
        public string DependentOn { get; set; }
        public Type Resolver { get; set; }

        public DependentValueAttribute (
            HttpRequestMethod RequestMethod,
            ModelAction ModelAction,
            Type Resolver,
            string DependentOn = null) {
            this.RequestMethod = RequestMethod;
            this.ModelAction = ModelAction;
            this.Resolver = Resolver;
            this.DependentOn = DependentOn;
        }
    }
}