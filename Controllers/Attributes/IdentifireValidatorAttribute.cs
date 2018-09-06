using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace API.Attributes {
    /// <summary>
    /// Any CRUD request must be queried by model's key property, when key property is not
    /// accesseble directly (for example when user model extent from IdentityUser<TKey>) you
    /// can specify it by using this attribute, Or you can simply let users generate select
    /// queries by more than one atribute (This atribute must be uniqe for any user)
    ///
    /// <param name="PropertyName">Name of that property</param>
    /// <param name="Validator">Any query identifier value must be verify before use it
    /// as query parameter, there are some ready to use validators </param>
    ///
    /// برای انجام اعمال کراد باید از کلید مدل در زمان کويری زدن استفاده شود یا پراپرتی که برای هر مدل منحصر به فرد باشد
    /// بعضی مواقع که این صفت در دسترس نیست مانند زمانی که از یک کلاس داخلی ارث بری می کنید می توانید آن پراپرتی را با این
    /// صفت مشخص کنید و برای پراپرتی هایی که در دسترس هستند تنها این صفت را برای آن ها استفاده کنید
    ///
    /// برای ایجاد یک رابطه تنها صفت کلید اصلی قابل قبول خواهد بود
    ///
    /// <seealso cref="EmailIdentifireValidator"/>
    /// <seealso cref="FreeForAllValidator"/>
    /// <seealso cref="GuidIdentifireValidator"/>
    /// <seealso cref="ImageDataValidator"/>
    /// </summary>
    [AttributeUsage (AttributeTargets.Class | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public sealed class IdentifireValidatorAttribute : PermissionAttribute {
        public string PropertyName { get; set; }
        public Type Validator { get; set; }

        public IdentifireValidatorAttribute (Type validator, [CallerMemberName] string propertyName = null) {
            this.Validator = validator;
            this.PropertyName = propertyName;
        }

        public IdentifireValidatorAttribute (String propertyName, Type validator) {
            this.Validator = validator;
            this.PropertyName = propertyName;
        }
    }
}