using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using API.Attributes;
using API.Interface;
using API.Models;
using API.Models.Architecture;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace API.Engine.Extention {
    public static class DatabaseExtention {

        /// <summary>
        /// This function must be called in <see cref="IdentityDbContext<TUser> "/> class and in
        /// <see cref="OnModelCreating"/> function after <code>base.OnModelCreating (builder);</code>
        ///
        /// This function will try to create table per each relation which has been determined with
        /// TRelation enum type with "IntractionTable" postfix
        ///
        /// </summary>
        /// <param name="builder"> Microsoft.EntityFrameworkCore.ModelBuilder </param>
        /// <param name="engineService"> This can be an instance of any class which has
        /// implemented <seealso IApiEngineService<TRelation, TUser>> interface </param>
        /// <param name="TPH"> Table Per Hierarchy </param>
        /// <typeparam name="TRelation"> Type of relation enum </typeparam>
        /// <typeparam name="TUser"> Type of user class which is extended from <see cref="IdentityUser"/></typeparam>
        /// <returns></returns>
        public static void ConfigureAPIDatabase<TRelation, TUser> (
            this ModelBuilder builder,
            IApiEngineService<TRelation, TUser> engineService,
            bool TPH = true) where TUser : IdentityUser {

            if (TPH) {
                builder.Ignore<RootModel> ();
                builder.Ignore<DescriptiveModel> ();
                builder.Ignore<VisualDescriptiveModel> ();
                builder.Ignore<ModelInteraction<TRelation>> ();
            }

            var relations = Enum.GetNames (typeof (TRelation));

            foreach (var relation in relations) {
                var intractionType = engineService.MapRelationToType (relation);
                var entity = builder.Entity (intractionType);
                entity.HasKey (new string[] {
                    "FirstModelId",
                    "SecondModelId",
                    "CreateDateTime",
                    "IntractionType"
                });
                entity.ToTable (relation + "IntractionTable");
                typeof (DatabaseExtention)
                .GetMethod ("ApplyValidIntractionQueryFilter")
                    .MakeGenericMethod (intractionType)
                    .Invoke (null, new object[] { builder });
            }

            var appAssemblies = AppDomain.CurrentDomain.GetAssemblies ().AsParallel ();

            var appName = AppDomain.CurrentDomain.FriendlyName;
            var types = appAssemblies
                .Where (assembly => assembly.GetName ().Name.Equals (appName))
                .FirstOrDefault ()
                .GetExportedTypes ()
                .Where (model =>
                    model.IsSubclassOf (typeof (RootModel)) &&
                    model.IsDefined (typeof (DirectAccessAllowedAttribute)))
                .ToList ();

            foreach (var type in types) {
                typeof (DatabaseExtention)
                .GetMethod ("ApplyDeactivatedQueryFilter")
                    .MakeGenericMethod (type)
                    .Invoke (null, new object[] { builder });
            }
        }

        public static void ApplyDeactivatedQueryFilter<T> (ModelBuilder builder) where T : class {
            builder.Entity<T> ().HasQueryFilter (b => EF.Property<bool> (b, "Deactivated") == false);
        }

        public static void ApplyValidIntractionQueryFilter<T> (ModelBuilder builder) where T : class {
            builder.Entity<T> ().HasQueryFilter (b =>
                EF.Property<bool> (b, "Valid") == true &&
                (
                    (EF.Property<DateTime?> (b, "ValidUntil") == null || EF.Property<DateTime?> (b, "ValidUntil").HasValue == false) ||
                    (EF.Property<DateTime?> (b, "ValidUntil") != null && EF.Property<DateTime?> (b, "ValidUntil").HasValue &&
                        DateTime.Now.CompareTo (EF.Property<DateTime?> (b, "ValidUntil")) < 0)
                )
            );
        }

        public static void ConfigureAPIService (this IServiceCollection services) {
            services.AddMvc ()
                .AddJsonOptions (options => {
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    options.SerializerSettings.MaxDepth = 4;
                    options.SerializerSettings.StringEscapeHandling = StringEscapeHandling.EscapeNonAscii;
                });

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                MaxDepth = 4,
                StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
            };

            services.AddSingleton<IModelParser, ModelParser> ();
        }

        public static void ConfigureAPIHttps (this IServiceCollection services) {
            services.Configure<MvcOptions> (options => {
                options.Filters.Add (new RequireHttpsAttribute ());
            });

            services.AddHttpsRedirection (options => {
                options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
                options.HttpsPort = 5001;
            });
        }

        public static void ConfigureAPIHttps (this IApplicationBuilder app) {
            app.UseHttpsRedirection ();
            app.UseRewriter (new RewriteOptions ().AddRedirectToHttps ());
        }
    }
}