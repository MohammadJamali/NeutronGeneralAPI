using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
                builder.Ignore<ModelIntraction<TRelation>> ();
            }

            var relations = Enum.GetNames (typeof (TRelation));

            foreach (var relation in relations) {
                var entity = builder.Entity (engineService.MapRelationToType (relation));
                entity.HasKey (new string[] {
                    "FirstModelId",
                    "SecondModelId",
                    "CreateDateTime",
                    "IntractionType"
                });
                entity.ToTable (relation + "IntractionTable");
            }
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